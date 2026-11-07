# MagicMirror — iPhone App Design Specification

**Status:** Draft v0.1 — recommended direction, pending visual approval  
**Last updated:** 2026-11-07  
**Platform:** Native iPhone, SwiftUI  
**Source repository:** https://github.com/Kevoyuan/MagicMirror  
**Document owner:** Mobile UI Design Agent  
**Scope:** Product UX, information architecture, visual language, interface contracts, accessibility, and acceptance criteria. This document is not proof of an implemented iOS app.

## 1. Product thesis

**MagicMirror turns the iPhone camera into an interactive human-anatomy mirror.** Rather than replacing someone with a costume or fictional character, it connects their real movements to understandable skeletal and, in later phases, muscular structures.

The signature loop is **move → observe → tap → understand**:

1. A person sees their camera image and an aligned pose overlay.
2. The overlay reacts as they move.
3. They select a highlighted body region, e.g. forearm, elbow, or shoulder.
4. A concise anatomy sheet explains what they are viewing.
5. They can switch layers or save a learning card.

**Positioning:** Educational, exploratory, and movement-driven — not a beauty filter, a costume effect, a clinical imaging tool, or a medical diagnostic product.

**Accuracy boundary:** Camera keypoints and illustrative anatomy must never be presented as a precise reconstruction of the user's own internal anatomy. A model that is merely pose-aligned is labeled illustrative.

## 2. Evidence and maturity

### Verified from existing repository

- README: Unity webcam-based Magic Mirror using OpenPose to detect human-body keypoints, create an interactive skeleton overlay, and demonstrate AR-like rendering.
- OpenPosePlugin/Assets/OpenPose/Examples/Scripts/HumanController2D.cs: draws pose, hand, and face keypoints according to their detection confidence.
- OpenPosePlugin/Assets/Script/MeshPoseCalculator.cs: experiments with positioning/scaling meshes from two pose keypoints.
- OpenPosePlugin/Assets/Script/JointToPose.cs and MeshPoseAdjuster.cs: experimental joint mapping and geometry adjustment.
- model_for_test/: contains forearm/radius FBX assets and test media.

### Not yet established by repository evidence

- A native iOS/SwiftUI project, production camera pipeline, or shipping iPhone app.
- A validated on-device body-pose tracker.
- Reliable whole-body anatomical mesh fitting or muscle activation analysis.
- Rights and attribution suitable for distribution of all third-party anatomy models.
- Performance, device support, privacy behavior, or any accuracy metric on an actual iPhone.

The existing Unity/OpenPose work is a reference implementation and asset source, **not an iOS runtime contract**. In particular, hard-coded mesh scale and raw pose coordinates are experimental, not production calibration.

## 3. User, situation, and product principles

### Primary user (working assumption)

A curious learner, student, or maker who wants to explore how human bones and muscles relate to movement by looking at their own body. Future validation should distinguish casual learners, teachers, and anatomy students.

### Usage setting

- Short 30–120 second sessions; often standing in front of an iPhone camera.
- One-handed screen operation when possible.
- Front camera for close framing; rear camera with a stable mount for wider-body framing when supported.
- Varying lighting and framing conditions; device may not capture the full body.

### Principles

1. **The human body is the content.** Keep camera/image and anatomy visualization dominant; chrome recedes.
2. **Explain what the system actually knows.** Distinguish detected joints, estimated alignment, and educational illustrations.
3. **Discoverable within seconds.** A first-time user should find camera start, layer control, and anatomy selection without a tutorial.
4. **Native interaction over novelty UI.** Standard iOS navigation, sheets, system settings, and accessibility conventions.
5. **Privacy and trust first.** Explicit contextual camera permission, local-by-default processing as a goal, and no implied medical results.

## 4. Design direction exploration

These are materially different design worlds, not merely three palettes.

| Direction | Visual grammar | Primary scene | Strength | Risk |
| --- | --- | --- | --- | --- |
| **A · Clinical Atlas** | Warm off-white paper, precise anatomy illustration, measured typography, strong editorial labels | Anatomical reference first; camera in a contained module | Clear for education; trusted and familiar | Camera experience feels secondary |
| **B · Dark Anatomy Studio** | Near-black immersive camera canvas, restrained jade interaction tint, warm bone highlights, cinematic focused overlays | Full-height mirror with tiny controls; information appears in a bottom sheet | Makes motion and visual anatomy the product; best fit for live mirror | Must protect contrast and avoid visual effects that resemble a game |
| **C · Motion Lab** | Bright graphite/white grids, motion traces, structured exercise-like controls, energetic coral highlight | Side-by-side motion view and anatomy information | Strong for posture/biomechanics exploration | Can imply diagnosis or workout scoring beyond current evidence |

### Provisional choice: B · Dark Anatomy Studio

**Use B as the implementation baseline for this first draft**, subject to explicit visual approval. Preserve A's anatomical labeling discipline and C's readable movement feedback without importing their full visual worlds.

The product's memorable moment is a subtle, pose-aligned anatomical overlay appearing on a live image, not a startup animation or decorative gradient.

## 5. Information architecture

Top-level iPhone tabs (three destinations, no action disguised as a tab):

1. **镜子 / Mirror** — preview, camera permission, tracking, layer switching, anatomy selection, capture.
2. **图鉴 / Atlas** — browse a consistent, educational body map and regional anatomy entries without a camera.
3. **收藏 / Saved** — saved anatomy learning cards and user-initiated captures.

**Settings** is a toolbar destination, not a fourth tab. **Start Mirror** is an action on the Mirror destination. The immersive camera uses a full-screen experience; tab navigation remains available outside that experience.

### Main journeys

**First visit:** Launch → Mirror landing → short purpose statement + “打开相机” → contextual iOS permission → framing guidance → live mirror. Declined permission → clearly explained alternative “浏览人体图鉴” and a path to iOS Settings.

**Discover anatomy:** Mirror → layer selector “骨架” → tap supported region → region-focused info sheet → expand detail → bookmark. Tracking remains visible while the sheet is collapsed; paused state is explicit when invoked.

**Learn without camera:** Atlas → full-body overview → choose region → definition / function / connected structures → save → return with native back behavior.

**Return to saved item:** Saved → learning card → linked Atlas detail. A camera capture is only shown if the user explicitly saved one.

## 6. Screen specifications

### S01 · Mirror landing

- Large native title: “MagicMirror”.
- One clear product line: “动起来，看见身体的结构。”
- Primary action: “打开相机”.
- Secondary action: “探索人体图鉴”.
- Visual proof: approved anatomy artwork, clearly labeled illustrative; not a fake camera measurement.
- Small privacy disclosure: “启用相机后才会开始识别。不会自动保存视频。”
- Empty/loading/denied/restricted camera states must have recovery actions.

### S02 · Live Mirror (signature surface)

**Priority stack:** camera image → pose/anatomy overlay → safety/framing status → compact controls → optional information sheet.

Top controls inside safe area:
- Close (returns to Mirror landing).
- Camera flip (only when available).
- Tracking status with textual state (“正在识别”, “请后退一点”, “未识别到人体”) and an accessible label.

Bottom controls above home indicator:
- Layer control: “原始画面 / 骨架 / 骨骼示意”; **骨骼示意** is gated until verified and clearly illustrative.
- Capture button; saves only with explicit confirmation.
- “暂停” action to freeze the preview and allow inspection.

Interactions:
- Tap only hittable, identified anatomy regions; do not create invisible hotspots everywhere.
- Selecting a region highlights it and presents a medium-height native sheet; expand to detail.
- Pinch-to-zoom applies to frozen educational model / Atlas, **not** silently to camera calibration.
- If confidence drops or body leaves frame, fade the overlay and show plain-language guidance; no stale “locked on” skeleton.
- Full screen and orientation transforms maintain mirrored front-camera coordinates correctly.

### S03 · Anatomy information sheet

Contents, in order:
- Region name, English/Latin equivalent when verified.
- Single-sentence function in accessible language.
- Consistent illustrative image or simplified region diagram.
- “相关结构” navigates to Atlas.
- Bookmark action with reversible saved state.
- Clarifier: “示意图仅供学习，并非个人医学影像。”

Do not show diagnoses, medical certainty scores, fabricated body measurements, or unsupported muscle activity percentages.

### S04 · Atlas

- Search anatomy names and browse region filters (head / trunk / arms / legs).
- Full-body illustration with selectable, labeled regions.
- Scrollable list alternative for motor and VoiceOver accessibility.
- Content can be read without granting camera permission.
- Model layers are visible only when approved, licensed assets and educational copy exist.

### S05 · Saved

- Two sections: “知识卡片” and “我的截图” if captures exist.
- Each item has a stable title and date; no placeholder clinical statistics.
- Empty state: “还没有收藏。打开图鉴，保存你想了解的身体结构。”
- Delete is explicit and reversible when feasible; no cloud sync is implied.

### S06 · Settings and permissions

- Camera access status and route to system settings.
- Processing/privacy information in plain language.
- Language and appearance (system by default), attribution, model data source/license, educational disclaimer.
- No dark-mode-only lock-in; support system light/dark appearance as a production requirement.

## 7. Visual system (B baseline, provisional)

### Palette

These are proposed brand tokens; implement through semantic colors with adaptable light/dark values and verify contrast on device.

| Token | Dark baseline | Use |
| --- | --- | --- |
| canvas | #0B1215 | Immersive non-camera background |
| surface | #162228 | Sheets/cards over dark area |
| surfaceElevated | #1D2C32 | Selected control surfaces |
| border | #35484F | Dividers and focus delineation |
| textPrimary | #F3F7F5 | High-emphasis text |
| textSecondary | #B8C8C9 | Explanations and metadata |
| accent | #65D9BB | Main interactive tint; not for body geometry |
| bone | #E7D6B5 | Educational bone structures |
| muscle | #E88775 | Educational muscle structures when available |
| warning | system orange | Framing/tracking warnings |
| error | system red | Permissions or operational errors |

Do not use bone/muscle coloring alone as the distinction: add labels, legends, or patterns. For light mode, create and test semantically equivalent colors rather than applying the dark hex values unchanged.

### Typography

- Use system SF Pro / SwiftUI text styles (largeTitle, title2, headline, body, callout, caption).
- Large titles only on top-level landing/Atlas/Saved; live camera uses restrained inline labeling.
- Support Dynamic Type, Chinese and English length, and text wrapping. No fixed-height text cards.
- Anatomy Latin names are subordinate to localized region titles.

### Spacing, sizing, and shape

- Base grid: 4 pt, with 8/12/16/24/32 pt common steps.
- Horizontal page margin: 20 pt target; adapt to compact screens and safe areas.
- Interactive touch targets: **minimum 44 × 44 pt**, including icons.
- Controls: 12–16 pt corner radii; sheet geometry uses native presentation detents.
- Camera canvas: edge-to-edge image, but controls respect safe areas and home indicator.
- Avoid dashboard card grids, fake glass panels, gradient text, and decorative health metrics.

### Icons and motion

- SF Symbols only for native chrome (camera, switch camera, pause, bookmark, close, information).
- State changes use short, restrained transitions; selection changes should emphasize the anatomy, not the surrounding UI.
- Overlay tracks pose smoothly within validated latency; reduce jitter via temporal smoothing and confidence gating.
- Respect Reduce Motion: replace moving highlight tours and spatial transitions with simple fades.
- Use haptics for supported selection/capture results, not continuous tracking updates.

## 8. Interaction states and error handling

| Situation | User-facing behavior | Recovery |
| --- | --- | --- |
| First camera request | Explain the purpose just before requesting OS permission | Open Atlas without camera |
| Camera permission denied | Inline explanation, not a blank preview | “打开系统设置” |
| Camera temporarily unavailable | Neutral error state without false preview | Retry or Atlas |
| No body in frame | “请让上半身进入画面” or relevant framing guidance | Keep live preview; hide overlay |
| Low pose confidence / occlusion | Reduce overlay visibility; do not assert alignment | Reposition or pause |
| Model asset missing | Offer keypoint/skeleton-only mode | Continue without fake anatomy |
| Thermal/performance pressure | Lower effect fidelity; maintain camera controls | Explicit notice if tracking disabled |
| Camera in background | Stop/suspend capture according to platform lifecycle | Resume with clear state |
| Save failed / storage unavailable | No success claim | Retry, choose not to save |
| Empty Saved | Instructional empty state | Link to Atlas |

State identifiers proposed for implementation: idle, requestingPermission, permissionDenied, starting, tracking, lowConfidence, noBody, paused, interrupted, failed. Do not convert all state transitions into modal alerts.

## 9. SwiftUI implementation guidance (not implemented yet)

**Native shell:** SwiftUI with NavigationStack, TabView, native sheets/confirmation dialogs, safe-area aware layout, SF Symbols, and semantic system colors. No decorative Liquid Glass adoption unless explicitly approved.

**Presentation responsibilities:**
- AppShellView: tabs and global navigation.
- MirrorLandingView: permission-aware start screen.
- MirrorSessionView: immersive camera flow.
- MirrorOverlayView: rendering surface boundary; controls must not subscribe to every camera frame.
- AnatomySheetView: details for a selected region.
- AtlasView / AnatomyDetailView: browse without camera.
- SavedView / SettingsView: library and configuration.

**Model boundaries:**
- CameraSession: permissions, capture and lifecycle; AVFoundation likely for native camera, subject to prototype validation.
- PoseEstimator: independent protocol so native Vision/Core ML candidates can be tested without coupling UI to old Unity OpenPose APIs.
- PoseCoordinateMapper: single point of truth for rotation, camera mirroring, cropping, aspect fill, and image-to-screen transforms.
- AnatomyAssetStore: licensed models/metadata and stable region IDs.
- AnatomyOverlayRenderer: separate 2D keypoint preview from the later 3D anatomical rendering spike.
- SavedContentStore: explicit local persistence of user-confirmed content.
- MirrorSessionState: view-observable state, not raw per-frame keypoint arrays embedded in a top-level SwiftUI view.

**State patterns:** On iOS 17+, prefer @Observable models with view-owned @State and @Bindable only where editable bindings are required. Keep rendering frame updates out of global UI invalidation paths. Use bounded queues / frame dropping for real-time estimation rather than unbounded processing backlogs.

**Rendering choice is OPEN:** Evaluate native 2D overlay first; assess RealityKit/Metal/Unity embedding only after asset compatibility, tracking fidelity, device performance, and interaction requirements are measured. Do not promise that the desktop OpenPose plugin runs directly on iOS.

**Minimum OS target is PROVISIONAL:** iOS 17+ for the design skeleton, finalize after device feasibility tests and supported-device policy review.

## 10. Anatomy content and asset rules

- Source models from explicitly licensed assets, with per-asset provenance, author, modifications, and attribution recorded.
- Z-Anatomy (https://github.com/Z-Anatomy/Models-of-human-anatomy) is a candidate source from the product concept, **not yet an approved shipping dependency**; inspect its precise licenses and compatibility before import.
- Use one stable region identifier across pose mapping, display labels, taps, educational copy, and bookmarks.
- Align educational labels with accepted anatomy nomenclature; review copy for factual accuracy.
- Use illustrated demo fixtures for prototypes. Do not imply a static image is a real-time camera result.
- No unsupported internal-organ alignment, tissue diagnosis, circulation, muscle engagement, or biomechanics quantification.

## 11. Privacy, ethics, and accessibility

**Privacy goals (not verified shipping behavior):** No automatic video upload. Camera is activated only after user intent and permission. On-device pose estimation preferred; if cloud processing is considered later, it requires a separate explicit product and consent decision. No recording without a user action. Give the user control over saved content.

**Medical boundaries:** Label anatomy as educational illustration, not personal diagnostic imaging. No injury detection or posture diagnosis without separately established scientific, clinical, regulatory, and product evidence.

**Accessibility:**
- All touch targets at least 44 pt and accessible to VoiceOver.
- Every canvas selection has a non-gesture equivalent in a region list.
- Dynamic Type and Bold Text support; 4.5:1 body text contrast and 3:1 large text where applicable.
- Do not rely solely on color or motion; provide text status and selection names.
- Visible focus/selection and sensible reading order in expanded sheets.
- Camera workflow has a fully usable non-camera Atlas path.
- Test light and dark appearance, Increase Contrast, Reduce Motion, and large text sizes.

**Localization:** Chinese is the initial design copy. Store strings using localization resources for English expansion; allow Latin anatomy terms where appropriate, not as the only label.

## 12. Phased delivery and explicit non-goals

### P0 · Design-first and technical feasibility

- Confirm visual direction with three distinct iPhone mockups.
- Produce a click-through prototype for landing → mirror → anatomy sheet → Atlas → Saved.
- Determine iPhone pose tracking option, camera/overlay coordinate mapping, and minimum supported devices.
- Validate one licensed anatomy asset's format and attributions.

**Gate:** Prototype can convey the core loop without fabricating model accuracy. Technical spike documents what alignment quality can actually be achieved.

### P1 · Honest MVP

- Native tab shell, permission flow, live camera preview.
- Stable **2D pose skeleton overlay**, confidence/lost-tracking feedback.
- One or a small set of well-defined tappable body regions.
- Camera-free Atlas with real educational content.
- Local bookmarks. User-confirmed screenshot saving only if implemented and validated.
- Full accessibility and lifecycle handling for the supported target.

**Not required to claim MVP:** Full skeleton mesh, muscles, interactive organs, perfect 3D body fit, cloud accounts, AI coach, social feed, real-time medical feedback.

### P2 · Anatomy layer

- Licensed bone assets in defined regions with tested camera alignment.
- Layer switching between keypoints and illustrative skeletal anatomy.
- Better selection affordances, occlusion handling, model detail panels.
- Measure tracking quality on supported iPhones and label limits.

### P3 · Learning expansion

- Muscular layers and structured guided exploration.
- Motion examples, educator mode, richer saved learning sessions.
- Advanced biomechanics only if the necessary evidence and validation exist.

## 13. Acceptance criteria for design handoff

**Navigation**
- From first launch, “打开相机” and “探索人体图鉴” are available and understandable.
- Every visible action produces a real state transition in the prototype.
- System back, dismissal, camera exit, and selection-sheet transitions are predictable.

**Mirror clarity**
- In 5 seconds, user can recognize the camera stage, tracking state, current anatomy layer, and close action.
- Tracking failure is explained without leaving stale anatomy aligned to a person.
- The difference between detected joints and illustrated anatomy is explicit.

**Visual and accessibility QA**
- Core screens render without clipped controls at compact and large iPhone sizes.
- 44 pt targets, safe areas, Dynamic Type, VoiceOver labels and light/dark contrast are manually verified.
- No invisible-only gesture requirement or color-only status.
- Loading, denied permission, empty, paused, and no-body states are designed.

**Implementation readiness**
- Each screen has state, actions, navigation, and backing data specified.
- Non-implemented features are labeled future or prototype only.
- Real-time model strategy and model license review remain tracked blockers until resolved.

## 14. Open decisions

1. **Visual direction approval:** Adopt B · Dark Anatomy Studio, A · Clinical Atlas, or C · Motion Lab? B is a working default only.
2. **First camera experience:** Upper-body front camera first or rear-camera supported full-body framing in P1?
3. **MVP anatomy depth:** Educational clickable regions over 2D skeleton only, versus a narrowly scoped forearm bone model spike.
4. **OS/device floor:** Confirm target after real-device profiling.
5. **Asset licensing:** Confirm every external model and illustration before distributing an app.

## 15. Source files consulted

- https://github.com/Kevoyuan/MagicMirror/blob/master/README.md
- https://github.com/Kevoyuan/MagicMirror/blob/master/OpenPosePlugin/Assets/OpenPose/Examples/Scripts/HumanController2D.cs
- https://github.com/Kevoyuan/MagicMirror/blob/master/OpenPosePlugin/Assets/Script/MeshPoseCalculator.cs
- https://github.com/Kevoyuan/MagicMirror/blob/master/OpenPosePlugin/Assets/Script/JointToPose.cs
- https://github.com/Kevoyuan/MagicMirror/blob/master/OpenPosePlugin/Assets/Script/MeshPoseAdjuster.cs
- https://github.com/Kevoyuan/MagicMirror/tree/master/model_for_test

---

**Change policy:** Update this document when a product direction is confirmed, after validating native tracking/asset feasibility, or when a shipped feature changes the experience. Do not silently rewrite verified repository behavior as a design aspiration.
