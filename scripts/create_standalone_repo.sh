#!/usr/bin/env bash
# Export MagicMirror 2.0 as a new independent Git repository (NOT a fork).
# Usage: bash scripts/create_standalone_repo.sh ../MagicMirror-2.0
set -euo pipefail

if [[ $# -ne 1 ]]; then
  echo "Usage: bash scripts/create_standalone_repo.sh <new-empty-directory>" >&2
  exit 2
fi

if ! command -v git >/dev/null 2>&1 || ! command -v tar >/dev/null 2>&1; then
  echo "Git and tar are required." >&2
  exit 2
fi

source_root="$(git rev-parse --show-toplevel 2>/dev/null || true)"
if [[ -z "$source_root" ]]; then
  echo "Run from a Git checkout containing the clean source branch." >&2
  exit 2
fi
cd "$source_root"

# A detached CI checkout and a published main branch are both valid source
# trees. The SOURCE CHECKS below, not a Git branch name, prove cleanliness.
if [[ ! -f "Docs/NEW_REPOSITORY_SETUP.md" ]]; then
  echo "Not a prepared MagicMirror 2.0 standalone source snapshot." >&2
  exit 2
fi
if [[ -n "$(git status --porcelain)" ]]; then
  echo "Working tree is dirty. Commit or discard local changes before export." >&2
  exit 2
fi
python3 Tools/ci/check_clean_repo.py
python3 Tools/ci/check_mediapipe_only.py

target="$1"
if [[ -e "$target" ]]; then
  echo "Target already exists. Refusing to overwrite: $target" >&2
  exit 2
fi
mkdir -p "$target"
target="$(cd "$target" && pwd)"
# git archive exports ONLY files in the current commit; it does NOT bring .git
# and therefore prevents copying original historical OpenPose commits.
git archive --format=tar HEAD | tar -xf - -C "$target"
if [[ -d "$target/OpenPosePlugin" || -d "$target/model_for_test" || \
      -e "$target/ikun_demo.gif" ]]; then
  echo "Extraction unexpectedly includes historical files. Aborting." >&2
  exit 1
fi
(
  cd "$target"
  python3 Tools/ci/check_clean_repo.py
  python3 Tools/ci/check_mediapipe_only.py
  git init -b main
  git add -A
  git commit -m "Initial MagicMirror 2.0 MediaPipe-only source"
)
echo
echo "Fresh Git repository created: $target"
echo "No old Git history was carried over."
echo "To publish after creating an EMPTY GitHub repo:"
echo "  cd \"$target\""
echo "  git remote add origin https://github.com/Kevoyuan/MagicMirror-2.0.git"
echo "  git push -u origin main"
