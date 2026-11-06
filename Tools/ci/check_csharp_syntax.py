"""Parse MagicMirror Unity C# files without pretending to compile Unity assemblies.

Requires tree-sitter + tree-sitter-c-sharp. Fails on syntax ERROR/MISSING nodes.
No Unity Engine APIs, type references or runtime behaviors are checked here.
"""
from __future__ import annotations

from pathlib import Path
import sys

from tree_sitter import Language, Parser
import tree_sitter_c_sharp


ROOT = Path(__file__).resolve().parents[2]
SCRIPTS = ROOT / "Unity/Assets"


def problems(node):
    if node.type == "ERROR" or node.is_missing:
        yield node
    for child in node.children:
        if child.has_error or child.is_missing or child.type == "ERROR":
            yield from problems(child)


def main():
    parser = Parser(Language(tree_sitter_c_sharp.language()))
    sources = sorted(SCRIPTS.rglob("*.cs"))
    if not sources:
        raise SystemExit("No Unity source scripts found")

    failures = []
    for path in sources:
        raw = path.read_bytes()
        tree = parser.parse(raw)
        if tree.root_node.has_error:
            locations = []
            for node in problems(tree.root_node):
                line, col = node.start_point
                locations.append(f"{line + 1}:{col + 1} {node.type}")
            failures.append((path, locations[:15]))

    for path, locations in failures:
        print(f"C# parse errors in: {path.relative_to(ROOT)}")
        for location in locations:
            print("  ", location)

    print(f"Parsed {len(sources)} Unity C# source files.")
    if failures:
        raise SystemExit(f"C# syntax parsing failed in {len(failures)} file(s).")
    print("Syntax tree check passed (NOT Unity C# compilation).")


if __name__ == "__main__":
    main()
