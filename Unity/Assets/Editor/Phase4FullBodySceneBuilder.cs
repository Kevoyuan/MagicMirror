using System;
using System.Collections.Generic;
using System.IO;
using MagicMirror.Anatomy;
using MagicMirror.Motion;
using MagicMirror.Tracking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MagicMirror.Editor
{
    /// <summary>
    /// Builds Phase 4 ON TOP of the Phase 3 camera/pose/anatomy compositor.
    /// A single whole-skeleton asset is used, never six exported bone files.
    ///
    /// The procedural test atlas is clearly a SCHEMATIC model only.
    /// Replace the entire prefab with original Z-Anatomy FBX via a reviewed
    /// FullBodyAtlasProfile, without changing its mesh hierarchy.
    /// </summary>
    public static class Phase4FullBodySceneBuilder
    {
        private const string Phase3Scene = "Assets/Scenes/Phase3ArmXRayPreview.unity";
        private const string Phase4Scene = "Assets/Scenes/Phase4FullBodyAtlas.unity";
        private const string DemoPrefab = "Assets/Generated/Models/SchematicFullBodyAtlas.prefab";
        private const string DemoProfile = "Assets/Generated/Profiles/SchematicFullBodyAtlasProfile.asset";

        [MenuItem("MagicMirror/Phase 4/Create Full Body Schematic Demo")]
        public static void CreateFullBodyDemo()
        {
            FullBodyAtlasProfile demo = CreateDemoAtlasIfNeeded();
            if (demo != null) CreateScene(demo);
        }

        public static void CreateScene(FullBodyAtlasProfile profile)
        {
            string validation;
            if (profile == null || !profile.Validate(out validation))
            {
                EditorUtility.DisplayDialog("Atlas profile is incomplete",
                    validation ?? "No atlas profile", "OK");
                return;
            }
            if (Application.isPlaying ||
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // Reuse the exact Phase 3 receiver, UI and alpha compositor.
            // This retains one camera owner and identical mirroring rules.
            if (!File.Exists(Phase3Scene))
                Phase3XRaySceneBuilder.CreateArmScene();
            if (!File.Exists(Phase3Scene))
            {
                EditorUtility.DisplayDialog("Missing Phase 3 source",
                    "Generate the Phase 3 Arm X-Ray Preview Scene first.", "OK");
                return;
            }

            if (File.Exists(Phase4Scene))
            {
                bool replace = EditorUtility.DisplayDialog(
                    "Replace Phase 4 Scene?",
                    Phase4Scene + " exists. Recreate from the current Phase 3 scene?",
                    "Replace", "Cancel");
                if (!replace) return;
                AssetDatabase.DeleteAsset(Phase4Scene);
            }

            if (!AssetDatabase.CopyAsset(Phase3Scene, Phase4Scene))
                throw new IOException("Could not create Phase 4 scene copy.");

            Scene scene = EditorSceneManager.OpenScene(Phase4Scene, OpenSceneMode.Single);
            var receiver = UnityEngine.Object.FindFirstObjectByType<PoseStreamReceiver>();
            GameObject cameraObject = GameObject.Find("Transparent Anatomy Render Camera");
            Camera anatomyCamera = cameraObject != null ?
                cameraObject.GetComponent<Camera>() : null;

            if (receiver == null || anatomyCamera == null)
                throw new InvalidOperationException(
                    "Phase 3 scene is missing pose receiver or anatomy camera.");

            GameObject armProxyRoot = GameObject.Find(
                "SCHEMATIC Anatomical Mesh Proxies");
            if (armProxyRoot != null) armProxyRoot.SetActive(false);

            // Existing arms should not continue retargeting hidden proxies.
            foreach (var arm in UnityEngine.Object.FindObjectsByType<AnatomicalArmRetargeter>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
                arm.enabled = false;

            var retargeter = new GameObject("Non-destructive Whole-Atlas Retargeter")
                .AddComponent<FullBodyAtlasRetargeter>();
            retargeter.Configure(receiver, anatomyCamera, profile);

            GameObject label = GameObject.Find("Scientific Disclaimer");
            if (label != null)
            {
                Text txt = label.GetComponent<Text>();
                if (txt != null)
                    txt.text = profile.schematicDemo ?
                        "SCHEMATIC FULL BODY — NOT AN X-RAY OR MEDICAL SCAN" :
                        "REFERENCE ANATOMY — ILLUSTRATIVE, NOT A MEDICAL SCAN";
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, Phase4Scene))
                throw new IOException("Could not save " + Phase4Scene);

            var scenes = new List<EditorBuildSettingsScene>();
            foreach (var item in EditorBuildSettings.scenes)
                if (item.path != Phase4Scene)
                    scenes.Add(item);
            scenes.Add(new EditorBuildSettingsScene(Phase4Scene, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("MagicMirror Phase 4 full atlas scene created: " +
                      Phase4Scene + " | Profile: " + profile.name);
        }

        /// <summary>
        /// One full-body prefab, with synthetic spatial layout and 10 groups.
        /// Does NOT represent real anatomical shapes or any Z-Anatomy data.
        /// It can run with the Python --demo sidecar, without model downloads.
        /// </summary>
        private static FullBodyAtlasProfile CreateDemoAtlasIfNeeded()
        {
            FullBodyAtlasProfile existing =
                AssetDatabase.LoadAssetAtPath<FullBodyAtlasProfile>(DemoProfile);
            if (existing != null) return existing;

            Directory.CreateDirectory("Assets/Generated/Models");
            Directory.CreateDirectory("Assets/Generated/Profiles");
            GameObject root = new GameObject("Full Schematic Skeleton Atlas");
            var segmentsRoot = new GameObject("Segments");
            segmentsRoot.transform.SetParent(root.transform, false);

            var profile = ScriptableObject.CreateInstance<FullBodyAtlasProfile>();
            profile.name = "Schematic Full Body Reference — NOT Anatomy";
            profile.schematicDemo = true;
            profile.referencePoseReviewed = false;
            profile.assetLicensesReviewed = false;
            profile.atlasLeftShoulderLocal = SourceXY(0.42f, 0.30f);
            profile.atlasRightShoulderLocal = SourceXY(0.58f, 0.30f);

            try
            {
                foreach (BodySegment segment in new[] {
                    BodySegment.Torso, BodySegment.Head,
                    BodySegment.LeftUpperArm, BodySegment.RightUpperArm,
                    BodySegment.LeftForearm, BodySegment.RightForearm,
                    BodySegment.LeftThigh, BodySegment.RightThigh,
                    BodySegment.LeftShin, BodySegment.RightShin
                })
                {
                    Vector2 a, b;
                    GetDemoEndpoints(segment, out a, out b);
                    Vector3 start = SourceXY(a.x, a.y);
                    Vector3 end = SourceXY(b.x, b.y);

                    var groupRoot = new GameObject(segment.ToString());
                    groupRoot.transform.SetParent(segmentsRoot.transform, false);
                    GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    primitive.name = "Schematic " + segment;
                    primitive.transform.SetParent(groupRoot.transform, false);
                    primitive.transform.localPosition = (start + end) / 2f;
                    primitive.transform.localRotation = Quaternion.FromToRotation(
                        Vector3.up, (end - start).normalized);
                    float thickness = segment == BodySegment.Torso ? 0.065f :
                                      segment == BodySegment.Head ? 0.095f : 0.025f;
                    primitive.transform.localScale =
                        new Vector3(thickness, (end - start).magnitude / 2f, thickness);
                    Collider collider = primitive.GetComponent<Collider>();
                    if (collider != null)
                        UnityEngine.Object.DestroyImmediate(collider);

                    profile.groups.Add(new FullBodyAtlasProfile.MotionGroup
                    {
                        segment = segment,
                        atlasStartLocal = start,
                        atlasEndLocal = end,
                        rendererRootPaths = new List<string> {
                            AtlasHierarchyPath.RelativePath(root.transform, groupRoot.transform)
                        },
                        reviewed = false // synthetic demo does not require anatomy verification
                    });
                }

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, DemoPrefab);
                if (prefab == null)
                    throw new InvalidOperationException("Could not save demo atlas prefab.");
                profile.wholeSkeletonPrefab = prefab;
                AssetDatabase.CreateAsset(profile, DemoProfile);
                AssetDatabase.SaveAssets();
                return profile;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Vector3 SourceXY(float x, float y)
        {
            // Phase 3 orthographic camera: Y spans 1 world unit and X spans
            // 16/9 world units. This schematic atlas matches demo landmarks.
            return new Vector3((x - 0.5f) * (16f / 9f), 0.5f - y, 0f);
        }

        private static void GetDemoEndpoints(BodySegment segment, out Vector2 a, out Vector2 b)
        {
            switch (segment)
            {
                case BodySegment.Torso:
                    a = new Vector2(0.50f, 0.58f); b = new Vector2(0.50f, 0.30f); return;
                case BodySegment.Head:
                    a = new Vector2(0.50f, 0.30f); b = new Vector2(0.50f, 0.16f); return;
                case BodySegment.LeftUpperArm:
                    a = new Vector2(0.42f, 0.30f); b = new Vector2(0.34f, 0.43f); return;
                case BodySegment.RightUpperArm:
                    a = new Vector2(0.58f, 0.30f); b = new Vector2(0.66f, 0.43f); return;
                case BodySegment.LeftForearm:
                    a = new Vector2(0.34f, 0.43f); b = new Vector2(0.28f, 0.58f); return;
                case BodySegment.RightForearm:
                    a = new Vector2(0.66f, 0.43f); b = new Vector2(0.72f, 0.58f); return;
                case BodySegment.LeftThigh:
                    a = new Vector2(0.45f, 0.58f); b = new Vector2(0.43f, 0.75f); return;
                case BodySegment.RightThigh:
                    a = new Vector2(0.55f, 0.58f); b = new Vector2(0.57f, 0.75f); return;
                case BodySegment.LeftShin:
                    a = new Vector2(0.43f, 0.75f); b = new Vector2(0.42f, 0.93f); return;
                case BodySegment.RightShin:
                    a = new Vector2(0.57f, 0.75f); b = new Vector2(0.58f, 0.93f); return;
                default:
                    throw new ArgumentException("Unsupported schematic segment: " + segment);
            }
        }
    }
}
