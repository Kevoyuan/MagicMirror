import unittest

from Tools.anatomy_tools.rules import hint_bone, hint_side, normalize_words, suggest


class RulesTests(unittest.TestCase):
    def test_camelcase_and_separator(self):
        self.assertEqual(normalize_words("LeftHumerus_mesh.01"),
                         ("left", "humerus", "mesh"))

    def test_bone_and_side(self):
        self.assertEqual(suggest("Right_Radius.001"),
                         {"boneHint": "radius", "sideHint": "right"})
        self.assertEqual(suggest("Ulna_Sinister"),
                         {"boneHint": "ulna", "sideHint": "left"})

    def test_numeric_unidentified(self):
        self.assertIsNone(hint_bone("FMA12345"))
        self.assertIsNone(hint_side("FMA12345"))

    def test_ambiguous_side_is_unknown(self):
        self.assertIsNone(hint_side("left_right_ulna"))

    def test_no_false_match_on_word_fragment(self):
        self.assertIsNone(hint_bone("ulnar_artery"))
        self.assertIsNone(hint_bone("radial_head_marker"))
        self.assertIsNone(hint_bone("humeral_landmark"))


if __name__ == "__main__":
    unittest.main()
