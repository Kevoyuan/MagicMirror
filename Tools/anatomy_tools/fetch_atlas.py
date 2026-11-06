"""Fetch one pinned, complete Z-Anatomy reference skeleton for LOCAL Unity use.

No mesh extraction or re-export. Do not redistribute fetched FBX without
source-specific attribution, share-alike and license review.

Usage:
    python3 -m Tools.anatomy_tools.fetch_atlas --download
    python3 -m Tools.anatomy_tools.fetch_atlas --verify-only
"""
from __future__ import annotations

import argparse
import hashlib
from pathlib import Path
import sys
import urllib.request

# Pin a source COMMIT, not the mutable PC-Version branch.
UPSTREAM_REPO = "LluisV/Z-Anatomy"
UPSTREAM_COMMIT = "6c7f9016bd5899ac8edafd31b9900c151df42ed6"
UPSTREAM_FILE = "Resources/Models/FBX/SkeletalSystem100.fbx"
EXPECTED_LENGTH = 41_339_660
EXPECTED_GIT_BLOB_SHA1 = "7c62e45211bf3992bb7239170343e06e2b073865"
SOURCE_URL = (
    f"https://raw.githubusercontent.com/{UPSTREAM_REPO}/"
    f"{UPSTREAM_COMMIT}/{UPSTREAM_FILE}"
)
REPO_ROOT = Path(__file__).resolve().parents[2]
DESTINATION = (
    REPO_ROOT / "Unity/Assets/ThirdParty/ZAnatomy/SkeletalSystem100.fbx"
)
BLOCK_SIZE = 256 * 1024


def git_blob_digest(data: bytes) -> str:
    """Git hashes (b'blob ' + len + NUL + bytes), not raw file bytes alone."""
    h = hashlib.sha1()
    h.update(f"blob {len(data)}\0".encode("ascii"))
    h.update(data)
    return h.hexdigest()


def verify_stream(readable, expected_length: int,
                  expected_sha1: str) -> tuple[int, str]:
    """Hash a byte stream without buffering a large anatomical model in RAM."""
    h = hashlib.sha1()
    h.update(f"blob {expected_length}\0".encode("ascii"))
    received = 0
    while True:
        chunk = readable.read(BLOCK_SIZE)
        if not chunk:
            break
        received += len(chunk)
        if received > expected_length:
            raise ValueError("File exceeds pinned size; aborting")
        h.update(chunk)
    digest = h.hexdigest()
    if received != expected_length:
        raise ValueError(f"File size mismatch: {received} != {expected_length}")
    if digest != expected_sha1:
        raise ValueError(f"Git blob mismatch: {digest} != {expected_sha1}")
    return received, digest


def verify_file(path: Path) -> tuple[int, str]:
    if not path.is_file():
        raise FileNotFoundError(f"Missing whole-body atlas FBX: {path}")
    if path.stat().st_size != EXPECTED_LENGTH:
        raise ValueError("Existing FBX size is not the pinned upstream size")
    with path.open("rb") as stream:
        return verify_stream(stream, EXPECTED_LENGTH, EXPECTED_GIT_BLOB_SHA1)


def download(destination: Path = DESTINATION) -> tuple[int, str]:
    """Fetch exactly one official unmodified FBX using atomic temporary write."""
    if destination.exists():
        result = verify_file(destination)
        print("Already verified complete full-body FBX:", destination)
        return result

    destination.parent.mkdir(parents=True, exist_ok=True)
    temporary = destination.with_name(destination.name + ".part")
    if temporary.exists():
        temporary.unlink()

    request = urllib.request.Request(
        SOURCE_URL,
        headers={"User-Agent": "MagicMirror-Source-Asset-Setup/1.0"}
    )
    try:
        with urllib.request.urlopen(request, timeout=60) as response:
            with temporary.open("wb") as output:
                total = 0
                while True:
                    chunk = response.read(BLOCK_SIZE)
                    if not chunk:
                        break
                    total += len(chunk)
                    if total > EXPECTED_LENGTH:
                        raise ValueError("Unexpected model size from upstream")
                    output.write(chunk)
        result = verify_file_pinned(temporary)
        temporary.replace(destination)
        print("Verified full atlas saved:", destination)
        return result
    finally:
        if temporary.exists():
            temporary.unlink()


def verify_file_pinned(path: Path) -> tuple[int, str]:
    if path.stat().st_size != EXPECTED_LENGTH:
        raise ValueError("Downloaded FBX does not match pinned size")
    with path.open("rb") as stream:
        return verify_stream(stream, EXPECTED_LENGTH, EXPECTED_GIT_BLOB_SHA1)


def main(argv=None):
    p = argparse.ArgumentParser(
        description="Use one pinned original Z-Anatomy SkeletalSystem100.fbx")
    options = p.add_mutually_exclusive_group()
    options.add_argument("--download", action="store_true",
                         help="Fetch and verify unmodified full-body FBX")
    options.add_argument("--verify-only", action="store_true",
                         help="Verify existing FBX; never download it")
    args = p.parse_args(argv)

    print("Upstream commit:", UPSTREAM_COMMIT)
    print("Source:", SOURCE_URL)
    print("Expected Git blob SHA-1:", EXPECTED_GIT_BLOB_SHA1)
    print("Destination:", DESTINATION)
    print("License notice: Z-Anatomy/BodyParts3D attribution and third-party "
          "source-specific licenses must be reviewed before redistribution.")

    if args.download:
        download()
    elif args.verify_only:
        verify_file(DESTINATION)
        print("Verified pinned full-body FBX.")
    else:
        print("No download requested. Run with --download when ready.")


if __name__ == "__main__":
    try:
        main()
    except (OSError, ValueError) as error:
        print("Asset setup failed:", error, file=sys.stderr)
        sys.exit(1)
