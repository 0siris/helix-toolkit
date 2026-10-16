"""Build the repository source graph without requiring an LLM API key."""

from __future__ import annotations

import os
import shutil
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
    """Build the code-only graph without the CLI's semantic-backend credential preflight."""
    from graphify.analyze import god_nodes, surprising_connections, suggest_questions
    from graphify.build import build_from_json
    from graphify.cluster import cluster, score_all
    from graphify.detect import detect
    from graphify.export import to_json
    from graphify.extract import collect_files, extract
    from graphify.report import generate

    previous_directory = Path.cwd()
    try:
        os.chdir(ROOT)
        extraction = extract(collect_files(STAGING_ROOT.relative_to(ROOT)), parallel=False)
        detection = detect(STAGING_ROOT.relative_to(ROOT))
    finally:
        os.chdir(previous_directory)
    graph = build_from_json(extraction)
    print(f"AST extracted {graph.number_of_nodes()} nodes and {graph.number_of_edges()} edges", flush=True)
    if graph.number_of_nodes() == 0:
        raise RuntimeError("The code-only AST extraction produced no graph nodes")
    communities = cluster(graph)
    cohesion = score_all(graph, communities)
    labels = {community: f"Community {community}" for community in communities}
    tokens = {"input": extraction.get("input_tokens", 0), "output": extraction.get("output_tokens", 0)}
    questions = suggest_questions(graph, communities, labels)
    report = generate(graph, communities, cohesion, labels, god_nodes(graph),
                      surprising_connections(graph, communities), detection, tokens, str(SOURCE_ROOT),
                      suggested_questions=questions)
    output = ROOT / "graphify-out"
    output.mkdir(exist_ok=True)
    to_json(graph, communities, str(GRAPH_PATH), force=True, community_labels=labels)
    (output / "GRAPH_REPORT.md").write_text(report, encoding="utf-8")
    normalize_graphify_paths()
    print(f"Graph: {graph.number_of_nodes()} nodes, {graph.number_of_edges()} edges, "
          f"{len(communities)} communities; tokens: {tokens}")


def main() -> None:
    if STAGING_ROOT.is_symlink() or STAGING_ROOT.resolve().parent != ROOT:
        raise ValueError("The temporary code corpus must remain directly inside this repository")
    copied = prepare_corpus()
    print(f"Prepared {copied} source files for graphify", flush=True)
    try:
        run_graphify()
    finally:
        if STAGING_ROOT.exists():
            shutil.rmtree(STAGING_ROOT)
    print(f"Wrote {GRAPH_PATH}")


if __name__ == "__main__":
    main()
