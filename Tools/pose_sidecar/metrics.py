"""Bounded local-only processing-time statistics.

These timings measure Python inference and JPEG encoding, *not* webcam-to-screen
latency: Unity and Python use different clocks and there is still transport,
decode, render, and display time to account for.
"""
from __future__ import annotations

from collections import deque
import math
import statistics


def percentile(values: list[float], fraction: float) -> float:
    """Nearest-rank percentile (non-interpolated) on a non-empty sample."""
    if not values or not 0 < fraction <= 1:
        raise ValueError("Expected non-empty samples and a percentile in (0, 1]")
    ordered = sorted(values)
    return ordered[max(0, math.ceil(len(ordered) * fraction) - 1)]


class ProcessingStats:
    def __init__(self, report_every: int = 120, window: int = 240):
        if report_every < 0 or window < 1:
            raise ValueError("Invalid statistics configuration")
        self.report_every = report_every
        self.inference_ms = deque(maxlen=window)
        self.jpeg_ms = deque(maxlen=window)
        self.frames = 0

    def add(self, inference_ms: float, jpeg_ms: float) -> str | None:
        if not (math.isfinite(inference_ms) and math.isfinite(jpeg_ms)):
            raise ValueError("Non-finite processing time")
        if inference_ms < 0 or jpeg_ms < 0:
            raise ValueError("Negative processing time")
        self.inference_ms.append(inference_ms)
        self.jpeg_ms.append(jpeg_ms)
        self.frames += 1
        if self.report_every == 0 or self.frames % self.report_every:
            return None
        infer = list(self.inference_ms)
        jpeg = list(self.jpeg_ms)
        return (f"Processed {self.frames} frames (recent {len(infer)}): "
                f"inference mean={statistics.fmean(infer):.1f}ms "
                f"p95={percentile(infer, 0.95):.1f}ms; "
                f"JPEG mean={statistics.fmean(jpeg):.1f}ms "
                f"p95={percentile(jpeg, 0.95):.1f}ms. "
                "Not end-to-end display latency.")
