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
from .download_model import model_path
from .metrics import ProcessingStats

HOST = "127.0.0.1"


def make_header(frame_id: int, timestamp_ms: int, width: int, height: int,
                landmarks: list[dict], world_landmarks: list[dict],
                *, demo: bool, model_profile: str | None = None,
                inference_ms: float | None = None,
                jpeg_encode_ms: float | None = None) -> dict:
    header = {
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
    # Optional v1 diagnostic fields: JsonUtility ignores unknown keys in the
    # current Unity receiver. They do NOT measure end-to-end UI latency.
    if model_profile is not None:
        header["modelProfile"] = model_profile
    if inference_ms is not None:
        header["inferenceMs"] = inference_ms
    if jpeg_encode_ms is not None:
        header["jpegEncodeMs"] = jpeg_encode_ms
    return header


def serialize_landmark(lm: object) -> dict[str, float]:
    def number(name: str, fallback: float) -> float:
        value = getattr(lm, name, fallback)
        return float(fallback if value is None else value)
    return {
        "x": number("x", 0.0), "y": number("y", 0.0),
        "z": number("z", 0.0), "visibility": number("visibility", 1.0),
        "presence": number("presence", 1.0),
    }


def demo_packets(fps: int, scenario: str = "wave", *, image_encoder=None):
    period = 1.0 / fps
    start = time.monotonic()
    frame_id = 0
    last_ms = -1
    while True:
        now = time.monotonic()
        timestamp_ms = max(int(now * 1000), last_ms + 1)
        last_ms = timestamp_ms
        # Draw the video and transmit the exact same landmarks computed here.
        # With no encoder the original stdlib-only black demo stays unchanged.
        landmarks = synthetic_landmarks(now - start, scenario)
        header = make_header(
            frame_id, timestamp_ms, 1280, 720,
            landmarks, [], demo=True
        )
        jpeg = image_encoder(landmarks, frame_id) if image_encoder else b""
        yield encode_packet(header, jpeg)
        frame_id += 1
        time.sleep(max(0.0, start + frame_id * period - time.monotonic()))


def camera_packets(args):
    # Lazy imports: --demo and the stdlib test suite need no third-party wheels.
    import cv2
    import mediapipe as mp

    model = Path(args.model) if args.model else model_path(args.model_profile)
    if not model.is_file():
        raise FileNotFoundError(
            f"Missing {args.model_profile} model: {model}. Run "
            f"python -m Tools.pose_sidecar.download_model --profile {args.model_profile}"
        )

    options = mp.tasks.vision.PoseLandmarkerOptions(
        base_options=mp.tasks.BaseOptions(model_asset_path=str(model)),
        running_mode=mp.tasks.vision.RunningMode.VIDEO,
        num_poses=1,
        min_pose_detection_confidence=args.min_detection_confidence,
        min_pose_presence_confidence=args.min_presence_confidence,
        min_tracking_confidence=args.min_tracking_confidence,
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
        stats = ProcessingStats(report_every=args.stats_every)
        while True:
            ok, image = camera.read()
            if not ok:
                raise RuntimeError("Camera read failed")
            # Timestamps are monotonic within this camera session. They pair
            # this camera image to its VIDEO inference, not the Unity wall clock.
            timestamp_ms = max(int(time.monotonic() * 1000), last_ms + 1)
            last_ms = timestamp_ms
            rgb = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)

            infer_start = time.perf_counter()
            result = detector.detect_for_video(
                mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb), timestamp_ms
            )
            inference_ms = (time.perf_counter() - infer_start) * 1000.0

            poses = result.pose_landmarks or []
            world = result.pose_world_landmarks or []
            landmarks = [serialize_landmark(p) for p in poses[0]] if poses else []
            world_landmarks = [serialize_landmark(p) for p in world[0]] if world else []

            jpeg_start = time.perf_counter()
            jpeg_ok, buffer = cv2.imencode(
                ".jpg", image, [int(cv2.IMWRITE_JPEG_QUALITY), args.jpeg_quality]
            )
            jpeg_encode_ms = (time.perf_counter() - jpeg_start) * 1000.0
            if not jpeg_ok:
                raise RuntimeError("JPEG encoding failed")

            height, width = image.shape[:2]
            header = make_header(
                frame_id, timestamp_ms, width, height,
                landmarks, world_landmarks, demo=False,
                model_profile="custom" if args.model else args.model_profile,
                inference_ms=round(inference_ms, 3),
                jpeg_encode_ms=round(jpeg_encode_ms, 3),
            )
            summary = stats.add(inference_ms, jpeg_encode_ms)
            if summary:
                print(summary, flush=True)
            yield encode_packet(header, buffer.tobytes())
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
                    if args.demo or args.demo_video:
                        # Lazy import: baseline --demo requires only the standard
                        # library, while --demo-video needs OpenCV (no webcam/ML).
                        encoder = None
                        if args.demo_video:
                            from .demo_video import render_demo_jpeg

                            def encoder(points, frame_id):
                                return render_demo_jpeg(
                                    points, frame_id, args.demo_scenario,
                                    args.jpeg_quality
                                )
                        source = demo_packets(
                            args.fps, args.demo_scenario, image_encoder=encoder
                        )
                    else:
                        source = camera_packets(args)
                    try:
                        for packet in source:
                            client.sendall(packet)
                    finally:
                        source.close()
            except (BrokenPipeError, ConnectionResetError, ConnectionAbortedError):
                print("Unity disconnected; waiting for reconnect.", flush=True)


def parse_args(argv=None):
    p = argparse.ArgumentParser(description="MagicMirror local MediaPipe sidecar")
    synthetic = p.add_mutually_exclusive_group()
    synthetic.add_argument("--demo", action="store_true",
                           help="Synthetic 33-point pose, black image; stdlib only")
    synthetic.add_argument("--demo-video", action="store_true",
                           help="Synthetic JPEG + same-frame pose; needs OpenCV, no camera or MediaPipe")
    p.add_argument("--demo-scenario", choices=SCENARIOS, default="wave",
                   help="Simulated pose: wave, walk, squat, dropout (with --demo)")
    p.add_argument("--port", type=int, default=8765)
    p.add_argument("--camera", type=int, default=0)
    p.add_argument("--width", type=int, default=1280)
    p.add_argument("--height", type=int, default=720)
    p.add_argument("--fps", type=int, default=20)
    p.add_argument("--jpeg-quality", type=int, default=72)
    p.add_argument("--model-profile", choices=("lite", "full", "heavy"), default="lite",
                   help="Official Pose Landmarker bundle, default: lite")
    p.add_argument("--model", default=None, help="Explicit .task path overrides --model-profile")
    p.add_argument("--min-detection-confidence", type=float, default=0.5)
    p.add_argument("--min-presence-confidence", type=float, default=0.5)
    p.add_argument("--min-tracking-confidence", type=float, default=0.5)
    p.add_argument("--stats-every", type=int, default=120,
                   help="Print inference/JPEG timing summary every N frames (0 disables)")
    args = p.parse_args(argv)
    if not 1 <= args.port <= 65535:
        p.error("--port must be 1..65535")
    if not 1 <= args.fps <= 60:
        p.error("--fps must be 1..60")
    if not 30 <= args.jpeg_quality <= 95:
        p.error("--jpeg-quality must be 30..95")
    if not 160 <= args.width <= 3840 or not 120 <= args.height <= 2160:
        p.error("Unsupported camera dimensions")
    for option in ("min_detection_confidence", "min_presence_confidence",
                   "min_tracking_confidence"):
        if not 0.0 <= getattr(args, option) <= 1.0:
            p.error("--" + option.replace("_", "-") + " must be in [0, 1]")
    if not 0 <= args.stats_every <= 100_000:
        p.error("--stats-every must be 0..100000")
    return args


if __name__ == "__main__":
    try:
        serve(parse_args())
    except KeyboardInterrupt:
        print("\nPose sidecar stopped.")
