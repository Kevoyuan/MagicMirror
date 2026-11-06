"""Run MediaPipe on the sole camera owner and stream matched image+pose to Unity.

Usage:
  python -m Tools.pose_sidecar.main --demo
  python -m Tools.pose_sidecar.main --model Tools/pose_sidecar/models/pose_landmarker_lite.task

Binds only to 127.0.0.1; images are not recorded or uploaded.
"""
from __future__ import annotations

import argparse
import contextlib
from pathlib import Path
import socket
import time

from .demo import SCENARIOS, synthetic_landmarks
from .protocol import VERSION, encode_packet

HOST = "127.0.0.1"


def make_header(frame_id: int, timestamp_ms: int, width: int, height: int,
                landmarks: list[dict], world_landmarks: list[dict],
                *, demo: bool) -> dict:
    return {
        "version": VERSION,
        "frameId": frame_id,
        "timestampMs": timestamp_ms,
        "width": width,
        "height": height,
        "mirrored": False,
        "demo": demo,
        "landmarks": landmarks,
        "worldLandmarks": world_landmarks,
    }


def serialize_landmark(lm: object) -> dict[str, float]:
    def number(name: str, fallback: float) -> float:
        value = getattr(lm, name, fallback)
        return float(fallback if value is None else value)
    return {
        "x": number("x", 0.0), "y": number("y", 0.0),
        "z": number("z", 0.0), "visibility": number("visibility", 1.0),
        "presence": number("presence", 1.0),
    }


def demo_packets(fps: int, scenario: str = "wave"):
    period = 1.0 / fps
    start = time.monotonic()
    frame_id = 0
    last_ms = -1
    while True:
        now = time.monotonic()
        timestamp_ms = max(int(now * 1000), last_ms + 1)
        last_ms = timestamp_ms
        header = make_header(
            frame_id, timestamp_ms, 1280, 720,
            synthetic_landmarks(now - start, scenario), [], demo=True
        )
        yield encode_packet(header, b"")  # Display overlay on black preview.
        frame_id += 1
        time.sleep(max(0.0, start + frame_id * period - time.monotonic()))


def camera_packets(args):
    # Lazy imports: --demo and the stdlib test suite need no third-party wheels.
    import cv2
    import mediapipe as mp

    model = Path(args.model)
    if not model.is_file():
        raise FileNotFoundError(
            f"Missing model: {model}. Run python -m Tools.pose_sidecar.download_model"
        )

    options = mp.tasks.vision.PoseLandmarkerOptions(
        base_options=mp.tasks.BaseOptions(model_asset_path=str(model)),
        running_mode=mp.tasks.vision.RunningMode.VIDEO,
        num_poses=1,
        min_pose_detection_confidence=0.5,
        min_pose_presence_confidence=0.5,
        min_tracking_confidence=0.5,
    )
    camera = cv2.VideoCapture(args.camera)
    if not camera.isOpened():
        camera.release()
        raise RuntimeError(f"Cannot open camera index {args.camera}")
    camera.set(cv2.CAP_PROP_FRAME_WIDTH, args.width)
    camera.set(cv2.CAP_PROP_FRAME_HEIGHT, args.height)
    camera.set(cv2.CAP_PROP_FPS, args.fps)

    with contextlib.ExitStack() as stack:
        stack.callback(camera.release)
        detector = stack.enter_context(mp.tasks.vision.PoseLandmarker.create_from_options(options))
        frame_id, last_ms = 0, -1
        while True:
            ok, image = camera.read()
            if not ok:
                raise RuntimeError("Camera read failed")
            timestamp_ms = max(int(time.monotonic() * 1000), last_ms + 1)
            last_ms = timestamp_ms
            rgb = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
            result = detector.detect_for_video(
                mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb), timestamp_ms
            )
            poses = result.pose_landmarks or []
            world = result.pose_world_landmarks or []
            landmarks = [serialize_landmark(p) for p in poses[0]] if poses else []
            world_landmarks = [serialize_landmark(p) for p in world[0]] if world else []
            jpeg_ok, buffer = cv2.imencode(
                ".jpg", image, [int(cv2.IMWRITE_JPEG_QUALITY), args.jpeg_quality]
            )
            if not jpeg_ok:
                raise RuntimeError("JPEG encoding failed")
            height, width = image.shape[:2]
            yield encode_packet(make_header(
                frame_id, timestamp_ms, width, height,
                landmarks, world_landmarks, demo=False
            ), buffer.tobytes())
            frame_id += 1


def serve(args):
    # Only localhost, so Unity can connect without opening an external interface.
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as server:
        server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        server.bind((HOST, args.port))
        server.listen(1)
        print(f"MagicMirror pose sidecar listening on {HOST}:{args.port}", flush=True)
        while True:
            client, address = server.accept()
            print(f"Unity connected: {address}", flush=True)
            try:
                with client:
                    # Re-create generator for every Unity Play session. This
                    # releases the camera/model after a client disconnect.
                    source = demo_packets(args.fps, args.demo_scenario) if args.demo else camera_packets(args)
                    try:
                        for packet in source:
                            client.sendall(packet)
                    finally:
                        source.close()
            except (BrokenPipeError, ConnectionResetError, ConnectionAbortedError):
                print("Unity disconnected; waiting for reconnect.", flush=True)


def parse_args(argv=None):
    p = argparse.ArgumentParser(description="MagicMirror local MediaPipe sidecar")
    p.add_argument("--demo", action="store_true", help="Synthetic 33-point pose; no camera or ML needed")
    p.add_argument("--demo-scenario", choices=SCENARIOS, default="wave",
                   help="Simulated pose: wave, walk, squat, dropout (with --demo)")
    p.add_argument("--port", type=int, default=8765)
    p.add_argument("--camera", type=int, default=0)
    p.add_argument("--width", type=int, default=1280)
    p.add_argument("--height", type=int, default=720)
    p.add_argument("--fps", type=int, default=20)
    p.add_argument("--jpeg-quality", type=int, default=72)
    p.add_argument("--model", default="Tools/pose_sidecar/models/pose_landmarker_lite.task")
    args = p.parse_args(argv)
    if not 1 <= args.port <= 65535:
        p.error("--port must be 1..65535")
    if not 1 <= args.fps <= 60:
        p.error("--fps must be 1..60")
    if not 30 <= args.jpeg_quality <= 95:
        p.error("--jpeg-quality must be 30..95")
    if not 160 <= args.width <= 3840 or not 120 <= args.height <= 2160:
        p.error("Unsupported camera dimensions")
    return args


if __name__ == "__main__":
    try:
        serve(parse_args())
    except KeyboardInterrupt:
        print("\nPose sidecar stopped.")
