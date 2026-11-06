"""Deterministic full-body synthetic fixtures; no MediaPipe, Unity or webcam."""
import math
import unittest

from Tools.pose_sidecar.demo import (
    BASE, LANDMARK_COUNT, SCENARIOS, synthetic_landmarks
)
from Tools.pose_sidecar.main import parse_args


class ScenarioTests(unittest.TestCase):
    def test_every_scenario_emits_33_landmarks(self):
        for scenario in SCENARIOS:
            for t in (0, 0.4, 1.2, 2.04, 3.8, 7.3):
                with self.subTest(scenario=scenario, t=t):
                    points = synthetic_landmarks(t, scenario)
                    self.assertEqual(len(points), LANDMARK_COUNT)
                    self.assertEqual(len(points[0]), 5)
                    for point in points:
                        self.assertTrue(
                            all(math.isfinite(v) for v in point.values())
                        )
                        self.assertGreaterEqual(point["x"], 0)
                        self.assertLessEqual(point["x"], 1)
                        self.assertGreaterEqual(point["y"], 0)
                        self.assertLessEqual(point["y"], 1)

    def test_subject_left_right_ids_stay_distinct(self):
        for scenario in SCENARIOS:
            p = synthetic_landmarks(0.7, scenario)
            self.assertLess(p[11]["x"], p[12]["x"])
            self.assertLess(p[23]["x"], p[24]["x"])
            self.assertLess(p[25]["x"], p[26]["x"])

    def test_wave_moves_left_wrist(self):
        self.assertNotEqual(
            synthetic_landmarks(0.0, "wave")[15]["x"],
            synthetic_landmarks(0.8, "wave")[15]["x"],
        )

    def test_walk_moves_legs_in_opposite_directions(self):
        initial = synthetic_landmarks(0, "walk")
        moving = synthetic_landmarks(0.7, "walk")
        self.assertNotEqual(initial[27]["x"], moving[27]["x"])
        self.assertNotEqual(initial[28]["x"], moving[28]["x"])
        dx_left = moving[27]["x"] - initial[27]["x"]
        dx_right = moving[28]["x"] - initial[28]["x"]
        self.assertLess(dx_left * dx_right, 0)

    def test_squat_lowers_shoulder_and_hip(self):
        initial = synthetic_landmarks(0, "squat")
        squatting = synthetic_landmarks(2, "squat")
        self.assertGreater(squatting[11]["y"], initial[11]["y"])
        self.assertGreater(squatting[23]["y"], initial[23]["y"])

    def test_dropout_is_temporary(self):
        before = synthetic_landmarks(1.9, "dropout")
        during = synthetic_landmarks(2.06, "dropout")
        after = synthetic_landmarks(2.3, "dropout")
        self.assertEqual(before[11]["visibility"], 1)
        self.assertEqual(during[11]["visibility"], 0)
        self.assertEqual(during[25]["presence"], 0)
        self.assertEqual(after[11]["visibility"], 1)

    def test_same_time_same_frame(self):
        self.assertEqual(
            synthetic_landmarks(3.14159, "walk"),
            synthetic_landmarks(3.14159, "walk"),
        )

    def test_bad_scenario_rejected(self):
        with self.assertRaises(ValueError):
            synthetic_landmarks(0, "teleport")
        with self.assertRaises(SystemExit):
            parse_args(["--demo-scenario", "teleport"])

    def test_cli_accepts_scenario(self):
        args = parse_args(["--demo", "--demo-scenario", "squat"])
        self.assertTrue(args.demo)
        self.assertEqual(args.demo_scenario, "squat")

    def test_no_false_pose_at_unused_face_indices(self):
        p = synthetic_landmarks(0)
        for i in (1, 2, 3, 8, 9):
            self.assertEqual(p[i]["presence"], 0.0)
        for i in (0, 11, 12, 13, 14, 15, 16, 23, 24, 27, 28, 31, 32):
            self.assertEqual(p[i]["visibility"], 1.0)


if __name__ == "__main__":
    unittest.main()
