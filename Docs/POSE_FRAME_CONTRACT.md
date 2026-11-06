# PoseFrame v1: media, coordinates and compatibility contract

**Implemented transport:** `Tools/pose_sidecar/protocol.py` → `Unity/Assets/Scripts/Tracking/PoseStreamReceiver.cs`.  
**Version:** `version: 1` · VIDEO-mode synchronized baseline.

## Framing

~~~text
uint32_be header_length           # 1 .. 131072 bytes
UTF-8 JSON header                 # one captured source frame
uint32_be jpeg_length             # 0 .. 2097152 bytes
JPEG bytes                        # 0 bytes is valid for synthetic demo
~~~

The header and JPEG are one indivisible logical packet. The Unity worker may replace an older pending packet with a newer packet but must never pair a header with JPEG from another packet.

## v1 JSON fields

| Field | Type | Contract |
| --- | --- | --- |
| `version` | integer | Exactly `1`; incompatible schema changes require v2 |
| `frameId` | integer | Increasing within one connected producer session; can restart after reconnect |
| `timestampMs` | integer | Monotonic source capture/inference timestamp in milliseconds, increasing within a producer session; **not wall-clock epoch** |
| `width`, `height` | integer | Source image dimensions, before display mirroring |
| `mirrored` | boolean | Whether SOURCE image was already horizontally mirrored |
| `demo` | boolean | True for synthetic pose data; JPEG may be empty |
| `landmarks` | array | Exactly 33 normalized image landmarks when detected, or empty array |
| `worldLandmarks` | array | Optional 33 estimated 3D landmarks, or empty array |

Each landmark is `{x,y,z,visibility,presence}` with finite numeric fields. MediaPipe's landmark order is used unchanged. `visibility` and `presence` help quality gating; they do not establish anatomical accuracy.

## Unity transport integrity rules (M2)

The receiver accepts complete header+JPEG packets via the latest-only queue, and rejects duplicate, reordered, or non-monotonic `frameId`/`timestampMs` pairs **within one connection**. The locally assigned TCP session number advances on reconnect; the producer may restart from frame zero. An older connection cannot replace a newer one.

**A non-demo frame with empty JPEG bytes must be rejected** to prevent new pose landmarks from being drawn on old camera video. Only `demo=true` may have an empty JPEG. When the stream stalls, Unity clears old debug dots and blacks out the old video after its configurable timeout. This addresses *freshness*, not absolute end-to-end display latency.

## Coordinate and timing rules

- Normalized image `x` increases from **image left to right**, `y` from **top to bottom**, in the UNMIRRORED source-camera image. These are image coordinates, not Unity world positions.
- `z` of normalized image landmarks is a relative model estimate. It is **not** calibrated camera depth.
- `worldLandmarks` are MediaPipe's estimated body-relative 3D coordinates. They are not camera extrinsics, measured joints, or locations of internal bones. Conversion to Unity handedness, axes, and calibrated motion belongs to a separate kinematic solver.
- The sidecar currently emits `mirrored=false` because it passes the unflipped OpenCV camera frame. Unity mirrors **both video and overlay** for the mirror display. Never mirror landmark x separately without applying the equivalent image transform.
- The image and landmarks are computed from one captured frame. Source `timestampMs` comes from a monotonic clock; cross-process latency metrics require an explicit clock strategy and cannot be inferred by subtracting a Unity wall clock from this timestamp.
- Camera rotation is currently **not explicitly encoded** in v1. Keep camera orientation fixed in the baseline and verify it manually on Windows and macOS before claiming a rotation-safe pipeline.
- No-pose frames have empty landmarks. Tracking filters must expire stale poses instead of endlessly showing a frozen anatomy model.

## Extension/versioning policy

Optional, ignorable diagnostic header fields may be introduced within v1 **only if existing Unity receivers remain compatible**. Any change to coordinate conventions, mirror semantics, payload framing, mandatory fields, or frame association requires `version=2` and coordinated producer/receiver changes.

Segmentation-mask transport, rotation metadata, explicit image-to-mask timestamp association, alternative media codecs and LIVE_STREAM inference **are not part of the tested v1 contract**. Do not add LIVE_STREAM without preserving result↔source frame identity when callbacks skip busy frames.

## Minimum verification

1. Python protocol round-trip tests, malformed packet rejection and loopback TCP fragmentation.
2. Unity generated pose scene loads and renders synthetic `--demo` landmarks.
3. In a real webcam scene, right/left arm moves on the correct reflected sides with no frame skew.
4. Connection loss and reacquisition clear stale anatomy correctly.
5. Record FPS, capture→render latency (using a valid synchronized method), and OS/GPU before a performance claim.

See `Docs/PHASE_1_SETUP.md` for running the sidecar and `Docs/UNITY_RUNTIME_VALIDATION.md` for the remaining on-device checks.
