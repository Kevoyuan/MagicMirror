"""Download the official MediaPipe Pose Landmarker Lite task bundle.

Model is stored locally only; copyright and licensing remain with upstream.
"""
from __future__ import annotations

import pathlib
import urllib.request

MODEL_URL = (
    "https://storage.googleapis.com/mediapipe-models/"
    "pose_landmarker/pose_landmarker_lite/float16/latest/pose_landmarker_lite.task"
)
DEST = pathlib.Path(__file__).parent / "models" / "pose_landmarker_lite.task"


def main():
    DEST.parent.mkdir(parents=True, exist_ok=True)
    if DEST.exists():
        print("Model already present:", DEST)
        return
    print("Fetching MediaPipe model from:", MODEL_URL)
    request = urllib.request.Request(MODEL_URL, headers={"User-Agent": "MagicMirror-Model-Setup/1.0"})
    with urllib.request.urlopen(request, timeout=40) as response:
        payload = response.read(30_000_001)
    if not 100_000 < len(payload) <= 30_000_000:
        raise ValueError("Unexpected downloaded model size")
    temp = DEST.with_suffix(".partial")
    temp.write_bytes(payload)
    temp.replace(DEST)
    print("Saved model:", DEST, "bytes:", len(payload))


if __name__ == "__main__":
    main()
