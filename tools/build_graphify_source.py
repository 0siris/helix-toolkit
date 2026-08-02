"""Build the repository source graph without requiring an LLM API key."""

from __future__ import annotations

import shutil
import subprocess
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SOURCE_ROOT = ROOT / "Source"
STAGING_ROOT = ROOT / ".graphify-code-corpus"
GRAPH_PATH = ROOT / "graphify-out" / "graph.json"

ALLOWED_SUFFIXES = {".cs", ".csproj", ".props", ".targets"}
EXCLUDED_DIRECTORIES = {"bin", "obj", ".vs", ".idea", "TestResults", "graphify-out"}
STAGING_PREFIX = ".graphify-code-corpus/"


def prepare_corpus() -> int:
    if STAGING_ROOT.exists():
        shutil.rmtree(STAGING_ROOT)

    copied = 0
    for source_file in SOURCE_ROOT.rglob("*"):
        if not source_file.is_file() or source_file.suffix.lower() not in ALLOWED_SUFFIXES:
            continue
        if any(part in EXCLUDED_DIRECTORIES for part in source_file.parts):
            continue

        target_file = STAGING_ROOT / source_file.relative_to(ROOT)
        target_file.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source_file, target_file)
        copied += 1
    return copied


def normalize_graphify_paths() -> None:
    output_root = ROOT / "graphify-out"
    if not GRAPH_PATH.exists():
        raise FileNotFoundError(f"Graphify did not create {GRAPH_PATH}")

    prefixes = (
        ".graphify-code-corpus/",
        ".graphify-code-corpus\\\\",
        f"{STAGING_ROOT.as_posix()}/",
        f"{STAGING_ROOT}\\\\",
    )
    for output_file in output_root.rglob("*"):
        if not output_file.is_file():
            continue
        try:
            content = output_file.read_text(encoding="utf-8")
        except UnicodeDecodeError:
            continue

        normalized = content
        for prefix in prefixes:
            normalized = normalized.replace(prefix, "")
        if normalized != content:
            output_file.write_text(normalized, encoding="utf-8")


def run_graphify() -> None:
    subprocess.run(
        ["graphify", "extract", str(STAGING_ROOT), "--no-viz", "--out", str(ROOT)],
        cwd=ROOT,
        check=True,
    )
    subprocess.run(
        ["graphify", "cluster-only", str(ROOT), "--no-viz", "--no-label"],
        cwd=ROOT,
        check=True,
    )
    normalize_graphify_paths()


def main() -> None:
    copied = prepare_corpus()
    print(f"Prepared {copied} source files for graphify")
    try:
        run_graphify()
    finally:
        if STAGING_ROOT.exists():
            shutil.rmtree(STAGING_ROOT)
    print(f"Wrote {GRAPH_PATH}")


if __name__ == "__main__":
    main()
