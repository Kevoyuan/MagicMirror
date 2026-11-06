using MagicMirror.Tracking;
using UnityEngine;
using UnityEngine.UI;

namespace MagicMirror.Rendering
{
    /// <summary>
    /// Composites a transparent off-screen anatomy-camera image over the
    /// EXACT source frame presented by PoseStreamReceiver.
    ///
    /// The camera draws layer 30 only. Each anatomical model is projected in
    /// SOURCE (unmirrored) coordinates. Mirror the anatomy RawImage's UVs
    /// exactly as the webcam RawImage, so the video and virtual bones match.
    ///
    /// This is an illustrative rendering prototype, NOT internal scanning.
    /// </summary>
    public sealed class ArmXRayCompositor : MonoBehaviour
    {
        [SerializeField] private PoseStreamReceiver source;
        [SerializeField] private Camera anatomyCamera;
        [SerializeField] private RawImage anatomyImage;
        [SerializeField] private Slider opacitySlider;
        [SerializeField, Range(0f, 1f)] private float opacity = 0.82f;
        [SerializeField, Range(256, 2048)] private int maxTextureWidth = 1280;

        private RenderTexture renderTexture;
        private int activeWidth;
        private int activeHeight;

        public void Configure(PoseStreamReceiver receiver, Camera camera,
                              RawImage target, Slider slider)
        {
            source = receiver;
            anatomyCamera = camera;
            anatomyImage = target;
            opacitySlider = slider;
        }

        private void OnEnable()
        {
            if (source != null)
                source.FrameReceived += OnFrameReceived;
        }

        private void Start()
        {
            if (anatomyCamera != null)
                anatomyCamera.enabled = false; // No screen rendering before first stream packet.
            if (opacitySlider != null)
            {
                opacitySlider.SetValueWithoutNotify(opacity);
                opacitySlider.onValueChanged.AddListener(SetOpacity);
            }
            SetOpacity(opacity);
        }

        public void SetOpacity(float value)
        {
            opacity = Mathf.Clamp01(value);
            if (anatomyImage == null) return;
            var color = anatomyImage.color;
            color.a = opacity;
            anatomyImage.color = color;
        }

        private void OnFrameReceived(PoseFrame frame)
        {
            if (anatomyCamera == null || anatomyImage == null || frame == null || !frame.IsValid)
                return;

            int width = Mathf.Min(frame.width, maxTextureWidth);
            int height = Mathf.Max(1, Mathf.RoundToInt((float)width * frame.height / frame.width));

            if (renderTexture == null || activeWidth != width || activeHeight != height)
            {
                ReplaceRenderTexture(width, height);
            }

            // The rendering camera projects x=[0..1], y=[0..1] onto a
            // transparent texture. The video and texture get the SAME flip,
            // instead of reversing landmark x twice.
            anatomyCamera.aspect = (float)frame.width / frame.height;
            anatomyImage.uvRect = frame.mirrored
                ? new Rect(0f, 0f, 1f, 1f)
                : new Rect(1f, 0f, -1f, 1f);
            anatomyCamera.enabled = true;
        }

        private void ReplaceRenderTexture(int width, int height)
        {
            ReleaseTexture();
            activeWidth = width;
            activeHeight = height;

            renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                name = "MagicMirror_TransparentAnatomy",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                antiAliasing = 1
            };
            renderTexture.Create();
            anatomyCamera.targetTexture = renderTexture;
            anatomyImage.texture = renderTexture;
        }

        private void OnDisable()
        {
            if (source != null) source.FrameReceived -= OnFrameReceived;
            if (opacitySlider != null) opacitySlider.onValueChanged.RemoveListener(SetOpacity);
            ReleaseTexture();
        }

        private void OnDestroy() { ReleaseTexture(); }

        private void ReleaseTexture()
        {
            if (anatomyCamera != null)
            {
                anatomyCamera.enabled = false;
                anatomyCamera.targetTexture = null;
            }
            if (anatomyImage != null) anatomyImage.texture = null;
            if (renderTexture == null) return;
            renderTexture.Release();
            Destroy(renderTexture);
            renderTexture = null;
            activeWidth = 0;
            activeHeight = 0;
        }
    }
}
