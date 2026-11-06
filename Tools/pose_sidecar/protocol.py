"""Versioned framing shared with the Unity PoseStreamReceiver.

Wire message (big-endian):
    uint32 header_length, UTF-8 JSON, uint32 jpeg_length, JPEG bytes
Both lengths are checked. An empty JPEG is valid for synthetic/no-camera frames.
JSON fields: version, frameId, timestampMs, width, height, mirrored,
landmarks[], worldLandmarks[]. Coordinates in landmarks are normalized
unmirrored image coordinates; Unity mirrors both frame and overlay.
"""
from __future__ import annotations

import io
import json
import struct
from typing import BinaryIO, Any

VERSION = 1
MAX_HEADER = 131_072
MAX_JPEG = 2_097_152


def encode_packet(header: dict[str, Any], jpeg: bytes = b"") -> bytes:
    if header.get("version") != VERSION:
        raise ValueError("Unsupported protocol version")
    if not isinstance(jpeg, bytes):
        raise TypeError("JPEG must be bytes")
    raw = json.dumps(header, separators=(",", ":"), allow_nan=False).encode("utf-8")
    if len(raw) == 0 or len(raw) > MAX_HEADER:
        raise ValueError("Invalid header length")
    if len(jpeg) > MAX_JPEG:
        raise ValueError("Image too large")
    return struct.pack(">I", len(raw)) + raw + struct.pack(">I", len(jpeg)) + jpeg


def read_exact(stream: BinaryIO, count: int) -> bytes:
    if count < 0:
        raise ValueError("Negative length")
    out = bytearray()
    while len(out) < count:
        data = stream.read(count - len(out))
        if not data:
            raise EOFError("Truncated packet")
        out.extend(data)
    return bytes(out)


def decode_packet(stream: BinaryIO) -> tuple[dict[str, Any], bytes]:
    header_length = struct.unpack(">I", read_exact(stream, 4))[0]
    if not 0 < header_length <= MAX_HEADER:
        raise ValueError("Invalid header size")
    header = json.loads(read_exact(stream, header_length).decode("utf-8"))
    if not isinstance(header, dict) or header.get("version") != VERSION:
        raise ValueError("Unsupported header")
    frame_length = struct.unpack(">I", read_exact(stream, 4))[0]
    if frame_length > MAX_JPEG:
        raise ValueError("Image too large")
    return header, read_exact(stream, frame_length)


def roundtrip(packet: bytes) -> tuple[dict[str, Any], bytes]:
    """Local protocol inspection helper, no webcam or MediaPipe needed."""
    return decode_packet(io.BytesIO(packet))
