using System.IO;
using MagicMirror.Tracking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MagicMirror.Editor
{
    public static class Phase1SceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Phase1PoseTracking.unity";

        [MenuItem("MagicMirror/Phase 1/Create Pose Tracking Scene")]
        public static void CreateTrackingScene()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Leave Play mode before generating a scene.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog(
                    "Replace Phase 1 Scene?",
                    "Overwrite " + ScenePath + "?",
                    "Replace", "Cancel"))
                return;

            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var canvasObject = new GameObject("MagicMirror Pose Canvas",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);

            var previewObject = new GameObject("Live Pose Image",
                typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
            previewObject.transform.SetParent(canvasObject.transform, false);
            var previewRect = previewObject.GetComponent<RectTransform>();
            previewRect.anchorMin = Vector2.zero;
            previewRect.anchorMax = Vector2.one;
            previewRect.offsetMin = Vector2.zero;
            previewRect.offsetMax = Vector2.zero;
            var fitter = previewObject.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 16f / 9f;
            var image = previewObject.GetComponent<RawImage>();
            image.color = Color.black;
            image.raycastTarget = false;

            var overlayObject = new GameObject(
                "33 Landmark Overlay", typeof(RectTransform), typeof(PoseOverlayRenderer));
            overlayObject.transform.SetParent(previewObject.transform, false);
            var overlayRect = overlayObject.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            var statusObject = new GameObject("Pose Status", typeof(RectTransform), typeof(Text));
            statusObject.transform.SetParent(canvasObject.transform, false);
            var statusRect = statusObject.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0, 1);
            statusRect.anchorMax = new Vector2(1, 1);
            statusRect.pivot = new Vector2(0.5f, 1f);
            statusRect.anchoredPosition = new Vector2(0, -16);
            statusRect.sizeDelta = new Vector2(-32, 50);
            var statusText = statusObject.GetComponent<Text>();
            statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statusText.fontSize = 22;
            statusText.alignment = TextAnchor.MiddleLeft;
            statusText.color = Color.white;
            statusText.text = "Waiting for local pose sidecar...";
            statusText.raycastTarget = false;

            var receiver = new GameObject("Local Pose Stream Receiver")
                .AddComponent<PoseStreamReceiver>();
            receiver.Configure(image, statusText, overlayObject.GetComponent<PoseOverlayRenderer>());

            Directory.CreateDirectory("Assets/Scenes");
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("Could not save pose scene.");

            // Keep both milestone scenes available in Build Settings if present.
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            foreach (var candidate in EditorBuildSettings.scenes)
                if (candidate.path != ScenePath)
                    list.Add(candidate);
            list.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();

            AssetDatabase.Refresh();
            Debug.Log("MagicMirror Phase 1 pose tracking scene created: " + ScenePath);
        }
    }
}
