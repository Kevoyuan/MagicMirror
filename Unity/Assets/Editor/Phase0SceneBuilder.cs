using System.IO;
using MagicMirror.Camera;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace MagicMirror.Editor
{
    public static class Phase0SceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Phase0CameraPreview.unity";

        [MenuItem("MagicMirror/Phase 0/Create Camera Test Scene")]
        public static void CreateCameraTestScene()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Exit Play mode before generating a scene.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog(
                    "Replace Camera Test Scene?",
                    ScenePath + " already exists. Replace it with a fresh scene?",
                    "Replace", "Cancel"))
                return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var canvasObject = new GameObject("MagicMirror Canvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);

            var previewObject = new GameObject("Webcam Mirror",
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
            var previewImage = previewObject.GetComponent<RawImage>();
            previewImage.color = Color.white;
            previewImage.raycastTarget = false;

            var statusObject = new GameObject("Status Label", typeof(RectTransform), typeof(Text));
            statusObject.transform.SetParent(canvasObject.transform, false);
            var statusRect = statusObject.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0f, 1f);
            statusRect.anchorMax = new Vector2(1f, 1f);
            statusRect.pivot = new Vector2(0.5f, 1f);
            statusRect.anchoredPosition = new Vector2(0f, -16f);
            statusRect.sizeDelta = new Vector2(-32f, 44f);

            var statusText = statusObject.GetComponent<Text>();
            statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statusText.fontSize = 24;
            statusText.alignment = TextAnchor.MiddleLeft;
            statusText.color = Color.white;
            statusText.text = "Waiting for camera...";
            statusText.raycastTarget = false;

            var controller = new GameObject("Webcam Preview Controller")
                .AddComponent<WebcamPreviewController>();
            controller.Configure(previewImage, statusText);

            Directory.CreateDirectory("Assets/Scenes");
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("Could not save scene: " + ScenePath);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.Refresh();
            Debug.Log("MagicMirror Phase 0 camera scene created at " + ScenePath);
        }
    }
}
