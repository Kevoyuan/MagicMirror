"""Profile, CLI and processing-metric tests that require no MediaPipe wheel."""
import io
import unittest
from contextlib import redirect_stderr

from Tools.pose_sidecar.download_model import MODEL_URLS, model_path
from Tools.pose_sidecar.main import make_header, parse_args
from Tools.pose_sidecar.metrics import ProcessingStats, percentile
from Tools.pose_sidecar.protocol import encode_packet, roundtrip


class ModelProfileTests(unittest.TestCase):
    def test_profiles_are_distinct_official_task_paths(self):
        self.assertEqual(set(MODEL_URLS), {"lite", "full", "heavy"})
        for name, url in MODEL_URLS.items():
            self.assertTrue(url.startswith("https://storage.googleapis.com/mediapipe-models/pose_landmarker/"))
            self.assertTrue(url.endswith(f"pose_landmarker_{name}.task"))
            self.assertEqual(model_path(name).name, f"pose_landmarker_{name}.task")

    def test_default_and_custom_selection(self):
        defaults = parse_args([])
        self.assertEqual(defaults.model_profile, "lite")
        self.assertIsNone(defaults.model)
        self.assertEqual(defaults.min_detection_confidence, 0.5)
        args = parse_args(["--model-profile", "heavy", "--min-tracking-confidence", "0.8",
                           "--model", "/tmp/custom.task", "--stats-every", "3"])
        self.assertEqual(args.model_profile, "heavy")
        self.assertEqual(args.model, "/tmp/custom.task")
        self.assertEqual(args.min_tracking_confidence, 0.8)
        self.assertEqual(args.stats_every, 3)

    def test_rejects_invalid_thresholds_and_interval(self):
        for cli in (["--min-presence-confidence", "-0.1"],
                    ["--min-detection-confidence", "1.1"],
                    ["--min-tracking-confidence", "nan"],
                    ["--stats-every", "-1"]):
            with self.subTest(cli=cli), redirect_stderr(io.StringIO()), self.assertRaises(SystemExit):
                parse_args(cli)

    def test_optional_diagnostics_remain_protocol_v1(self):
        header = make_header(3, 789, 1280, 720, [], [], demo=False,
                             model_profile="full", inference_ms=13.2, jpeg_encode_ms=2.1)
        decoded, _ = roundtrip(encode_packet(header, b"jpeg"))
        self.assertEqual(decoded["version"], 1)
        self.assertEqual(decoded["modelProfile"], "full")
        self.assertEqual(decoded["inferenceMs"], 13.2)
        self.assertEqual(decoded["jpegEncodeMs"], 2.1)


class MetricsTests(unittest.TestCase):
    def test_nearest_rank(self):
        self.assertEqual(percentile([1, 2, 3, 4, 5], 0.95), 5)
        self.assertEqual(percentile([1, 2, 3, 4, 5], 0.5), 3)

    def test_bounded_interval_summary(self):
        stats = ProcessingStats(report_every=2, window=2)
        self.assertIsNone(stats.add(10, 1))
        summary = stats.add(20, 3)
        self.assertIn("inference mean=15.0ms p95=20.0ms", summary)
        self.assertIn("Not end-to-end display latency", summary)
        self.assertIsNone(stats.add(30, 5))
        self.assertEqual(list(stats.inference_ms), [20, 30])

    def test_disabled_summary(self):
        stats = ProcessingStats(report_every=0)
        for _ in range(10):
            self.assertIsNone(stats.add(4.5, 1.2))


if __name__ == "__main__":
    unittest.main()
