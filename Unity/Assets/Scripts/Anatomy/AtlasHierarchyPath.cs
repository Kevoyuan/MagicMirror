using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicMirror.Anatomy
{
    /// <summary>
    /// Stable-within-one-import paths for a whole anatomical FBX hierarchy.
    /// Canonical segment syntax: UriEscapedObjectName@siblingIndex.
    ///
    /// This prevents Transform.Find silently selecting the wrong anatomy
    /// when different meshes share names. A pinned FBX revision is required:
    /// changing sibling order intentionally INVALIDATES the old mapping.
    /// Legacy name-only paths are accepted only when each level is unique.
    /// </summary>
    public static class AtlasHierarchyPath
    {
        public static string RelativePath(Transform root, Transform child)
        {
            if (root == null || child == null)
                throw new ArgumentNullException(root == null ? "root" : "child");

            var components = new List<string>();
            Transform cursor = child;
            while (cursor != null && cursor != root)
            {
                components.Add(
                    Uri.EscapeDataString(cursor.name) + "@" + cursor.GetSiblingIndex());
                cursor = cursor.parent;
            }

            if (cursor != root || components.Count == 0)
                throw new ArgumentException(
                    "Target must be a strict descendant of the intact atlas root.");

            components.Reverse();
            return string.Join("/", components);
        }

        public static bool TryResolve(
            Transform root, string path, out Transform result, out string error)
        {
            result = null;
            error = null;

            if (root == null || string.IsNullOrWhiteSpace(path) ||
                path.StartsWith("/") || path.EndsWith("/") || path.Contains("//"))
            {
                error = "Empty or invalid full-atlas hierarchy path: " + path;
                return false;
            }

            Transform current = root;
            foreach (string raw in path.Split('/'))
            {
                int at = raw.LastIndexOf('@');
                int index = -1;
                bool hasIndex = at > 0 &&
                                int.TryParse(raw.Substring(at + 1), out index);

                if (hasIndex)
                {
                    string expectedName;
                    try { expectedName = Uri.UnescapeDataString(raw.Substring(0, at)); }
                    catch (UriFormatException)
                    {
                        error = "Invalid encoded hierarchy name: " + raw;
                        return false;
                    }

                    if (index < 0 || index >= current.childCount)
                    {
                        error = "Sibling index out of range at: " + raw;
                        return false;
                    }
                    Transform candidate = current.GetChild(index);
                    if (!string.Equals(candidate.name, expectedName, StringComparison.Ordinal))
                    {
                        error = "Pinned FBX child order/name changed at: " + raw +
                                "; observed: " + candidate.name;
                        return false;
                    }
                    current = candidate;
                }
                else
                {
                    // Backward-compatible legacy name paths. Refuse ambiguous
                    // names instead of silently selecting an unrelated bone.
                    string expected;
                    try { expected = Uri.UnescapeDataString(raw); }
                    catch (UriFormatException)
                    {
                        error = "Invalid legacy hierarchy name: " + raw;
                        return false;
                    }

                    Transform only = null;
                    for (int j = 0; j < current.childCount; j++)
                    {
                        Transform candidate = current.GetChild(j);
                        if (candidate.name != expected) continue;
                        if (only != null)
                        {
                            error = "Ambiguous legacy child name: " + expected +
                                    ". Re-export a sibling-indexed inventory.";
                            return false;
                        }
                        only = candidate;
                    }
                    if (only == null)
                    {
                        error = "Missing full-atlas hierarchy child: " + expected;
                        return false;
                    }
                    current = only;
                }
            }

            result = current;
            return true;
        }
    }
}
