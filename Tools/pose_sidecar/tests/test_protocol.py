"""No webcam or third-party dependencies required for these checks."""
import io
import json
import struct
import unittest

from Tools.pose_sidecar.demo import synthetic_landmarks
from Tools.pose_sidecar.main import make_header, parse_args
from Tools.pose_sidecar.protocol import (
    MAX_HEADER, MAX_JPEG, VERSION, decode_packet, encode_packet, roundtrip
)


class ProtocolTests(unittest.TestCase):
    def setUp(self):
        self.header = make_header(
            4, 1500, 1280, 720, synthetic_landmarks(0), [], demo=True
        )

    def test_roundtrip_empty_image_demo(self):
        header, jpg = roundtrip(encode_packet(self.header))
        self.assertEqual(jpg, b"")
        self.assertEqual(header["version"], VERSION)
        self.assertEqual(header["frameId"], 4)
        self.assertEqual(len(header["landmarks"]), 33)
        self.assertFalse(header["mirrored"])

    def test_jpeg_roundtrip(self):
        header, jpg = roundtrip(encode_packet(self.header, b"\xff\xd8\xff\xd9"))
        self.assertEqual(header["timestampMs"], 1500)
        self.assertEqual(jpg, b"\xff\xd8\xff\xd9")

    def test_rejects_non_finite(self):
        self.header["landmarks"][0]["x"] = float("nan")
        with self.assertRaises(ValueError):
            encode_packet(self.header)

    def test_unsupported_version(self):
        self.header["version"] = 99
        with self.assertRaises(ValueError):
            encode_packet(self.header)

    def test_rejects_oversized_frame(self):
        with self.assertRaises(ValueError):
            encode_packet(self.header, bytes(MAX_JPEG + 1))

    def test_rejects_oversized_header(self):
        invalid = struct.pack(">I", MAX_HEADER + 1)
        with self.assertRaises(ValueError):
            decode_packet(io.BytesIO(invalid))

    def test_rejects_truncated_header(self):
        raw = encode_packet(self.header)
        with self.assertRaises(EOFError):
            decode_packet(io.BytesIO(raw[:8]))

    def test_rejects_truncated_image(self):
        raw = encode_packet(self.header, b"0123456789")
        with self.assertRaises(EOFError):
            decode_packet(io.BytesIO(raw[:-5]))

    def test_landmark_indices(self):
        points = synthetic_landmarks(0)
        for index in (11, 12, 13, 14, 15, 16, 23, 24, 25, 26, 27, 28):
            self.assertEqual(points[index]["presence"], 1.0)
        self.assertEqual(points[1]["presence"], 0.0)

    def test_arg_validation(self):
        with self.assertRaises(SystemExit):
            parse_args(["--fps", "0"])
        args = parse_args(["--demo", "--port", "8765"])
        self.assertTrue(args.demo)


if __name__ == "__main__":
    unittest.main()
