using System;
using MagicMirror.Anatomy;
using MagicMirror.Motion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MagicMirror.Editor
{
    /// <summary>
    /// Editor-only controlled replacement of schematic capsules with 6
    /// independently authored, manually verified anatomy-bone prefabs.
    /// Does not download, extract or redistribute third-party meshes.
    /// </summary>
    public sealed class AnatomyArmImportWindow : EditorWindow
    {
        private const string RootName = "VERIFIED Anatomical Arm Models";
        private const int AnatomyLayer = 30;
        private AnatomicalArmMap map;

        [MenuItem("MagicMirror/Anatomy/Install Verified Arm Bones")]
        public static void ShowWindow()
        {
            GetWindow<AnatomyArmImportWindow>("MagicMirror Bone Import");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Verified Anatomical Bone Import",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Only isolated, independently reviewed Z-Anatomy bone prefabs. " +
                "A model object name alone cannot verify its anatomy, side or " +
                "license. This tool replaces SCHEMATIC arm proxies in a Phase 3 scene.",
                MessageType.Warning);

            map = (AnatomicalArmMap)EditorGUILayout.ObjectField(
                "Arm Map Asset", map, typeof(AnatomicalArmMap), false);
            if (map == null)
            {
                EditorGUILayout.HelpBox(
                    "Create an asset using Assets > Create > MagicMirror > Anatomy > " +
                    "Verified Arm Model Map, fill all six bone slots and source fields.",
                    MessageType.Info);
                return;
            }

            string problem;
            bool valid = map.Validate(out problem);
            if (!valid)
                EditorGUILayout.HelpBox(problem, MessageType.Warning);
            else
                EditorGUILayout.HelpBox(
                    "Mapping fields are complete. Manual expert review and " +
                    "platform rendering tests are still required.", MessageType.Info);

            using (new EditorGUI.DisabledScope(!valid || EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Install Six Verified Bones into Scene"))
                    Install(map);
            }
        }

        private static void Install(AnatomicalArmMap definition)
        {
            string problem = "No anatomical map provided.";
            if (definition == null || !definition.Validate(out problem))
                throw new InvalidOperationException("Invalid arm map: " + problem);

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
                throw new InvalidOperationException("No open Unity scene.");

            if (GameObject.Find(RootName) != null)
            {
                EditorUtility.DisplayDialog("Already installed",
                    "An anatomy-bone installation already exists. Undo or delete " +
                    "that root before importing again.", "OK");
                return;
            }

            AnatomicalArmRetargeter[] retargeters =
                UnityEngine.Object.FindObjectsByType<AnatomicalArmRetargeter>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);

            AnatomicalArmRetargeter left = null;
            AnatomicalArmRetargeter right = null;
            foreach (var rig in retargeters)
            {
                if (rig.gameObject.scene != scene) continue;
                if (rig.Side == ArmSide.Left) left = rig;
                else if (rig.Side == ArmSide.Right) right = rig;
            }

            if (left == null || right == null)
            {
                EditorUtility.DisplayDialog("Missing retargeters",
                    "Open a Phase 3 X-Ray scene with both arm retargeter components.",
                    "OK");
                return;
            }

            // The map must refer to prefab ASSETS; this avoids accidental
            // clone-and-reparent operations on a source scene hierarchy.
            foreach (ArmSide side in Enum.GetValues(typeof(ArmSide)))
            {
                foreach (AnatomicalArmMap.BoneType bone in
                         Enum.GetValues(typeof(AnatomicalArmMap.BoneType)))
                {
                    var entry = definition.Get(side, bone);
                    if (!EditorUtility.IsPersistent(entry.sourcePrefab) ||
                        !PrefabUtility.IsPartOfPrefabAsset(entry.sourcePrefab))
                    {
                        EditorUtility.DisplayDialog("Source must be a prefab asset",
                            side + " " + bone + " must be an isolated verified prefab " +
                            "asset, not a live scene object or part of a whole-body scene.",
                            "OK");
                        return;
                    }
                }
            }

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Install verified anatomical arm bones");
            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create anatomy mesh root");
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            try
            {
                InstallSide(ArmSide.Left, definition, left, root.transform);
                InstallSide(ArmSide.Right, definition, right, root.transform);

                var oldProxies = GameObject.Find("SCHEMATIC Anatomical Mesh Proxies");
                if (oldProxies != null)
                {
                    Undo.RecordObject(oldProxies, "Hide schematic arm proxies");
                    oldProxies.SetActive(false);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(undoGroup);
                EditorUtility.DisplayDialog("Import installed",
                    "Six bone prefabs are assigned to the left and right arm " +
                    "retargeters. Save the scene and validate anatomical alignment " +
                    "at runtime. Source license review is still your responsibility.",
                    "OK");
            }
            catch (Exception error)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogException(error);
                EditorUtility.DisplayDialog("Import failed", error.Message, "OK");
            }
        }

        private static void InstallSide(ArmSide side, AnatomicalArmMap definition,
                                        AnatomicalArmRetargeter rig, Transform parent)
        {
            var humerusEntry = definition.Get(side, AnatomicalArmMap.BoneType.Humerus);
            var radiusEntry = definition.Get(side, AnatomicalArmMap.BoneType.Radius);
            var ulnaEntry = definition.Get(side, AnatomicalArmMap.BoneType.Ulna);

            Transform humerus = InstantiateBone(humerusEntry, side + " Humerus", parent);
            Transform radius = InstantiateBone(radiusEntry, side + " Radius", parent);
            Transform ulna = InstantiateBone(ulnaEntry, side + " Ulna", parent);

            Undo.RecordObject(rig, "Assign verified anatomy bones");
            rig.InstallAnatomicalMeshes(
                humerus, Endpoints(humerusEntry),
                radius, Endpoints(radiusEntry),
                ulna, Endpoints(ulnaEntry));
            EditorUtility.SetDirty(rig);
        }

        private static AnatomicalArmRetargeter.BoneEndpoints Endpoints(
            AnatomicalArmMap.BoneAsset bone)
        {
            return new AnatomicalArmRetargeter.BoneEndpoints
            {
                localStart = bone.proximalLocal,
                localEnd = bone.distalLocal
            };
        }

        private static Transform InstantiateBone(AnatomicalArmMap.BoneAsset asset,
                                                  string label, Transform parent)
        {
            var instance = PrefabUtility.InstantiatePrefab(
                asset.sourcePrefab, parent) as GameObject;
            if (instance == null)
                throw new InvalidOperationException("Prefab instance failed: " + label);

            Undo.RegisterCreatedObjectUndo(instance, "Add " + label);
            instance.name = label + " (Reference Anatomy)";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            SetLayerRecursive(instance.transform, AnatomyLayer);
            instance.SetActive(false);
            return instance.transform;
        }

        private static void SetLayerRecursive(Transform transform, int layer)
        {
            transform.gameObject.layer = layer;
            foreach (Transform child in transform)
                SetLayerRecursive(child, layer);
        }
    }
}
