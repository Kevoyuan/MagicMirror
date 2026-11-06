"""Reproducible 33-landmark exercise scenarios for full-body AR QA.

These are SCHEMATIC tracking fixtures, not anatomical models.
The returned coordinates are the unmirrored source image coordinates.
"""
from __future__ import annotations

import math

LANDMARK_COUNT = 33

# MediaPipe normalized image landmark indices. Full-body joints are supplied
# so that Phase 4 can test arms, torso, thighs, shins, feet, and hands.
BASE = {
    0: (0.50, 0.16),
    11: (0.42, 0.30), 12: (0.58, 0.30),
    13: (0.34, 0.43), 14: (0.66, 0.43),
    15: (0.28, 0.58), 16: (0.72, 0.58),
    17: (0.26, 0.60), 18: (0.74, 0.60),
    19: (0.25, 0.62), 20: (0.75, 0.62),
    21: (0.27, 0.62), 22: (0.73, 0.62),
    23: (0.45, 0.58), 24: (0.55, 0.58),
    25: (0.43, 0.75), 26: (0.57, 0.75),
    27: (0.42, 0.92), 28: (0.58, 0.92),
    29: (0.42, 0.94), 30: (0.58, 0.94),
    31: (0.41, 0.98), 32: (0.59, 0.98),
}

SCENARIOS = ("wave", "walk", "squat", "dropout")


def synthetic_landmarks(t: float, scenario: str = "wave") -> list[dict[str, float]]:
    """Return deterministic MediaPipe-style landmarks for a given time/scenario.

    Each pose contains 33 entries, with invalid face-only landmarks marked
    invisible rather than filled with invented anatomically meaningful points.
    """
    if scenario not in SCENARIOS:
        raise ValueError(f"Unknown demo scenario: {scenario}")
    t = max(0.0, float(t))

    positions = dict(BASE)

    if scenario in ("wave", "dropout"):
        swing = 0.12 * math.sin(t * 2)
        positions[13] = (0.34, 0.41 - 0.50 * abs(swing))
        positions[15] = (0.29 + swing, 0.49 - abs(swing))
        # Keep synthetic palm/finger markers near their wrist.
        hand_x, hand_y = positions[15]
        positions[17] = (hand_x - 0.02, hand_y + 0.02)
        positions[19] = (hand_x - 0.04, hand_y + 0.04)
        positions[21] = (hand_x - 0.01, hand_y + 0.04)

    elif scenario == "walk":
        phase = math.sin(t * 2.4)
        phase_opposite = -phase
        for hip, knee, ankle, heel, toe, stride in (
            (23, 25, 27, 29, 31, phase),
            (24, 26, 28, 30, 32, phase_opposite),
        ):
            hx, hy = positions[hip]
            positions[knee] = (hx + 0.045 * stride, 0.75 + 0.015 * abs(stride))
            positions[ankle] = (hx + 0.090 * stride, 0.92 - 0.040 * max(0, stride))
            ax, ay = positions[ankle]
            positions[heel] = (ax, ay + 0.015)
            positions[toe] = (ax + (-0.015 if stride < 0 else 0.015), ay + 0.030)

        # Counter-swing both arms, preserving shoulder anchors.
        positions[13] = (0.35 + 0.045 * phase_opposite, 0.43)
        positions[15] = (0.29 + 0.07 * phase_opposite, 0.59)
        positions[14] = (0.65 + 0.045 * phase, 0.43)
        positions[16] = (0.71 + 0.07 * phase, 0.59)

    elif scenario == "squat":
        down = (1 - math.cos(t * 1.5)) * 0.5
        # Position change is intentionally modest: synthetic tracking test,
        # not faithful biomechanical squat kinematics.
        for index in (0, 11, 12, 13, 14, 15, 16, 23, 24):
            x, y = positions[index]
            positions[index] = (x, y + down * 0.11)
        for index in (25, 26):
            x, y = positions[index]
            positions[index] = (x + (0.03 if index == 25 else -0.03) * down,
                                y + down * 0.065)
        for index in (27, 28, 29, 30, 31, 32):
            x, y = positions[index]
            positions[index] = (x, y - down * 0.005)

    points = [
        dict(x=0.5, y=0.5, z=0.0, visibility=0.0, presence=0.0)
        for _ in range(LANDMARK_COUNT)
    ]

    for index, (x, y) in positions.items():
        points[index] = dict(
            x=float(x), y=float(y), z=0.0,
            visibility=1.0, presence=1.0
        )

    # A reproducible 180-ms pose-tracking dropout every 5 seconds.
    # Entire body disappears: full atlas should hold briefly and then recover.
    if scenario == "dropout" and 2.0 <= t % 5.0 < 2.18:
        for point in points:
            point["visibility"] = 0.0
            point["presence"] = 0.0

    return points
