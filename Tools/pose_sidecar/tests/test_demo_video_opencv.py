"""OpenCV-only optional image-codec check for the synthetic M2 video source.

The core pose test suite remains dependency-free; CI installs OpenCV in a
separate job to exercise the actual JPEG encoder and decoder.
"""
import importlib.util
import unittest

from Tools.pose_sidecar.demo import synthetic_landmarks
from Tools.pose_sidecar.demo_video import render_demo_jpeg

HAVE_CV2 = (importlib.util.find_spec("cv2") is not None and
            importlib.util.find_spec("numpy") is not None)


@unittest.skipUnless(HAVE_CV2, "OpenCV+NumPy not installed in stdlib-only environment")
class SyntheticVideoCodecTests(unittest.TestCase):
    def test_jpeg_decodes_with_expected_dimensions_and_landmark_marker(self):
        import cv2
        import numpy as np

        points = synthetic_landmarks(0.25, "wave")
        payload = render_demo_jpeg(points, frame_id=13, scenario="wave")
        self.assertTrue(payload.startswith(b"\xff\xd8"))
        image = cv2.imdecode(np.frombuffer(payload, dtype=np.uint8),
                             cv2.IMREAD_COLOR)
        self.assertIsNotNone(image)
        self.assertEqual(image.shape, (720, 1280, 3))
        shoulder = points[11]
        pixel = image[round(shoulder["y"] * 720),
                      round(shoulder["x"] * 1280)].astype(int)
        # OpenCV stores BGR. JPEG is lossy, so use a moderate tolerance.
        self.assertLess(int(np.abs(pixel - [255, 170, 15]).max()), 65)

    def test_dropout_frame_still_decodes_without_pose(self):
        import cv2
        import numpy as np

        invisible = synthetic_landmarks(2.1, "dropout")
        self.assertTrue(all(point["presence"] == 0.0 for point in invisible))
        image = cv2.imdecode(
            np.frombuffer(render_demo_jpeg(invisible, frame_id=14,
                                           scenario="dropout"), dtype=np.uint8),
            cv2.IMREAD_COLOR)
        self.assertIsNotNone(image)
        self.assertEqual(image.shape[:2], (720, 1280))


if __name__ == "__main__":
    unittest.main()
