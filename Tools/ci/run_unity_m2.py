"""Run MagicMirror M2 synthetic checks in the *actual* Unity Editor.

No Unity license, editor install, GPU, webcam or FBX is claimed by this script.
Use an installed editor executable: python3 Tools/ci/run_unity_m2.py --unity /path/to/Unity
"""
from __future__ import annotations

import argparse
from pathlib import Path
import subprocess
import sys


MARK_PASS = "MAGICMIRROR_M2_BATCH_PASS"
MARK_FAIL = "MAGICMIRROR_M2_BATCH_FAIL"


def build_command(editor: str, project: Path, log_path: Path) -> list[str]:
    return [editor, "-batchmode", "-nographics", "-quit",
            "-projectPath", str(project.resolve()),
            "-executeMethod", "MagicMirror.Editor.M2BatchValidation.Run",
            "-logFile", str(log_path.resolve())]


def verify_result(returncode: int, log_content: str) -> tuple[bool, str]:
    if returncode != 0:
        return False, f"Unity exited with code {returncode}"
    if MARK_FAIL in log_content:
        return False, "Unity reported a batch validation failure"
    if MARK_PASS not in log_content:
        return False, "Unity did not emit the M2 success marker"
    return True, "Unity Editor synthetic validation passed (NOT camera/FBX/AR tests)"


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description="Run MagicMirror Unity M2 batch Editor checks")
    parser.add_argument("--unity", required=True,
                        help="Absolute path to the installed Unity editor executable")
    parser.add_argument("--project", type=Path,
                        default=Path(__file__).resolve().parents[2] / "Unity")
    parser.add_argument("--log", type=Path,
                        default=Path(__file__).resolve().parents[2] / "m2-editor-validation.log")
    parser.add_argument("--timeout", type=int, default=900)
    args = parser.parse_args(argv)
    if args.timeout < 1:
        parser.error("--timeout must be positive")
    if not args.project.is_dir():
        parser.error(f"Unity project not found: {args.project}")
    editor = Path(args.unity)
    if not editor.is_file():
        parser.error(f"Unity executable not found: {editor}")
    args.log.parent.mkdir(parents=True, exist_ok=True)
    if args.log.exists():
        args.log.unlink()  # Never accept stale prior PASS markers.
    command = build_command(str(editor.resolve()), args.project, args.log)
    print("Launching installed Unity Editor:", command[0], flush=True)
    try:
        result = subprocess.run(command, timeout=args.timeout, check=False)
    except subprocess.TimeoutExpired:
        print(f"FAIL: Unity Editor timed out after {args.timeout}s.", file=sys.stderr)
        return 1
    except OSError as exc:
        print(f"FAIL: Cannot start Unity Editor: {exc}", file=sys.stderr)
        return 1
    contents = args.log.read_text(encoding="utf-8", errors="replace") if args.log.exists() else ""
    passed, reason = verify_result(result.returncode, contents)
    print(("PASS: " if passed else "FAIL: ") + reason)
    print("Unity log:", args.log.resolve())
    if not passed:
        print("Last lines of Unity log:\n" + "\n".join(contents.splitlines()[-30:]),
              file=sys.stderr)
    return 0 if passed else 1


if __name__ == "__main__":
    sys.exit(main())
