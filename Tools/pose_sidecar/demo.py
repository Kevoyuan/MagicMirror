"""Deterministic skeleton for transport/UI development without camera or ML."""
from __future__ import annotations

import math


def synthetic_landmarks(t: float) -> list[dict[str, float]]:
    # Normalized image coordinates (unmirrored). Most landmark points are
    # intentionally invisible; the torso, arms and legs animate.
    points = [
        dict(x=0.5, y=0.5, z=0.0, visibility=0.0, presence=0.0)
        for _ in range(33)
    ]
    positions = {
        0: (0.50, 0.16), 11: (0.42, 0.30), 12: (0.58, 0.30),
        13: (0.34, 0.43), 14: (0.66, 0.43),
        15: (0.28, 0.58), 16: (0.72, 0.58),
        23: (0.45, 0.58), 24: (0.55, 0.58),
        25: (0.43, 0.75), 26: (0.57, 0.75),
        27: (0.42, 0.93), 28: (0.58, 0.93)
    }
    wave = 0.12 * math.sin(t * 2)
    positions[15] = (0.29 + wave, 0.49 - abs(wave))
    positions[13] = (0.34, 0.41 - 0.5 * abs(wave))
    for index, (x, y) in positions.items():
        points[index] = dict(x=x, y=y, z=0.0, visibility=1.0, presence=1.0)
    return points
