# M2 increment — Frame ordering, stale overlays and synthetic video

**Status:** implementation source and CI tests; **actual Unity Editor and real camera validation remain pending**. This change does not claim verified AR alignment.

## Why this matters for the Anatomical X-Ray Mirror

Even when the Python producer generates pose landmarks and JPEG from the **same frame**, Unity must not display an older pose or stick with old dots after the sidecar disconnects. In addition, the original dependency-free `--demo` produced only points on black, which cannot exercise image-versus-pose overlay alignment.

This increment adds:

1. `PoseFrameGuard` rejects invalid, duplicate and non-monotonic source frame IDs/timestamps **within one TCP session**. Reconnect starts a new internally assigned session and permits the Python producer to restart at frame 0. Old-session packets cannot be reinstated.
2. `PoseFrame.IsValid` checks frame metadata and the expected optional 33-world-landmark length.
3. `PoseStreamReceiver` refuses missing JPEG **for non-demo camera packets** so it never applies a new pose on top of a previous live image. It continues to receive whole header+image packets via the existing latest-only worker queue.
4. On timeout, the receiver hides old debug landmarks and turns the stale camera preview black; its overlay smoothing resets. Full-body and arm retargeters retain their existing independent pose-timeout/hide behavior.
5. `--demo-video` renders a synthetic unmirrored JPEG with visible joint reference marks and a source frame ID using OpenCV, and streams **that image and precisely the same pose sample**. No webcam and no MediaPipe model needed.
6. `MagicMirror/Validation/Pose Frame Integrity Self Check` runs ordering/reconnect tests in the actual Unity Editor; the M2 batch entry point now includes this fifth test suite.

## Local synthetic image + pose test

From repository root, create a Python environment with just OpenCV and NumPy:

~~~bash
python3 -m venv .venv
source .venv/bin/activate  # Windows PowerShell: .venv\Scripts\Activate.ps1
python3 -m pip install "opencv-python>=4.10,<5" "numpy>=1.26,<3"
python3 -m Tools.pose_sidecar.main --demo-video --demo-scenario wave --fps 20
~~~

In Unity 6.3, generate/open the **Phase 1 Pose Tracking Scene**, enter Play, then examine the camera-like JPEG: the rendered blue/orange joint dots in the source image should remain aligned with the turquoise Unity 33-point overlay when moving (allowing for independently configured overlay smoothing). The status header and in-picture `FRAME` number should advance. The feed is intentionally **not a real human image**, and all simulated movement is schematic.

Repeat with:

~~~bash
python3 -m Tools.pose_sidecar.main --demo-video --demo-scenario walk
python3 -m Tools.pose_sidecar.main --demo-video --demo-scenario squat
python3 -m Tools.pose_sidecar.main --demo-video --demo-scenario dropout
~~~

Stop the sidecar and ensure the debug overlay clears after ~0.75 s instead of retaining stale dots/video. Restart it and ensure frame 0 from the new connection is accepted. Switch to original `--demo` and confirm it displays black video (not the prior synthetic image). Finally test a **real** webcam using `--model-profile lite` and camera setup in `Docs/PHASE_1_SETUP.md`.

## Offline producer-to-TCP integration regression

`Tools/pose_sidecar/tests/test_sidecar_integration.py` starts the **real Python
sidecar as a subprocess**, reads successive framed packets through a localhost
TCP client, and verifies the 33-point schema, monotonic frame IDs, timestamp
association and reconnection frame reset. The optional `--demo-video` variant
tests actual JPEG packet bytes; it requires only OpenCV/NumPy, not MediaPipe.

```bash
python3 -m unittest Tools.pose_sidecar.tests.test_sidecar_integration -v
```

The GitHub pose-sidecar workflow runs both dependency-free and OpenCV-backed
variants. This checks real Python transport behavior, **not Unity rendering**.

## Limits / next gate

- Source image landmarks are `x` left-to-right and `y` top-to-bottom before Unity mirror transform. The test does not prove camera handedness on specific webcam hardware.
- `inferenceMs` is processing-only; no valid end-to-end latency claim is possible from it.
- The new unit tests run without Unity or OpenCV by injecting a fake JPEG encoder. They check the same-frame packet **construction**; they do not evaluate visual overlay error.
- If the real Unity test reveals misalignment, inspect `RawImage` aspect fitting and the anatomy RenderTexture together rather than inserting a second arbitrary x flip.
- This still does not import, bind, or validate the original Z-Anatomy FBX, or solve 3D joint constraints.

**Manual M2 completion requires:** running the actual Unity Editor on hardware, verifying mirrored image/pose alignment, alpha compositing, sidecar reconnection and measured FPS/latency. See `Docs/UNITY_RUNTIME_VALIDATION.md`.
