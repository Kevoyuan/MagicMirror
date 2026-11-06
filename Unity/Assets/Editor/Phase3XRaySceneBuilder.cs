using System.IO;
using MagicMirror.Motion;
using MagicMirror.Rendering;
using MagicMirror.Tracking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MagicMirror.Editor
{
    /// <summary>
    /// Creates a testable AR visual-compositing vertical slice without
    /// redistributing third-party anatomical assets.
    ///
    /// The generated ivory capsules are explicitly SCHEMATIC PROXIES.
    /// They can later be replaced by individually licensed/imported
    /// anatomical mesh roots with calibrated bone endpoints.
    /// </summary>
    public static class Phase3XRaySceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Phase3ArmXRayPreview.unity";
        private const string MaterialPath = "Assets/Generated/Materials/AnatomyProxy.mat";
        private const int AnatomyLayer = 30;

        [MenuItem("MagicMirror/Phase 3/Create Arm X-Ray Preview Scene")]
        public static void CreateArmScene()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Leave Play mode before creating the scene.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog(
                "Replace X-Ray Preview Scene?",
                "Overwrite " + ScenePath + "?",
                "Replace", "Cancel"))
                return;

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // The default main camera does not draw the proxy-bone layer.
            Camera screenCamera = Camera.main;
            if (screenCamera != null)
                screenCamera.cullingMask &= ~(1 << AnatomyLayer);

            Material boneMaterial = EnsureProxyMaterial();

            var canvasObject = new GameObject("MagicMirror AR Canvas",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);

            GameObject preview = NewUI("Source Camera Image", canvasObject.transform,
                typeof(RawImage), typeof(AspectRatioFitter));
            Stretch(preview.GetComponent<RectTransform>());
            var previewFitter = preview.GetComponent<AspectRatioFitter>();
            previewFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            previewFitter.aspectRatio = 16f / 9f;
            RawImage previewImage = preview.GetComponent<RawImage>();
            previewImage.raycastTarget = false;
            previewImage.color = Color.black;

            // Offscreen-camera output draws above video but under keypoint
            // debug overlay. Both RawImages use the same image-space flip.
            GameObject anatomyLayerUI = NewUI("Transparent Anatomical Layer",
                preview.transform, typeof(RawImage));
            Stretch(anatomyLayerUI.GetComponent<RectTransform>());
            var anatomyImage = anatomyLayerUI.GetComponent<RawImage>();
            anatomyImage.raycastTarget = false;
            anatomyImage.color = new Color(1f, 1f, 1f, 0.82f);

            GameObject keypointsUI = NewUI("33-Point Pose Debug",
                preview.transform, typeof(PoseOverlayRenderer));
            Stretch(keypointsUI.GetComponent<RectTransform>());
            var poseOverlay = keypointsUI.GetComponent<PoseOverlayRenderer>();

            Text status = MakeText(canvasObject.transform, "Stream Status",
                "Waiting for localhost pose sidecar...", 24, TextAnchor.MiddleLeft,
                Color.white, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -16f),
                new Vector2(-40f, 48f));

            Text angles = MakeText(canvasObject.transform, "Joint Angle Readout",
                "Elbow: waiting for pose...", 20, TextAnchor.MiddleLeft,
                new Color(0.56f, 0.96f, 0.84f, 1f),
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 110f),
                new Vector2(-40f, 42f));

            Text disclaimer = MakeText(canvasObject.transform, "Scientific Disclaimer",
                "SCHEMATIC 3D BONE PROXIES — NOT AN X-RAY OR MEDICAL SCAN",
                17, TextAnchor.MiddleCenter, new Color(1f, 0.82f, 0.55f, 1f),
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 16f),
                new Vector2(-20f, 32f));
            disclaimer.raycastTarget = false;

            // EventSystem makes the uGUI slider responsive to mouse input.
            new GameObject("UI EventSystem", typeof(EventSystem),
                typeof(StandaloneInputModule));
            Slider slider = CreateOpacitySlider(canvasObject.transform);
            MakeText(canvasObject.transform, "Opacity Label",
                "BONE OPACITY", 16, TextAnchor.MiddleLeft, Color.white,
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(24f, 80f),
                new Vector2(230f, 25f));

            // The Unity C# receiver does not open the camera. Only the Python
            // pose sidecar can own the physical device for this scene.
            var receiver = new GameObject("Local Pose Stream Receiver")
                .AddComponent<PoseStreamReceiver>();
            receiver.Configure(previewImage, status, poseOverlay);

            var anglePresenter = new GameObject("Estimated Arm Angles")
                .AddComponent<ArmAnglePresenter>();
            anglePresenter.Configure(receiver, angles);

            var anatomyCameraObject = new GameObject(
                "Transparent Anatomy Render Camera", typeof(Camera));
            Camera anatomyCamera = anatomyCameraObject.GetComponent<Camera>();
            anatomyCamera.transform.position = new Vector3(0f, 0f, -2f);
            anatomyCamera.transform.rotation = Quaternion.identity;
            anatomyCamera.orthographic = true;
            anatomyCamera.orthographicSize = 0.5f;
            anatomyCamera.aspect = 16f / 9f;
            anatomyCamera.nearClipPlane = 0.1f;
            anatomyCamera.farClipPlane = 10f;
            anatomyCamera.clearFlags = CameraClearFlags.SolidColor;
            anatomyCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            anatomyCamera.cullingMask = 1 << AnatomyLayer;
            anatomyCamera.allowHDR = false;
            anatomyCamera.allowMSAA = false;
            anatomyCamera.enabled = false;

            var compositor = new GameObject("AR Anatomy Compositor")
                .AddComponent<ArmXRayCompositor>();
            compositor.Configure(receiver, anatomyCamera, anatomyImage, slider);

            var proxiesRoot = new GameObject("SCHEMATIC Anatomical Mesh Proxies");
            proxiesRoot.transform.position = Vector3.zero;
            CreateArm(ArmSide.Left, proxiesRoot.transform, boneMaterial,
                receiver, anatomyCamera);
            CreateArm(ArmSide.Right, proxiesRoot.transform, boneMaterial,
                receiver, anatomyCamera);

            Directory.CreateDirectory("Assets/Scenes");
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("Could not save " + ScenePath);

            var existing = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            foreach (var item in EditorBuildSettings.scenes)
                if (item.path != ScenePath)
                    existing.Add(item);
            existing.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = existing.ToArray();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("MagicMirror Phase 3 proxy X-Ray preview generated: " + ScenePath);
        }

        private static void CreateArm(ArmSide side, Transform parent, Material material,
                                      PoseStreamReceiver receiver, Camera camera)
        {
            Transform humerus = MakeBoneProxy(side + " Humerus PROXY", parent, material, 0.14f);
            Transform radius = MakeBoneProxy(side + " Radius PROXY", parent, material, 0.07f);
            Transform ulna = MakeBoneProxy(side + " Ulna PROXY", parent, material, 0.07f);
            var rig = new GameObject(side + " Arm Schematic Retargeter")
                .AddComponent<AnatomicalArmRetargeter>();
            var unit = AnatomicalArmRetargeter.BoneEndpoints.UnitY;
            rig.Configure(receiver, camera, side, humerus, radius, ulna,
                unit, unit, unit, 0.08f);
            humerus.gameObject.SetActive(false);
            radius.gameObject.SetActive(false);
            ulna.gameObject.SetActive(false);
        }

        private static Transform MakeBoneProxy(string name, Transform parent,
                                                Material material, float thickness)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.layer = AnatomyLayer;

            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = name + " capsule mesh";
            capsule.transform.SetParent(root.transform, false);
            capsule.transform.localPosition = Vector3.zero;
            capsule.transform.localRotation = Quaternion.identity;
            capsule.transform.localScale = new Vector3(thickness, 1f, thickness);
            capsule.layer = AnatomyLayer;

            var collider = capsule.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);
            capsule.GetComponent<MeshRenderer>().sharedMaterial = material;
            return root.transform;
        }

        private static Material EnsureProxyMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null) return material;

            Directory.CreateDirectory("Assets/Generated/Materials");
            Shader shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) throw new System.InvalidOperationException(
                "No supported built-in shader for the proxy bone material.");
            material = new Material(shader) { name = "Schematic Bone Proxy (NOT medical)" };
            material.color = new Color(0.88f, 0.94f, 0.83f, 1f);
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static Slider CreateOpacitySlider(Transform canvas)
        {
            var sliderObject = NewUI("Bone Opacity Slider", canvas, typeof(Slider));
            var rect = sliderObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(24f, 58f);
            rect.sizeDelta = new Vector2(260f, 18f);

            GameObject background = NewUI("Background", sliderObject.transform, typeof(Image));
            Stretch(background.GetComponent<RectTransform>());
            var backgroundImage = background.GetComponent<Image>();
            backgroundImage.color = new Color(0.15f, 0.20f, 0.24f, 0.90f);

            GameObject fillArea = NewUI("Fill Area", sliderObject.transform);
            Stretch(fillArea.GetComponent<RectTransform>());
            GameObject fill = NewUI("Fill", fillArea.transform, typeof(Image));
            Stretch(fill.GetComponent<RectTransform>());
            var fillImage = fill.GetComponent<Image>();
            fillImage.color = new Color(0.40f, 0.90f, 0.77f, 1f);
            fillImage.raycastTarget = false;

            GameObject handleArea = NewUI("Handle Area", sliderObject.transform);
            Stretch(handleArea.GetComponent<RectTransform>());
            GameObject handle = NewUI("Handle", handleArea.transform, typeof(Image));
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(16f, 28f);
            var handleImage = handle.GetComponent<Image>();
            handleImage.color = Color.white;

            Slider slider = sliderObject.GetComponent<Slider>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.value = 0.82f;
            return slider;
        }

        private static GameObject NewUI(string name, Transform parent, params System.Type[] components)
        {
            var types = new System.Collections.Generic.List<System.Type> { typeof(RectTransform) };
            types.AddRange(components);
            var go = new GameObject(name, types.ToArray());
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Text MakeText(Transform canvas, string name, string message,
                                     int size, TextAnchor alignment, Color color,
                                     Vector2 anchorMin, Vector2 anchorMax,
                                     Vector2 pivot, Vector2 anchoredPos, Vector2 dimensions)
        {
            GameObject go = NewUI(name, canvas, typeof(Text));
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = dimensions;
            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.text = message;
            text.raycastTarget = false;
            return text;
        }
    }
}
