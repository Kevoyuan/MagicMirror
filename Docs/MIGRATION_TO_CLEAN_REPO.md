# Move MagicMirror 2.0 to a new GitHub repository

**Status (2026-11-06):** Verified clean export in GitHub Actions. A new remote GitHub repository has **not** been created.

## Why a fresh history?

The old `Kevoyuan/MagicMirror` repository started as an earlier computer-vision prototype. MagicMirror 2.0 is a distinct MediaPipe-only Unity architecture. **Do not fork** the old repository or merge its master history into the new one. Create a **new empty** GitHub repository and push the single-commit bootstrap bundle.

## Verified migration build

- GitHub Actions run: https://github.com/Kevoyuan/MagicMirror/actions/runs/42243986615
- Download the artifact named **MagicMirror-2.0-clean-repository** from the run's Artifacts section (GitHub login may be required).
- Extract it. Relevant files: `MagicMirror-2.0-main.bundle`, `MagicMirror-2.0-clean-source.tar.gz`, `CLEAN_FILE_MANIFEST.txt`, `SHA256SUMS.txt`.
- Artifact retention is configured to 30 days. Regenerate via workflow dispatch or any relevant source update when needed.
- The workflow verifies the new Git bundle has exactly one commit and excludes archived runtime, obsolete FBX-bone importer, original Git history, MediaPipe task weights and anatomical FBX binaries.

## Publish it on GitHub without Unity

1. Open https://github.com/new under account `Kevoyuan`.
2. Name the new repository `MagicMirror-2.0`, select Public or Private as appropriate.
3. **Do not** initialize it with README, .gitignore or LICENSE; leave it completely empty.
4. Extract the Action artifact ZIP into a local folder.
5. Run from that folder:

~~~bash
git clone MagicMirror-2.0-main.bundle MagicMirror-2.0
cd MagicMirror-2.0
git remote remove origin
git remote add origin https://github.com/Kevoyuan/MagicMirror-2.0.git
git push -u origin main
~~~

If you use SSH Git authentication, replace the `origin` URL with the SSH form. Configure your Git credentials normally; do not paste tokens into the repo.

## Notes before public release

- No code license was invented during this migration. A distinct code LICENSE should be chosen, independently of third-party anatomical mesh licenses.
- The generated README correctly describes runtime validation as **pending**. Source code and CI passing are not proof of Unity Editor compilation or real webcam/FBX AR.
- Keep the old repository available for history/attribution, but stop active 2.0 development there after the new remote exists.
- Once the GitHub connector has permission to the new repo, continue M1/M2/M3 work there instead.
