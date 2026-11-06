"""Validate synthetic image/pose pairing without installing OpenCV or MediaPipe."""
from contextlib import redirect_stderr
import io
import unittest

from Tools.pose_sidecar.main import demo_packets, parse_args
from Tools.pose_sidecar.protocol import roundtrip


class DemoVideoTests(unittest.TestCase):
    def test_exact_pose_used_by_optional_video_encoder(self):
        source_poses = []

        def fake_jpeg_encoder(points, frame_id):
            source_poses.append((points, frame_id))
            return b"FAKE_JPEG_FRAME_" + str(frame_id).encode("ascii")

        generator = demo_packets(20, "wave", image_encoder=fake_jpeg_encoder)
        try:
            header, image = roundtrip(next(generator))
        finally:
            generator.close()
        self.assertEqual(header["frameId"], source_poses[0][1])
        self.assertEqual(header["landmarks"], source_poses[0][0])
        self.assertEqual(len(header["landmarks"]), 33)
        self.assertEqual(image, b"FAKE_JPEG_FRAME_0")
        self.assertTrue(header["demo"])
        self.assertFalse(header["mirrored"])

    def test_original_dependency_free_demo_unchanged(self):
        generator = demo_packets(20, "walk")
        try:
            header, image = roundtrip(next(generator))
        finally:
            generator.close()
        self.assertEqual(image, b"")
        self.assertEqual(len(header["landmarks"]), 33)
        self.assertTrue(header["demo"])

    def test_demo_modes_are_mutually_exclusive(self):
        self.assertTrue(parse_args(["--demo-video"]).demo_video)
        with redirect_stderr(io.StringIO()), self.assertRaises(SystemExit):
            parse_args(["--demo", "--demo-video"])


if __name__ == "__main__":
    unittest.main()
