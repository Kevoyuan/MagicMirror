using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace MagicMirror.Tracking
{
    /// <summary>
    /// Reads localhost-only MediaPipe sidecar packets. The sidecar owns the
    /// camera and sends both JPEG and pose from the SAME captured frame.
    /// Network I/O is on a worker thread. Unity texture/UI access is only on
    /// the main thread. Only the latest packet is retained (no backlog).
    /// </summary>
    public sealed class PoseStreamReceiver : MonoBehaviour
    {
        private const int MaxHeaderBytes = 131072;
        private const int MaxJpegBytes = 2097152;

        [SerializeField] private RawImage previewImage;
        [SerializeField] private Text statusText;
        [SerializeField] private PoseOverlayRenderer poseOverlay;
        [SerializeField] private int port = 8765;

        private readonly object gate = new object();
        private Thread worker;
        private volatile bool running;
        private TcpClient activeClient;
        private FrameBytes pendingFrame;
        private string pendingStatus;
        private Texture2D previewTexture;

        private sealed class FrameBytes
        {
            public string header;
            public byte[] jpeg;
        }

        public event Action<PoseFrame> FrameReceived;

        public void Configure(RawImage preview, Text status, PoseOverlayRenderer overlay)
        {
            previewImage = preview;
            statusText = status;
            poseOverlay = overlay;
        }

        private void Start()
        {
            if (previewImage == null || poseOverlay == null)
            {
                Debug.LogError("MagicMirror: PoseStreamReceiver requires preview and overlay references.");
                return;
            }

            // A new project scene uses this receiver INSTEAD OF the Phase 0
            // WebCamTexture controller, avoiding double-opening a USB camera.
            previewImage.color = Color.black;
            previewImage.uvRect = new Rect(1f, 0f, -1f, 1f);
            running = true;
            pendingStatus = "Waiting for Pose Sidecar on 127.0.0.1:" + port + " ...";
            worker = new Thread(ReceiveLoop) { IsBackground = true, Name = "MagicMirrorPoseReader" };
            worker.Start();
        }

        private void ReceiveLoop()
        {
            while (running)
            {
                var client = new TcpClient();
                try
                {
                    lock (gate) activeClient = client;
                    client.NoDelay = true;
                    client.Connect(IPAddress.Loopback, port);
                    SetStatusFromWorker("Connected to Pose Sidecar.");

                    using (NetworkStream stream = client.GetStream())
                    {
                        while (running)
                        {
                            int headerBytes = ReadLength(stream, MaxHeaderBytes);
                            string header = Encoding.UTF8.GetString(ReadExact(stream, headerBytes));
                            int jpegBytes = ReadLength(stream, MaxJpegBytes);
                            byte[] jpeg = ReadExact(stream, jpegBytes);
                            lock (gate) pendingFrame = new FrameBytes { header = header, jpeg = jpeg };
                        }
                    }
                }
                catch (Exception exception) when (
                    exception is SocketException ||
                    exception is IOException ||
                    exception is ObjectDisposedException ||
                    exception is InvalidDataException)
                {
                    if (running)
                    {
                        SetStatusFromWorker("Waiting for sidecar: " + exception.Message);
                        lock (gate) pendingFrame = null;
                    }
                }
                finally
                {
                    lock (gate)
                    {
                        if (ReferenceEquals(activeClient, client))
                            activeClient = null;
                    }
                    client.Close();
                }

                if (running) Thread.Sleep(500);
            }
        }

        private static int ReadLength(Stream stream, int max)
        {
            byte[] size = ReadExact(stream, 4);
            uint value = ((uint)size[0] << 24) | ((uint)size[1] << 16) |
                         ((uint)size[2] << 8) | size[3];
            if (value > max || (max == MaxHeaderBytes && value == 0))
                throw new InvalidDataException("Invalid frame length: " + value);
            return (int)value;
        }

        private static byte[] ReadExact(Stream stream, int count)
        {
            byte[] result = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = stream.Read(result, offset, count - offset);
                if (read <= 0) throw new EndOfStreamException("Sidecar disconnected.");
                offset += read;
            }
            return result;
        }

        private void SetStatusFromWorker(string message)
        {
            lock (gate) pendingStatus = message;
        }

        private void Update()
        {
            FrameBytes current;
            string message;
            lock (gate)
            {
                current = pendingFrame;
                pendingFrame = null;
                message = pendingStatus;
                pendingStatus = null;
            }

            if (!string.IsNullOrEmpty(message) && statusText != null)
                statusText.text = message;

            if (current == null) return;

            PoseFrame frame;
            try
            {
                frame = JsonUtility.FromJson<PoseFrame>(current.header);
            }
            catch (Exception error)
            {
                Debug.LogWarning("MagicMirror rejected malformed pose JSON: " + error.Message);
                return;
            }

            if (frame == null || !frame.IsValid)
            {
                Debug.LogWarning("MagicMirror rejected invalid or mismatched pose packet.");
                return;
            }

            if (current.jpeg.Length > 0)
            {
                if (previewTexture == null)
                    previewTexture = new Texture2D(2, 2, TextureFormat.RGB24, false);
                if (!previewTexture.LoadImage(current.jpeg, false))
                    return;

                previewImage.texture = previewTexture;
                previewImage.color = Color.white;
            }

            // Source image and poses are always transformed together.
            bool horizontalMirror = !frame.mirrored;
            previewImage.uvRect = horizontalMirror
                ? new Rect(1f, 0f, -1f, 1f)
                : new Rect(0f, 0f, 1f, 1f);

            var fitter = previewImage.GetComponent<AspectRatioFitter>();
            if (fitter != null) fitter.aspectRatio = (float)frame.width / frame.height;

            poseOverlay.Render(frame, horizontalMirror);
            if (statusText != null)
                statusText.text = (frame.demo ? "DEMO" : "MEDIAPIPE") +
                                  " | frame " + frame.frameId +
                                  " | visible points " +
                                  poseOverlay.VisibleLandmarkCount;

            FrameReceived?.Invoke(frame);
        }

        private void OnDisable() { Stop(); }
        private void OnDestroy() { Stop(); }

        private void Stop()
        {
            running = false;
            lock (gate)
            {
                if (activeClient != null)
                    activeClient.Close();
                activeClient = null;
                pendingFrame = null;
            }

            if (worker != null && worker.IsAlive)
                worker.Join(1000);
            worker = null;
            if (previewImage != null)
            {
                previewImage.texture = null;
                previewImage.color = Color.black;
            }
            if (previewTexture != null)
                Destroy(previewTexture);
            previewTexture = null;
        }
    }
}
