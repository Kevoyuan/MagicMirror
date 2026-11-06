"""Generate a synthetic JPEG video to validate Unity image/pose compositing.

Requires opencv-python and numpy, but NEVER imports MediaPipe or opens a camera.
The frame number and landmark positions are from exactly the same synthetic
pose sample. The bright reference dots should coincide with Unity's overlay.
"""
from __future__ import annotations


# MediaPipe Pose body skeleton connections (face-heavy detail omitted).
EDGES = (
    (11, 12), (11, 13), (13, 15), (12, 14), (14, 16),
    (11, 23), (12, 24), (23, 24),
    (23, 25), (25, 27), (24, 26), (26, 28),
    (27, 31), (28, 32),
)


def render_demo_jpeg(
    landmarks: list[dict],
    frame_id: int,
    scenario: str = "wave",
    quality: int = 72,
    width: int = 1280,
    height: int = 720,
) -> bytes:
    """Render a diagnostic JPEG in unmirrored source image coordinates."""
    try:
        import cv2
        import numpy as np
    except ImportError as exc:
        raise RuntimeError(
            "Synthetic JPEG preview requires opencv-python and numpy. "
            "Install those two packages; MediaPipe is not required."
        ) from exc

    if len(landmarks) != 33:
        raise ValueError("Synthetic preview requires the MediaPipe 33-point schema")
    if width <= 0 or height <= 0:
        raise ValueError("Invalid output image dimensions")

    frame = np.zeros((height, width, 3), dtype=np.uint8)
    frame[:] = (32, 27, 24)  # BGR dark background
    cv2.line(frame, (width // 2, 0), (width // 2, height), (65, 65, 65), 1)
    cv2.putText(
        frame, f"SYNTHETIC SOURCE FRAME {frame_id:06d} | {scenario.upper()}",
        (20, 44), cv2.FONT_HERSHEY_SIMPLEX, 0.9, (225, 225, 225), 2
    )
    cv2.putText(
        frame, "Source image: NOT flipped. Compare blue dots with Unity pose overlay.",
        (20, 77), cv2.FONT_HERSHEY_SIMPLEX, 0.55, (190, 190, 190), 1
    )

    def point(index: int):
        item = landmarks[index]
        if min(item["visibility"], item["presence"]) < 0.5:
            return None
        return (round(item["x"] * width), round(item["y"] * height))

    for start, end in EDGES:
        a, b = point(start), point(end)
        if a is not None and b is not None:
            cv2.line(frame, a, b, (170, 90, 90), 2, cv2.LINE_AA)
    for index in (11, 12, 13, 14, 15, 16, 23, 24, 25, 26, 27, 28):
        xy = point(index)
        if xy is not None:
            cv2.circle(frame, xy, 7, (255, 170, 15), -1, cv2.LINE_AA)

    # Source-left is anatomical LEFT: on an unmirrored front-facing source
    # image it can appear on the viewer's left depending on camera conventions.
    # Labels identify MediaPipe IDs, not the physical camera's orientation.
    for index, label in ((11, "MP 11"), (12, "MP 12"),
                         (15, "MP 15"), (16, "MP 16")):
        xy = point(index)
        if xy is not None:
            cv2.putText(frame, label, (xy[0] + 12, xy[1] - 8),
                        cv2.FONT_HERSHEY_SIMPLEX, 0.45, (225, 225, 225), 1)

    ok, encoded = cv2.imencode(
        ".jpg", frame, [int(cv2.IMWRITE_JPEG_QUALITY), quality]
    )
    if not ok:
        raise RuntimeError("Synthetic JPEG encoding failed")
    return encoded.tobytes()
