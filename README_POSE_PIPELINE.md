# MagicMirror 2.0 — Phase 1 pose tracking

**Development status:** code committed on a feature branch; end-to-end Unity runtime pending manual test.

## Working components

- Single-owner Python webcam capture + MediaPipe Pose Landmarker (33 keypoints)
- Local-only framed TCP stream (JPEG + same-frame pose JSON)
- Synthetic skeleton source that needs no camera, models or Python packages
- Unity main-thread JPEG preview, horizontal mirror and 33-point overlay
- Worker-thread receiver with latest-frame only and automatic reconnect
- Python standard-library protocol tests and GitHub Actions check

## Quickest demonstration

\`\`\`bash
python3 -m Tools.pose_sidecar.main --demo
\`\`\`

In Unity Hub, open \`Unity/\` and use \`MagicMirror > Phase 1 > Create Pose Tracking Scene\`. Run generated scene with Play. The preview will be black (no synthetic camera image), with moving skeleton points and edges.

For a real camera, see \`Docs/PHASE_1_SETUP.md\`. This feature branch does not contain model weights, third-party anatomical FBXs, or a finished AR/X-Ray mode.

The old Phase 0 scene remains separate. **Do not attach Phase 0 and Phase 1 camera controllers to the same scene.**

Current target: pose tracking data layer. Next target: corrected arm registration, Z-Anatomy bones and calibrated AR projection.
