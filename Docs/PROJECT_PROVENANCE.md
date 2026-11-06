# Project provenance and migration boundary

**Created:** 2026-11-06 · **Target repository:** `Kevoyuan/MagicMirror-2.0` (not created by the assistant)

MagicMirror 2.0 continues the educational AR concept from the original
[Kevoyuan/MagicMirror](https://github.com/Kevoyuan/MagicMirror), but is intentionally a **new, independent codebase snapshot** using **MediaPipe Tasks**, Python local pose streaming, and a fresh Unity 6.3 project.

The source of this clean snapshot is the stacked development branch
`feature/magicmirror-2.0-m2-frame-integrity` built on prior branches and PRs
[#9](https://github.com/Kevoyuan/MagicMirror/pull/9),
[#10](https://github.com/Kevoyuan/MagicMirror/pull/10), and
[#11](https://github.com/Kevoyuan/MagicMirror/pull/11).

### Intentionally excluded

- Entire historical `OpenPosePlugin/` Unity project and native OpenPose code.
- Historical 2023 README and older deprecated root-level README variants.
- Old large GIF/video files and experimental individual FBX sample assets
  under `model_for_test/`.
- The obsolete historical Phase 3B six-bone extraction guide.

The new Unity 2.0 project still includes some interim **MediaPipe-based**
Phase 0–3 debugging and arm geometry tools. These do **not** depend on OpenPose,
but should be consolidated after real Unity Editor validation.

### Reproducibility and independent Git history

A **clean file snapshot** alone does not erase old Git commit history if you
fork the original repository or push its development branch as-is.
Run `scripts/create_standalone_repo.sh` to export the checked-out clean tree
using `git archive`, then initialize **new Git history** with `git init`.
Do not use GitHub Fork, `git push --mirror`, or migrate the old `.git` folder
to the standalone repository.

Do not delete the historical repository until the new one has been created
and checks pass. Reference the original development PRs for auditability.

### Attribution / license gate

The Unity and Python source code here was developed for this MagicMirror
project. No bundled full Z-Anatomy FBX, MediaPipe task model, original
OpenPose binary, or third-party benchmark video is included.

The repository owner must select a code license; no such selection is
inferred by this migration. Every anatomical model requires a
source-specific attribution/license audit before redistribution.
See `Docs/ASSETS_AND_LICENSES.md`.
