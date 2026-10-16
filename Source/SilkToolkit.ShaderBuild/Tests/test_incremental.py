"""Run real DXC through the owning MSBuild target in an isolated temporary fixture.

Usage: py -3 test_incremental.py --dxc <current-package-x64-dxc.exe> --evidence <directory>
Use --targets to run the same discriminator against an unmodified baseline target.
No SDK restore, GPU, product resource generation, or compiler substitute is involved.
"""

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import tempfile
import time


def describe(path):
    """Return raw output identity and write time for an existing file."""
    data = path.read_bytes()
    return {"sha256": hashlib.sha256(data).hexdigest(), "bytes": len(data),
            "mtime_ns": path.stat().st_mtime_ns}


def rewrite(path, text):
    """Write changed fixture input after a timestamp gap visible to MSBuild."""
    time.sleep(0.1)
    path.write_text(text, encoding="utf-8")


def run(args):
    """Assert include-only recompilation and genuine unchanged-build compiler skips."""
    evidence = args.evidence.resolve()
    evidence.mkdir(parents=True, exist_ok=True)
    records = []
    with tempfile.TemporaryDirectory(prefix="silk-dxc-incremental-") as temporary:
        fixture = Path(temporary)
        compiler = fixture / "compiler"
        compiler.mkdir()
        for name in ("dxc.exe", "dxcompiler.dll", "dxil.dll"):
            shutil.copyfile(args.dxc.resolve().parent / name, compiler / name)
        (fixture / "PS/Nested").mkdir(parents=True)
        target = fixture / "ShaderBuild.targets"
        shutil.copyfile(args.targets, target)
        helper = fixture / "PS/psCommon.hlsl"
        nested = fixture / "PS/Nested/value.hlsli"
        helper.write_text('#include "Nested/value.hlsli"\nfloat value() { return 0.25 + offset(); }\n',
                          encoding="utf-8")
        nested.write_text("float offset() { return 0.125; }\n", encoding="utf-8")
        (fixture / "PS/pixel.hlsl").write_text(
            '#include "psCommon.hlsl"\n'
            '#ifndef SHIFT\n#define SHIFT 0.0\n#endif\n'
            'float4 main() : SV_Target { return float4(value() + SHIFT, 0, 0, 1); }\n'
            'float4 alternate() : SV_Target { return float4(0, value() + SHIFT, 0, 1); }\n',
            encoding="utf-8")
        project = fixture / "Fixture.proj"
        project.write_text('''<Project>
  <PropertyGroup>
    <IntermediateOutputPath>obj/</IntermediateOutputPath>
    <TargetDir>bin/</TargetDir>
    <FixtureEntry Condition="'$(FixtureEntry)' == ''">main</FixtureEntry>
  </PropertyGroup>
  <ItemGroup>
    <DxcShader Include="PS/pixel.hlsl">
      <ShaderProfile>ps_6_0</ShaderProfile>
      <EntryPointName>$(FixtureEntry)</EntryPointName>
      <ObjectFileOutput>bin/pixel.dxil</ObjectFileOutput>
    </DxcShader>
    <DxcShader Include="PS/pixel.hlsl">
      <ShaderProfile>ps_6_0</ShaderProfile>
      <EntryPointName>alternate</EntryPointName>
      <ShaderName>pixelAlias</ShaderName>
    </DxcShader>
  </ItemGroup>
  <Import Project="ShaderBuild.targets"/>
</Project>
''', encoding="utf-8")
        outputs = [fixture / "bin/pixel.dxil", fixture / "bin/DX12/PS/pixelAlias.alternate.dxil"]
        environment = dict(os.environ, DOTNET_CLI_UI_LANGUAGE="en-US", VSLANG="1033")

        def build(name, compile_count, properties=(), changed=True):
            """Run MSBuild, retain diagnostics, and check execution plus raw output behavior."""
            before = [describe(path) for path in outputs] if outputs[0].exists() else None
            command = ["dotnet", "msbuild", str(project), "-t:CompileDxcShaders", "-m:1", "-nr:false",
                       "-v:diag", "-tl:off", f"-p:DxcToolPath={compiler / 'dxc.exe'}",
                       "-p:Configuration=Release", *properties]
            result = subprocess.run(command, cwd=fixture, env=environment, capture_output=True,
                                    text=True, encoding="utf-8", errors="replace", timeout=60)
            log = result.stdout + result.stderr
            (evidence / f"{name}.log").write_text(log, encoding="utf-8")
            after = [describe(path) for path in outputs] if result.returncode == 0 else []
            executions = len(re.findall(r'Task "Exec"', log))
            skips = len(re.findall(r'Skipping target "RunDxcShaders"', log))
            records.append({"name": name, "command": command, "exit_code": result.returncode,
                            "compiler_executions": executions, "target_skips": skips,
                            "before": before, "after": after})
            (evidence / "results.json").write_text(json.dumps(records, indent=2) + "\n", encoding="utf-8")
            assert result.returncode == 0, f"{name}: MSBuild failed; see diagnostic log"
            assert executions == compile_count, f"{name}: expected {compile_count} real Exec tasks, got {executions}"
            for index, path in enumerate(outputs):
                data = path.read_bytes()
                assert data[:4] == b"DXBC" and b"DXIL" in data, f"{name}: output is not DXIL"
                shutil.copyfile(path, evidence / f"{name}-{index}.dxil")
            if before is not None:
                if changed is True:
                    assert before[0]["sha256"] != after[0]["sha256"], f"{name}: raw DXIL stayed stale"
                elif changed is False:
                    assert before == after and skips == 2, f"{name}: unchanged build did not skip both batches"
                else:
                    assert all(old["mtime_ns"] != new["mtime_ns"] for old, new in zip(before, after)), \
                        f"{name}: outputs were not rewritten"
            print(f"{name}: {executions} real compiler tasks, {skips} target skips", flush=True)

        build("01-initial", 2)
        rewrite(helper, helper.read_text(encoding="utf-8").replace("0.25", "0.75"))
        build("02-ps-include-only", 2)
        build("03-unchanged", 0, changed=False)
        rewrite(nested, "float offset() { return 0.0625; }\n")
        build("04-nested-hlsli-only", 2)
        build("05-options-only", 2, ["-p:DxcDebugArgs=-O3 -D SHIFT=0.125"])
        build("06-options-unchanged", 0, ["-p:DxcDebugArgs=-O3 -D SHIFT=0.125"], changed=False)
        metadata = ["-p:DxcDebugArgs=-O3 -D SHIFT=0.125", "-p:FixtureEntry=alternate"]
        build("07-entry-metadata-only", 2, metadata)
        build("08-entry-unchanged", 0, metadata, changed=False)
        rewrite(project, project.read_text(encoding="utf-8").replace("<Project>", "<Project><!-- metadata -->"))
        build("09-project-only", 2, metadata, changed=None)
        rewrite(target, target.read_text(encoding="utf-8").replace("<Project>", "<Project><!-- target -->"))
        build("10-target-only", 2, metadata, changed=None)
        time.sleep(0.1)
        (compiler / "dxc.exe").touch()
        build("11-compiler-only", 2, metadata, changed=None)
        time.sleep(0.1)
        (compiler / "dxcompiler.dll").touch()
        build("12-compiler-runtime-only", 2, metadata, changed=None)
        build("13-final-unchanged", 0, metadata, changed=False)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dxc", type=Path, required=True)
    parser.add_argument("--targets", type=Path, default=Path(__file__).resolve().parents[1] /
                        "SilkToolkit.ShaderBuild.targets")
    parser.add_argument("--evidence", type=Path, required=True)
    run(parser.parse_args())
