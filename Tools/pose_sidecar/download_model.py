"""Download official MediaPipe Pose Landmarker task bundles into local gitignored storage.

The 'latest' Google URLs are NOT immutable version pins; after platform testing,
record/pin SHA-256 checksums separately for release reproducibility.
"""
from __future__ import annotations

import argparse
import pathlib
import urllib.request

MODEL_URLS = {
    name: ("https://storage.googleapis.com/mediapipe-models/"
           f"pose_landmarker/pose_landmarker_{name}/float16/latest/"
           f"pose_landmarker_{name}.task")
    for name in ("lite", "full", "heavy")
}
MODEL_DIR = pathlib.Path(__file__).parent / "models"
MAX_MODEL_BYTES = 75_000_000

# Backwards-compatible aliases for existing Lite setup code.
MODEL_URL = MODEL_URLS["lite"]
DEST = MODEL_DIR / "pose_landmarker_lite.task"


def model_path(profile: str = "lite") -> pathlib.Path:
    if profile not in MODEL_URLS:
        raise ValueError(f"Unknown MediaPipe model profile: {profile!r}")
    return MODEL_DIR / f"pose_landmarker_{profile}.task"


def download(profile: str = "lite") -> pathlib.Path:
    destination = model_path(profile)
    destination.parent.mkdir(parents=True, exist_ok=True)
    if destination.exists():
        if not 100_000 < destination.stat().st_size <= MAX_MODEL_BYTES:
            raise ValueError(f"Existing model has unexpected size: {destination}")
        print("Model already present:", destination)
        return destination

    url = MODEL_URLS[profile]
    print(f"Fetching {profile} MediaPipe model from:", url, flush=True)
    request = urllib.request.Request(url, headers={"User-Agent": "MagicMirror-Model-Setup/2.0"})
    temporary = destination.with_suffix(".partial")
    try:
        with urllib.request.urlopen(request, timeout=90) as response:
            payload = response.read(MAX_MODEL_BYTES + 1)
        if not 100_000 < len(payload) <= MAX_MODEL_BYTES:
            raise ValueError(f"Unexpected {profile} model size: {len(payload)} bytes")
        temporary.write_bytes(payload)
        temporary.replace(destination)
    finally:
        if temporary.exists():
            temporary.unlink()
    print("Saved model:", destination, "bytes:", len(payload), flush=True)
    return destination


def main(argv=None):
    parser = argparse.ArgumentParser(description="Download official MediaPipe Pose Landmarker bundle(s)")
    parser.add_argument("--profile", choices=tuple(MODEL_URLS), default="lite")
    parser.add_argument("--all", action="store_true", help="Download Lite, Full and Heavy")
    args = parser.parse_args(argv)
    for profile in (MODEL_URLS if args.all else (args.profile,)):
        download(profile)


if __name__ == "__main__":
    main()
