using System;
using System.Collections.Generic;
using MagicMirror.Anatomy;
using UnityEditor;
using UnityEngine;

namespace MagicMirror.Editor
{
    /// <summary>
    /// Scene-view authoring of root-local anatomical joint centers on ONE
    /// intact Z-Anatomy FBX prefab. All authoring data lives in a separate
    /// FullBodyAtlasProfile; the imported original mesh stays unmodified.
    ///
    /// PositionHandles are hints for human calibration, not automatically
    /// inferred anatomical or medical ground truth.
    /// </summary>
    public sealed class FullBodyAtlasCalibrationWindow : EditorWindow
    {
        private enum EditableAnchor
        {
            LeftShoulder,
            RightShoulder,
            SegmentStart,
            SegmentEnd
        }

        private FullBodyAtlasProfile profile;
        private GameObject preview;
        private int selectedGroupIndex;
        private EditableAnchor anchor = EditableAnchor.LeftShoulder;
        private string lastHierarchyCheck;
        private bool hierarchyCheckPassed;

        [MenuItem("MagicMirror/Phase 4/Calibrate Full Atlas Anchors")]
        private static void ShowCalibrationWindow()
        {
            GetWindow<FullBodyAtlasCalibrationWindow>("Atlas Joint Anchors");
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += DrawSceneHandles;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DrawSceneHandles;
            DestroyPreview();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Intact Atlas — Joint Center Calibration",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Do not split or deform the source FBX. All anchors and "
                + "renderer hierarchy paths are stored separately in a profile. "
                + "Scene handles are manually placed and require anatomical review.",
                MessageType.Info);

            FullBodyAtlasProfile next = (FullBodyAtlasProfile)EditorGUILayout.ObjectField(
                "Full Atlas Profile", profile, typeof(FullBodyAtlasProfile), false);
            if (next != profile)
            {
                DestroyPreview();
                profile = next;
                selectedGroupIndex = 0;
            }

            if (profile == null || profile.wholeSkeletonPrefab == null)
            {
                EditorGUILayout.HelpBox(
                    "First create a FullBodyAtlasProfile from the complete FBX "
                    + "using MagicMirror > Phase 4 > Full Atlas Setup.",
                    MessageType.Warning);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Show Whole Atlas Preview"))
                    CreatePreview();
                using (new EditorGUI.DisabledScope(preview == null))
                {
                    if (GUILayout.Button("Remove Preview"))
                        DestroyPreview();
                }
            }

            if (preview == null)
            {
                EditorGUILayout.HelpBox(
                    "Create preview to drag joint anchors and select bone objects.",
                    MessageType.None);
                return;
            }

            EditorGUILayout.LabelField("Select a joint center", EditorStyles.boldLabel);
            anchor = (EditableAnchor)EditorGUILayout.EnumPopup("Edit Anchor", anchor);

            if (profile.groups != null && profile.groups.Count > 0)
            {
                var labels = new string[profile.groups.Count];
                for (int i = 0; i < labels.Length; i++)
                    labels[i] = profile.groups[i].segment.ToString();
                selectedGroupIndex = Mathf.Clamp(
                    selectedGroupIndex, 0, profile.groups.Count - 1);
                selectedGroupIndex = EditorGUILayout.Popup(
                    "Motion Region", selectedGroupIndex, labels);
            }
            else if (anchor == EditableAnchor.SegmentStart ||
                     anchor == EditableAnchor.SegmentEnd)
            {
                EditorGUILayout.HelpBox(
                    "Add a MotionGroup to the profile Inspector before editing "
                    + "segment anchors.", MessageType.Warning);
            }

            if (GUILayout.Button("Frame Full Atlas in Scene View"))
            {
                Selection.activeGameObject = preview;
                if (SceneView.lastActiveSceneView != null)
                    SceneView.lastActiveSceneView.FrameSelected();
            }

            Transform selected = Selection.activeTransform;
            if (selected != null && IsInPreview(selected))
            {
                EditorGUILayout.LabelField("Selected source node:", selected.name);
                using (new EditorGUI.DisabledScope(
                    !CanEditSelectedAnchor()))
                {
                    if (GUILayout.Button("Use Selected Node Pivot as Anchor"))
                        WriteAnchor(preview.transform.InverseTransformPoint(
                            selected.position));
                }

                if (profile.groups != null && profile.groups.Count > 0 &&
                    selected != preview.transform)
                {
                    if (GUILayout.Button("Add Selected Subtree to Motion Region"))
                    {
                        string path = SourceRelativePath(selected);
                        var group = profile.groups[selectedGroupIndex];
                        Undo.RecordObject(profile, "Add Anatomical Motion Path");
                        if (!group.rendererRootPaths.Contains(path))
                        {
                            group.rendererRootPaths.Add(path);
                            EditorUtility.SetDirty(profile);
                        }
                    }
                }
            }

            string error;
            if (!profile.Validate(out error))
            {
                EditorGUILayout.HelpBox("Incomplete profile: " + error,
                    MessageType.Warning);
            }

            // The original atlas may contain thousands of renderers, so
            // full hierarchy validation is explicit, not run every repaint.
            if (GUILayout.Button("Validate All Source Hierarchy Paths"))
            {
                hierarchyCheckPassed = profile.ValidateHierarchy(out lastHierarchyCheck);
                if (hierarchyCheckPassed)
                    lastHierarchyCheck = "All configured paths resolve in the intact FBX. "
                        + "Anatomical identity and joint centers remain manually reviewed.";
            }

            if (!string.IsNullOrEmpty(lastHierarchyCheck))
                EditorGUILayout.HelpBox(lastHierarchyCheck,
                    hierarchyCheckPassed ? MessageType.Info : MessageType.Warning);

            EditorGUILayout.HelpBox(
                "Green = left shoulder, Cyan = right shoulder, "
                + "Yellow = selected region anchor. Changes affect ONLY "
                + "the ScriptableObject, not Z-Anatomy original FBX.",
                MessageType.None);
        }

        private void CreatePreview()
        {
            DestroyPreview();
            if (profile == null || profile.wholeSkeletonPrefab == null)
                return;

            preview = PrefabUtility.InstantiatePrefab(
                profile.wholeSkeletonPrefab) as GameObject;
            if (preview == null)
            {
                Debug.LogWarning("MagicMirror: failed to instantiate complete atlas.");
                return;
            }

            preview.name = "[TEMP] Whole Z-Anatomy Joint Calibration";
            // Scene/editor-only visualization. Never save the temporary
            // preview in a game build or serialized scene.
            preview.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            preview.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            preview.transform.localScale = Vector3.one;
            Selection.activeGameObject = preview;

            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.FrameSelected();

            SceneView.RepaintAll();
        }

        private void DestroyPreview()
        {
            if (preview != null)
                DestroyImmediate(preview);
            preview = null;
        }

        private bool IsInPreview(Transform t)
        {
            if (preview == null) return false;
            for (Transform node = t; node != null; node = node.parent)
                if (node == preview.transform) return true;
            return false;
        }

        private string SourceRelativePath(Transform node)
        {
            return AtlasHierarchyPath.RelativePath(preview.transform, node);
        }

        private bool CanEditSelectedAnchor()
        {
            return anchor == EditableAnchor.LeftShoulder ||
                   anchor == EditableAnchor.RightShoulder ||
                   (profile.groups != null && profile.groups.Count > 0);
        }

        private Vector3 ReadAnchor()
        {
            if (profile == null) return Vector3.zero;
            switch (anchor)
            {
                case EditableAnchor.LeftShoulder:
                    return profile.atlasLeftShoulderLocal;
                case EditableAnchor.RightShoulder:
                    return profile.atlasRightShoulderLocal;
                case EditableAnchor.SegmentStart:
                    return profile.groups[selectedGroupIndex].atlasStartLocal;
                default:
                    return profile.groups[selectedGroupIndex].atlasEndLocal;
            }
        }

        private void WriteAnchor(Vector3 newLocal)
        {
            if (profile == null || !CanEditSelectedAnchor())
                return;
            Undo.RecordObject(profile, "Move Atlas Joint Center");
            switch (anchor)
            {
                case EditableAnchor.LeftShoulder:
                    profile.atlasLeftShoulderLocal = newLocal;
                    break;
                case EditableAnchor.RightShoulder:
                    profile.atlasRightShoulderLocal = newLocal;
                    break;
                case EditableAnchor.SegmentStart:
                    profile.groups[selectedGroupIndex].atlasStartLocal = newLocal;
                    break;
                case EditableAnchor.SegmentEnd:
                    profile.groups[selectedGroupIndex].atlasEndLocal = newLocal;
                    break;
            }
            EditorUtility.SetDirty(profile);
            Repaint();
            SceneView.RepaintAll();
        }

        private void DrawSceneHandles(SceneView scene)
        {
            if (preview == null || profile == null) return;
            Transform root = preview.transform;
            Handles.color = Color.green;
            DrawFixedMarker(root.TransformPoint(profile.atlasLeftShoulderLocal),
                            "LEFT SHOULDER");
            Handles.color = Color.cyan;
            DrawFixedMarker(root.TransformPoint(profile.atlasRightShoulderLocal),
                            "RIGHT SHOULDER");

            if (!CanEditSelectedAnchor())
                return;
            Vector3 local = ReadAnchor();
            Vector3 world = root.TransformPoint(local);
            Handles.color = Color.yellow;
            EditorGUI.BeginChangeCheck();
            Vector3 next = Handles.PositionHandle(world, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
                WriteAnchor(root.InverseTransformPoint(next));
            Handles.Label(world, anchor.ToString());
        }

        private static void DrawFixedMarker(Vector3 world, string label)
        {
            float size = HandleUtility.GetHandleSize(world) * 0.075f;
            Handles.SphereHandleCap(0, world, Quaternion.identity,
                                    size, EventType.Repaint);
            Handles.Label(world, label);
        }
    }
}
