import io
import unittest

from Tools.anatomy_tools.fetch_atlas import (
    DESTINATION,
    EXPECTED_GIT_BLOB_SHA1,
    EXPECTED_LENGTH,
    SOURCE_URL,
    UPSTREAM_COMMIT,
    git_blob_digest,
    verify_stream,
)


class PinnedAtlasTests(unittest.TestCase):
    def test_known_git_blob_sha1(self):
        # git hash-object --stdin for 'hello' (without newline)
        self.assertEqual(
            git_blob_digest(b"hello"),
            "b6fc4c620b67d95f953a5c1c1230aaab5db5a1b0"
        )

    def test_read_stream_and_verify(self):
        data = b"Anatomical atlas synthetic test data"
        size, sha = verify_stream(io.BytesIO(data), len(data),
                                  git_blob_digest(data))
        self.assertEqual(size, len(data))
        self.assertEqual(sha, git_blob_digest(data))

    def test_reject_truncated_data(self):
        data = b"partial"
        with self.assertRaisesRegex(ValueError, "size mismatch"):
            verify_stream(io.BytesIO(data), len(data) + 1,
                          git_blob_digest(data))

    def test_reject_oversized_data(self):
        data = b"more"
        with self.assertRaisesRegex(ValueError, "exceeds pinned size"):
            verify_stream(io.BytesIO(data), len(data) - 1,
                          git_blob_digest(data))

    def test_reject_checksum_mismatch(self):
        data = b"wrong"
        with self.assertRaisesRegex(ValueError, "Git blob mismatch"):
            verify_stream(io.BytesIO(data), len(data), "f" * 40)

    def test_upstream_is_pinned_and_copyrighted_asset_not_bundled(self):
        self.assertEqual(len(UPSTREAM_COMMIT), 40)
        self.assertIn(UPSTREAM_COMMIT, SOURCE_URL)
        self.assertIn("SkeletalSystem100.fbx", SOURCE_URL)
        self.assertIn("Unity/Assets/ThirdParty", str(DESTINATION))
        self.assertEqual(EXPECTED_LENGTH, 41_339_660)
        self.assertEqual(len(EXPECTED_GIT_BLOB_SHA1), 40)


if __name__ == "__main__":
    unittest.main()
