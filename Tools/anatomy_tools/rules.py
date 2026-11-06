"""CONSERVATIVE anatomical name hints; never auto-assigns mesh identities.

The public Z-Anatomy asset names and structures may vary by revision.
Matching a name is NOT proof that geometry is a particular bone. All
candidates must be visually inspected in Blender and verified by an expert.
"""
from __future__ import annotations

import re
from typing import Optional


BONES = ("humerus", "radius", "ulna")


def normalize_words(name: str) -> tuple[str, ...]:
    """Split simple camelCase, underscores, periods, and spaces."""
    spaced = re.sub(r"(?<=[a-z])(?=[A-Z])", " ", name)
    return tuple(re.findall(r"[a-z]+", spaced.lower()))


def hint_bone(name: str) -> Optional[str]:
    words = normalize_words(name)
    for bone in BONES:
        if bone in words:
            return bone
    return None


def hint_side(name: str) -> Optional[str]:
    words = normalize_words(name)
    left = any(w in ("left", "sinister") for w in words)
    right = any(w in ("right", "dexter") for w in words)
    if left == right:  # unknown or ambiguous
        return None
    return "left" if left else "right"


def suggest(name: str) -> dict[str, Optional[str]]:
    return {"boneHint": hint_bone(name), "sideHint": hint_side(name)}
