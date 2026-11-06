# Phase M1 — MediaPipe profile selection and processing metrics

**Status:** Source implementation committed; Python CI verification required. **No actual camera/model/GPU benchmark yet.** This is the first M1 increment only.

## Why

Official Pose Landmarker bundles exist in **Lite, Full and Heavy** variants. The sidecar previously hard-coded Lite and 0.5 confidence thresholds. M1 lets a developer compare models on the **same source video and machine** before choosing a pinned production configuration.

Official model documentation: https://developers.google.com/edge/mediapipe/solutions/vision/pose_landmarker

## Install / choose a profile

From the repository root:

~~~bash
python3 -m venv .venv
source .venv/bin/activate  # Windows: .venv\Scripts\Activate.ps1
python3 -m pip install -r Tools/pose_sidecar/requirements.txt
python3 -m Tools.pose_sidecar.download_model --profile lite
python3 -m Tools.pose_sidecar.download_model --profile full
python3 -m Tools.pose_sidecar.download_model --profile heavy
~~~

Or download all three: `python3 -m Tools.pose_sidecar.download_model --all`. Bundles are downloaded from the official Google-hosted `float16/latest` URL into gitignored `Tools/pose_sidecar/models/`. **The URLs are not immutable or SHA-256 pinned yet.** Capture checksums and validate actual model behavior before reproducible release claims.

Use the existing Unity Phase 1 scene and a real camera:

~~~bash
python3 -m Tools.pose_sidecar.main --model-profile full \
  --camera 0 --width 1280 --height 720 --fps 20 \
  --min-detection-confidence 0.6 \
  --min-presence-confidence 0.5 \
  --min-tracking-confidence 0.6 \
  --stats-every 120
~~~

Use `--model /absolute/path/custom.task` to override the selected profile's local file path; such packets report `modelProfile=custom`. By default the sidecar remains Lite / VIDEO / one person, preserving existing usage. `--demo` requires neither MediaPipe nor a task bundle.

## Timings and compatibility

Every 120 frames by default, Python prints recent inference and JPEG encoding mean/p95 processing times (bounded rolling sample). `--stats-every 0` disables reporting. These are **NOT end-to-end camera-to-display latencies**: capture, TCP transfer, decode, Unity rendering and display still add delay.

Additional optional PoseFrame v1 JSON keys:
- `modelProfile`: `lite`, `full`, `heavy`, or `custom`.
- `inferenceMs`: Python `detect_for_video` duration measured with `time.perf_counter()`.
- `jpegEncodeMs`: JPEG encoder duration in milliseconds.

The existing Unity `JsonUtility` receiver can ignore the new fields, so the v1 framing and landmark data remain unchanged. MediaPipe result, image and timestamp still originate from one captured source frame.

## CI tests and acceptance still pending

~~~bash
python3 -m unittest discover -s Tools/pose_sidecar/tests -v
python3 Tools/ci/check_mediapipe_only.py
~~~

Before marking M1 complete, run side-by-side actual camera/model tests, record OS and device, freeze the working Python/MediaPipe dependency versions, verify model SHA-256, evaluate motion stability with held-out clips, and measure true Unity display latency. Optional segmentation and LIVE_STREAM callback↔frame identity are **not implemented in this increment**.
