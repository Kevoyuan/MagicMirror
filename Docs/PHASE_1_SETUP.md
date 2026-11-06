# Phase 1 — Pose data pipeline (tracking vertical slice)

## Current implementation

This branch adds a real MediaPipe Pose Landmarker Python **sidecar** and a Unity localhost TCP receiver. The sidecar is the sole camera owner; each message carries a JPEG **and landmarks from the same captured frame**. This avoids opening a single webcam twice, removes immediate native Unity plugin compatibility blockers, and avoids mixing pose timestamps across components.

The old `WebcamPreviewController` is only used in the *separate* Phase 0 scene.

**Status:** Python framing + synthetic pose tests run in GitHub CI. Unity editor, physical webcam, MediaPipe wheel/model, image decode and end-to-end performance remain **unverified** until the user runs them.

## Start without any hardware/ML dependencies

From repo root:

```bash
python3 -m unittest discover -s Tools/pose_sidecar/tests -v
python3 -m Tools.pose_sidecar.main --demo
```

The demo server listens on `127.0.0.1:8765`. It streams 33 synthetic landmark positions and **no JPEG** (black preview). Your Unity overlay should show a moving stick figure and `DEMO` status. Close with Ctrl+C.

In Unity Hub open the **`Unity/` project** using the editor version in `Unity/ProjectSettings/ProjectVersion.txt`. Wait for import. Then:

1. Unity menu **MagicMirror > Phase 1 > Create Pose Tracking Scene**.
2. Open `Assets/Scenes/Phase1PoseTracking.unity`.
3. Enter Play mode. It connects to `127.0.0.1:8765` and renders the demo skeleton.
4. Stop the sidecar while Play is still active to verify reconnect.
5. Restart the sidecar and verify the UI recovers.

Only the frame reader uses the TCP worker thread; Unity texture upload and skeleton rendering happen on Unity's main thread.

## Real camera + MediaPipe

Python 3.11 recommended (separate virtual environment).

```bash
python3 -m venv .venv
source .venv/bin/activate  # Windows PowerShell: .venv\Scripts\Activate.ps1
python -m pip install -r Tools/pose_sidecar/requirements.txt
python -m Tools.pose_sidecar.download_model
python -m Tools.pose_sidecar.main --camera 0 --width 1280 --height 720 --fps 20
```

The model downloader fetches the official **MediaPipe Pose Landmarker Lite** bundle into `Tools/pose_sidecar/models/`; the model is gitignored. The sidecar does not download any model while serving.

Start the Unity Phase 1 scene and enter Play mode. It should show the same video frame and normalized landmarks mirrored together. If the model is unavailable, run `--demo` instead to test transport first.

If the camera cannot be opened on macOS, check **Privacy & Security > Camera** for the Python or terminal process. The Unity Editor does not access the camera in this mode; the Python sidecar does.

## Binary stream v1

```text
uint32 big-endian JSON_HEADER_SIZE
JSON_HEADER UTF-8 (max 128 KiB)
uint32 big-endian JPEG_SIZE
JPEG_BYTES (max 2 MiB; 0 is allowed for demo)
```

JSON header keys:
- `version=1`
- `frameId`, `timestampMs` (monotonic, strictly increasing source clock)
- `width`, `height`, `mirrored`, `demo`
- `landmarks` (33 normalized unmirrored image-space points for one pose; empty when none)
- `worldLandmarks` (optional corresponding estimated 3D world points; do not treat as calibrated absolute camera depth)

Each landmark has `x,y,z,visibility,presence`. Landmark IDs follow MediaPipe's standard 33-point body map. Mirror operation is applied only on the Unity visual presentation, so later calibrated anatomical retargeting must consistently define coordinate handedness.

Both processes bind/connect to loopback only and do not record images or send them to remote services.

## Phase 1 acceptance checks (manual tests pending)

- [ ] Unity imports and compiles the new C# components.
- [ ] Generated tracking scene opens without errors.
- [ ] Synthetic demo draws the 33-point schema (visible subset) on black preview.
- [ ] Stop/restart demo shows disconnect and reconnect.
- [ ] After Python deps/model install, real camera video and body joints appear.
- [ ] Raising right/left arm mirrors video and joint overlay consistently.
- [ ] No visible skeleton if pose is absent.
- [ ] FPS and end-to-end latency measured on target Mac and Windows computers.
- [ ] Commit the scene and generated Unity metadata after an editor run.

## Risks / limitations

- Unity `Texture2D.LoadImage` for each JPEG and Python JPEG encoding may be CPU-intensive; measure before making 30 FPS promises.
- The sidecar requires its own Python environment, a compatible MediaPipe wheel, and a separately acquired model.
- 3D world landmarks do NOT solve camera calibration or true anatomical depth.
- At most one viewer is supported at a time.
- Demo skeleton contains only selected meaningful limb joints and no video.
- Phase 1 is a transport/pose-debug milestone, **not** anatomical overlay yet.

See Google AI Edge's Pose Landmarker Python guide:
https://developers.google.com/edge/mediapipe/solutions/vision/pose_landmarker/python
