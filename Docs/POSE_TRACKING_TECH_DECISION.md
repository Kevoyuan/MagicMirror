> **Historical implementation rationale, still applicable where consistent with ADR-001.** The authoritative 2.0 decision is [ADR-001 MediaPipe-only](ADR-001_MEDIAPIPE_ONLY.md); no OpenPose fallback or alternative active pose provider is supported. See the versioned [PoseFrame v1 contract](POSE_FRAME_CONTRACT.md).

# Decision: Python MediaPipe sidecar for Phase 1

Date: 2026-11-06

## Decision

Use a Python MediaPipe Tasks Pose Landmarker process as a local camera-and-pose provider, and render its aligned image/landmarks in Unity using a lightweight TCP reader.

## Why

- The existing Unity OpenPose plugin is old and calls a native `openpose` library.
- Unity package / OS-specific MediaPipe native plugin compatibility is not yet verified.
- A *single* Python camera owner gives an unambiguous, same-frame pair of image and pose data; it avoids concurrent webcam contention.
- The Unity side owns frame presentation and overlay; future anatomy retargeting remains C# and engine-independent via an explicit `PoseFrame` model.
- A deterministic no-camera demo lets us test integration without a model, GPU or physical machine.
- MediaPipe VIDEO mode accepts sequential monotonic timestamps and returns 33 normalized and world landmarks. It does not guarantee physically calibrated internal anatomy.

## Trade-offs

- Installing Python separately makes the developer UX less seamless.
- JPEG encoding/decoding and local TCP framing add latency.
- This is not yet a user-facing distributable app.
- Future options: Unity native MediaPipe plugin, optimized shared memory transport, async frame capture, model-specific backend on Apple Silicon.

## Non-goals for this decision

- Importing anatomical meshes, medical-grade reconstruction, precise occlusion, cloud processing, user recording or mobile runtime.

## Invariants

1. One camera owner per scene.
2. A pose packet and image come from the same captured frame.
3. All UnityEngine UI/Texture operations occur on the Unity main thread.
4. Latest-only frame replacement bounds receiver memory in delayed rendering.
5. Network access is limited to 127.0.0.1.
6. No model weights or copyrighted anatomy content copied into project.
