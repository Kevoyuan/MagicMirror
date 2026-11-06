using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MagicMirror.Anatomy;
using UnityEditor;
using UnityEngine;

namespace MagicMirror.Editor
{
    /// <summary>
    /// Full FBX in, ONE profile out. No extraction, individual FBX exports or
    /// destructive hierarchy modifications. This does not assume FBX=rig.
    /// </summary>
    public sealed class FullBodyAtlasSetupWindow : EditorWindow
    {
        private const string PinnedAtlasPath =
            "Assets/ThirdParty/ZAnatomy/SkeletalSystem100.fbx";

        private GameObject sourcePrefab;
        private FullBodyAtlasProfile profile;

        private void OnEnable()
        {
            // The developer downloads ONE intact pinned source FBX locally.
            // Unity imports it normally; no scene mesh reconstruction needed.
            if (sourcePrefab == null)
                sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PinnedAtlasPath);
        }

        [Serializable]
        private sealed class InventoryEntry
        {
            public string hierarchyPath;
            public string rendererType;
            public string objectName;
            public string suggestedGroup;
            public bool verified;
        }

        [Serializable]
        private sealed class Inventory
        {
            public string sourceAssetPath;
            public int rendererCount;
            public List<InventoryEntry> entries = new List<InventoryEntry>();
            public string warning =
                "This report preserves source hierarchy paths. Suggested motion " +
                "groups are name-based hints ONLY; never treat them as anatomy verification.";
        }

        [MenuItem("MagicMirror/Phase 4/Full Atlas Setup")]
        private static void Open()
        {
            GetWindow<FullBodyAtlasSetupWindow>("Full Atlas Setup");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("MagicMirror — Full Body Anatomy",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Use the entire Z-Anatomy SkeletalSystem100.fbx as ONE model. " +
                "Its original spatial relationships are preserved. Only " +
                "animation group assignments and joint reference centers " +
                "are added; NO mesh splitting is required.", MessageType.Info);

            sourcePrefab = (GameObject)EditorGUILayout.ObjectField(
                "Original whole-body FBX", sourcePrefab, typeof(GameObject), false);
            profile = (FullBodyAtlasProfile)EditorGUILayout.ObjectField(
                "Atlas Profile", profile, typeof(FullBodyAtlasProfile), false);

            if (sourcePrefab == null)
            {
                EditorGUILayout.HelpBox(
                    "Pinned atlas not found. From repository root run: " +
                    "python3 -m Tools.anatomy_tools.fetch_atlas --download. " +
                    "Unity will import the entire 41 MB FBX as one model.",
                    MessageType.Info);
                if (GUILayout.Button("Re-scan Pinned Whole Atlas"))
                    sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PinnedAtlasPath);
            }

            using (new EditorGUI.DisabledScope(sourcePrefab == null))
            {
                if (GUILayout.Button("Create Profile for Whole Model"))
                    CreateProfile();
                if (GUILayout.Button("Export Hierarchy Inventory (JSON)"))
                    ExportInventory();
            }

            if (profile != null)
            {
                string error;
                bool valid = profile.Validate(out error);
                EditorGUILayout.HelpBox(valid
                        ? "Profile fields complete (runtime still unverified)."
                        : "Not yet ready: " + error,
                    valid ? MessageType.Info : MessageType.Warning);

                using (new EditorGUI.DisabledScope(!valid || EditorApplication.isPlaying))
                {
                    if (GUILayout.Button("Build Phase 4 AR Scene from Profile"))
                        Phase4FullBodySceneBuilder.CreateScene(profile);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "To test WITHOUT third-party assets, use menu: " +
                "MagicMirror > Phase 4 > Create Full Body Schematic Demo.",
                MessageType.None);
        }

        private void CreateProfile()
        {
            if (!AssetDatabase.Contains(sourcePrefab))
            {
                EditorUtility.DisplayDialog("Not an asset",
                    "Select the original imported FBX/prefab asset from Project, " +
                    "not an instance in a Unity scene.", "OK");
                return;
            }
            string path = EditorUtility.SaveFilePanelInProject(
                "Save full atlas motion profile", "FullBodyAtlasProfile", "asset",
                "Save a non-destructive map for the complete source FBX.");
            if (string.IsNullOrEmpty(path)) return;

            var asset = ScriptableObject.CreateInstance<FullBodyAtlasProfile>();
            asset.wholeSkeletonPrefab = sourcePrefab;
            asset.schematicDemo = false;
            asset.referencePoseReviewed = false;
            asset.assetLicensesReviewed = false;
            // Real shoulder anchors and per-region mappings intentionally
            // remain unset: never infer exact anatomy from object names alone.
            asset.groups.Clear();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            profile = asset;
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            Debug.Log("Full-body anatomy profile created (incomplete until " +
                      "rest joints, provenance and groups are reviewed): " + path);
        }

        private void ExportInventory()
        {
            if (!AssetDatabase.Contains(sourcePrefab))
            {
                EditorUtility.DisplayDialog("Not an asset",
                    "Select a full FBX or prefab asset from Project.", "OK");
                return;
            }
            Transform root = sourcePrefab.transform;
            var inventory = new Inventory
            {
                sourceAssetPath = AssetDatabase.GetAssetPath(sourcePrefab)
            };

            foreach (Renderer renderer in
                     sourcePrefab.GetComponentsInChildren<Renderer>(true))
            {
                inventory.entries.Add(new InventoryEntry
                {
                    hierarchyPath = RelativePath(root, renderer.transform),
                    objectName = renderer.gameObject.name,
                    rendererType = renderer.GetType().Name,
                    suggestedGroup = SuggestGroup(renderer.gameObject.name),
                    verified = false
                });
            }
            inventory.rendererCount = inventory.entries.Count;
            inventory.entries.Sort((a, b) =>
                StringComparer.Ordinal.Compare(a.hierarchyPath, b.hierarchyPath));

            string output = EditorUtility.SaveFilePanelInProject(
                "Save full FBX hierarchy report",
                "FullSkeletonHierarchyInventory", "json",
                "Export ALL renderer paths with optional name-based hints.");
            if (string.IsNullOrEmpty(output)) return;

            File.WriteAllText(output, JsonUtility.ToJson(inventory, true),
                              Encoding.UTF8);
            AssetDatabase.ImportAsset(output);
            Debug.Log("Exported " + inventory.rendererCount +
                      " full-atlas renderer paths to " + output +
                      ". No bone objects were split or moved.");
        }

        private static string RelativePath(Transform root, Transform target)
        {
            var parts = new List<string>();
            for (Transform t = target; t != root && t != null; t = t.parent)
                parts.Add(t.name);
            parts.Reverse();
            return string.Join("/", parts);
        }

        // Hints only. Z-Anatomy uses multiple languages and coded labels;
        // nothing is assigned or marked reviewed automatically.
        private static string SuggestGroup(string name)
        {
            string s = name.ToLowerInvariant();
            bool left = s.Contains("left") || s.Contains("sinister");
            bool right = s.Contains("right") || s.Contains("dexter");
            if (left == right) return "";
            string side = left ? "Left" : "Right";
            if (s.Contains("humerus")) return side + "UpperArm";
            if (s.Contains("radius") || s.Contains("ulna"))
                return side + "Forearm";
            if (s.Contains("femur")) return side + "Thigh";
            if (s.Contains("tibia") || s.Contains("fibula"))
                return side + "Shin";
            return "";
        }
    }
}
