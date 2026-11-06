"""Actual Python sidecar process → localhost TCP → packet decoder smoke tests.

No Unity Editor or webcam is required. The default mode uses only Python's
standard library. The optional JPEG case needs OpenCV/NumPy, not MediaPipe.
This tests the REAL producer loop, fragmentation and reconnection rather than
only a synthetic encoder helper.
"""
from __future__ import annotations

import importlib.util
from pathlib import Path
import socket
import subprocess
import sys
import time
import unittest

from Tools.pose_sidecar.protocol import decode_packet

ROOT = Path(__file__).resolve().parents[3]
HAVE_OPENCV = (importlib.util.find_spec("cv2") is not None and
               importlib.util.find_spec("numpy") is not None)


def unused_loopback_port() -> int:
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
        sock.bind(("127.0.0.1", 0))
        return sock.getsockname()[1]


class SidecarProcessHarness:
    def __init__(self, *, jpeg: bool = False):
        self.port = unused_loopback_port()
        self.jpeg = jpeg
        self.proc = None

    def __enter__(self):
        argv = [sys.executable, "-m", "Tools.pose_sidecar.main",
                "--port", str(self.port), "--fps", "30",
                "--demo-video" if self.jpeg else "--demo",
                "--demo-scenario", "walk"]
        self.proc = subprocess.Popen(
            argv, cwd=ROOT, stdout=subprocess.DEVNULL, stderr=subprocess.PIPE
        )
        return self

    def connect(self):
        deadline = time.monotonic() + 8
        while time.monotonic() < deadline:
            if self.proc.poll() is not None:
                self._fail("Sidecar exited before opening TCP listener")
            try:
                sock = socket.create_connection(("127.0.0.1", self.port), 0.3)
                sock.settimeout(4)
                return sock
            except (ConnectionRefusedError, OSError):
                time.sleep(0.05)
        self._fail("Timed out waiting for sidecar to listen on loopback")

    def _fail(self, message):
        code = self.proc.poll()
        raise AssertionError(f"{message} (exit={code})")

    def __exit__(self, exc_type, exc, tb):
        if self.proc is not None:
            self.proc.terminate()
            try:
                self.proc.communicate(timeout=4)
            except subprocess.TimeoutExpired:
                self.proc.kill()
                self.proc.communicate(timeout=4)


class RealSidecarTcpTests(unittest.TestCase):
    def assert_sequence(self, stream, count: int, jpeg: bool):
        previous_frame_id = -1
        previous_timestamp = -1
        for _ in range(count):
            header, payload = decode_packet(stream)
            self.assertEqual(header["version"], 1)
            self.assertTrue(header["demo"])
            self.assertEqual(header["width"], 1280)
            self.assertEqual(header["height"], 720)
            self.assertEqual(len(header["landmarks"]), 33)
            self.assertFalse(header["mirrored"])
            self.assertGreater(header["frameId"], previous_frame_id)
            self.assertGreater(header["timestampMs"], previous_timestamp)
            if jpeg:
                self.assertTrue(payload.startswith(b"\xff\xd8"),
                                "Expected synthetic JPEG from same packet")
                self.assertTrue(payload.endswith(b"\xff\xd9"))
            else:
                self.assertEqual(payload, b"")
            previous_frame_id = header["frameId"]
            previous_timestamp = header["timestampMs"]
        return previous_frame_id

    def test_live_stdlib_demo_and_reconnect(self):
        with SidecarProcessHarness() as server:
            with server.connect() as sock:
                with sock.makefile("rb") as stream:
                    self.assertEqual(self.assert_sequence(stream, 5, False), 4)

            # Restarting the TCP client creates a fresh producer generator.
            # Its frame IDs intentionally restart at zero; the Unity guard
            # must track these as a new connection/session.
            with server.connect() as sock:
                with sock.makefile("rb") as stream:
                    self.assertEqual(self.assert_sequence(stream, 4, False), 3)

    @unittest.skipUnless(HAVE_OPENCV, "OpenCV/NumPy needed for demo-video")
    def test_actual_jpeg_stream_packet_pairing(self):
        with SidecarProcessHarness(jpeg=True) as server:
            with server.connect() as sock:
                with sock.makefile("rb") as stream:
                    self.assertEqual(self.assert_sequence(stream, 3, True), 2)


if __name__ == "__main__":
    unittest.main()
