# Create the independent MagicMirror 2.0 GitHub repository

**Preferred name:** `Kevoyuan/MagicMirror-2.0`

The GitHub connection available to this assistant can edit existing repository
branches and open PRs, but does not expose a **create repository** operation.
Therefore, this clean snapshot is staged in the historical repository until
you create an empty destination repository. **No Unity installation is required
to complete the migration.**

## Recommended: one command to create brand-new Git history

From a machine with Git (macOS, Linux, Windows Git Bash):

```bash
git clone --depth 1 --branch feature/magicmirror-2.0-clean-migration --single-branch \
  https://github.com/Kevoyuan/MagicMirror.git
cd MagicMirror
bash scripts/create_standalone_repo.sh ../MagicMirror-2.0
```

This exports only the current checked-out **clean file snapshot** via
`git archive HEAD`, initializes `../MagicMirror-2.0` as a fresh Git repository,
and commits it as an independent `main` branch. It refuses to overwrite an
existing target directory and fails if the clean extraction still contains
historical OpenPose sources, large sample media, or unsupported legacy files.

### Publish to GitHub

Create an **empty public or private** repository named
`MagicMirror-2.0` under your `Kevoyuan` account
([GitHub New Repository](https://github.com/new)).
**Do not initialize it with README, .gitignore, or a license** — the snapshot
already has those source files (except for the deliberately undecided license).

Then:

```bash
cd ../MagicMirror-2.0
git remote add origin https://github.com/Kevoyuan/MagicMirror-2.0.git
git push -u origin main
```

Or if GitHub CLI (`gh`) is installed and authenticated, you can create and
push in one command after generating the fresh local snapshot:

```bash
cd ../MagicMirror-2.0
gh repo create Kevoyuan/MagicMirror-2.0 --public --source . --remote origin --push
```

Choose `--private` instead of `--public` if you want a private repository.

## Verify the new repository

```bash
test ! -d OpenPosePlugin
test ! -d model_for_test
test ! -e ikun_demo.gif
python3 Tools/ci/check_clean_repo.py
python3 Tools/ci/check_mediapipe_only.py
python3 -m unittest discover -s Tools/pose_sidecar/tests -v
python3 -m unittest discover -s Tools/ci/tests -v
git log --oneline -3
```

The new `git log` should start with the independent initial commit; it
must **not** contain the old OpenPose or video/FBX history. GitHub Actions
will run source tests automatically on new pushes.

## After publication

1. Set the GitHub repository description, topics and default `main`.
2. Choose and add an explicit code `LICENSE` before claiming open-source
   redistribution rights; audit third-party anatomical assets separately.
3. Review clean CI checks in the new repository.
4. Keep the old repository as a historical prototype with a link to the new one.
5. Validate Unity 6.3 Editor compilation and real-camera integration later
   on a machine with Unity installed.

Do not assume that removing files from a branch removes those files from the
*original repository's Git history*. This guide intentionally creates a new
independent history.
