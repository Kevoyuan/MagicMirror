"""Blender-only FBX inventory for Z-Anatomy source review.

Example:
  blender --background --python Tools/anatomy_tools/inspect_fbx.py -- \
      --input path/to/SkeletalSystem100.fbx --output /tmp/anatomy.json

This tool never exports/copies/repackages meshes. It writes metadata only.
Candidate names are hints, NOT anatomical verification.
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path
import sys

# Running this script via Blender does not guarantee repository root on sys.path.
ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT))
from Tools.anatomy_tools.rules import suggest  # noqa: E402


def arguments():
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    return parser.parse_args(args)


def main():
    import bpy  # Imported only when actually running under Blender.

    opts = arguments()
    if not opts.input.is_file():
        raise SystemExit(f"Missing FBX: {opts.input}")
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(opts.input.resolve()))

    objects = []
    for obj in bpy.data.objects:
        if obj.type != "MESH":
            continue

        stats = suggest(obj.name)
        items = {
            "name": obj.name,
            "type": obj.type,
            "parent": obj.parent.name if obj.parent else None,
            "meshDataName": obj.data.name if obj.data else None,
            "vertexCount": len(obj.data.vertices) if obj.data else 0,
            "triangleCount": sum(len(poly.vertices) - 2 for poly in obj.data.polygons),
            "dimensionsBlenderUnits": [float(v) for v in obj.dimensions],
            "worldMatrixRows": [[float(v) for v in row] for row in obj.matrix_world],
            "materialNames": [x.name if x else None for x in obj.data.materials],
            **stats,
        }
        objects.append(items)

    output = {
        "source": str(opts.input.name),
        "description": (
            "Mesh metadata only. Bone/side suggestions require manual anatomy "
            "review and license validation before use."
        ),
        "meshCount": len(objects),
        "armNameCandidates": sum(1 for obj in objects if obj["boneHint"]),
        "objects": sorted(objects, key=lambda item: item["name"].casefold()),
    }
    opts.output.parent.mkdir(parents=True, exist_ok=True)
    opts.output.write_text(json.dumps(output, indent=2), encoding="utf-8")
    print("Saved", len(objects), "mesh records to", opts.output)


if __name__ == "__main__":
    main()
