using UnityEngine;
using UnityEngine.UI;

namespace MagicMirror.Camera
{
    /// <summary>
    /// Phase 0 webcam smoke test. No frames are recorded or transmitted.
    /// No pose estimation or anatomy registration is performed here.
    /// </summary>
    public sealed class WebcamPreviewController : MonoBehaviour
    {
        [SerializeField] private RawImage previewImage;
        [SerializeField] private Text statusText;
        [SerializeField] private int requestedWidth = 1280;
        [SerializeField] private int requestedHeight = 720;
        [SerializeField] private int requestedFrameRate = 30;

        private WebCamTexture stream;
        private string activeCamera;
        private bool firstFrameReported;

        public void Configure(RawImage preview, Text status)
        {
            previewImage = preview;
            statusText = status;
        }

        private void Start()
        {
            if (previewImage == null)
            {
                SetStatus("Camera preview UI is not configured.");
                Debug.LogError("MagicMirror: missing RawImage.");
                return;
            }

            var devices = WebCamTexture.devices;
            if (devices.Length == 0)
            {
                SetStatus("No camera detected. Check device and camera permissions.");
                return;
            }

            activeCamera = devices[0].name;
            stream = new WebCamTexture(activeCamera, requestedWidth, requestedHeight, requestedFrameRate);
            previewImage.texture = stream;

            // Mirror horizontally to behave like a conventional mirror.
            previewImage.uvRect = new Rect(1f, 0f, -1f, 1f);
            SetStatus("Starting camera: " + activeCamera + " ...");
            try
            {
                stream.Play();
            }
            catch (System.Exception exception)
            {
                SetStatus("Could not start webcam: " + exception.Message);
                Debug.LogException(exception);
                StopCamera();
            }
        }

        private void Update()
        {
            if (stream == null || !stream.isPlaying || !stream.didUpdateThisFrame)
                return;

            // Unity can initially report a placeholder size before real frames arrive.
            if (!firstFrameReported && stream.width > 16 && stream.height > 16)
            {
                firstFrameReported = true;
                var fitter = previewImage.GetComponent<AspectRatioFitter>();
                if (fitter != null)
                    fitter.aspectRatio = (float)stream.width / stream.height;

                SetStatus("Camera: " + activeCamera + "  |  " + stream.width + " x " + stream.height);
                Debug.Log("MagicMirror Phase 0: webcam produced a frame.");
            }
        }

        private void OnDisable()
        {
            StopCamera();
        }

        private void OnDestroy()
        {
            StopCamera();
        }

        private void StopCamera()
        {
            if (stream == null)
                return;
            if (stream.isPlaying)
                stream.Stop();
            if (previewImage != null)
                previewImage.texture = null;
            Destroy(stream);
            stream = null;
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }
    }
}
