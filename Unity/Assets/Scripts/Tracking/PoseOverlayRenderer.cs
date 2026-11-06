using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MagicMirror.Tracking
{
    /// <summary>
    /// Displays MediaPipe 33-landmark debug skeleton in the same UI space as
    /// the mirrored image. Does not drive anatomical bones yet.
    /// </summary>
    public sealed class PoseOverlayRenderer : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float confidenceThreshold = 0.35f;
        [SerializeField, Range(0.01f, 1f)] private float smoothing = 0.55f;
        [SerializeField] private Color jointColor = new Color(0.10f, 0.92f, 0.85f, 1f);
        [SerializeField] private Color edgeColor = new Color(0.25f, 0.80f, 1f, 0.7f);
        [SerializeField] private float jointDiameter = 10f;
        [SerializeField] private float lineThickness = 4f;

        // MediaPipe Pose indices; face-only detailed landmark connections are
        // intentionally omitted to keep the body/skeletal debug legible.
        private static readonly int[] EdgePairs = {
            11,12, 11,13, 13,15, 12,14, 14,16,
            11,23, 12,24, 23,24, 23,25, 25,27,
            24,26, 26,28, 27,31, 28,32,
            15,17, 15,19, 15,21, 16,18, 16,20, 16,22
        };

        private RectTransform canvasRect;
        private readonly List<RectTransform> joints = new List<RectTransform>();
        private readonly List<RectTransform> edges = new List<RectTransform>();
        private readonly Vector2[] positions = new Vector2[33];
        private readonly bool[] wasVisible = new bool[33];
        private readonly bool[] visible = new bool[33];

        public int VisibleLandmarkCount { get; private set; }

        private void Awake()
        {
            canvasRect = GetComponent<RectTransform>();
            // Lines are behind points, and the overlay does not intercept UI.
            for (int i = 0; i < EdgePairs.Length / 2; i++)
                edges.Add(MakeMarker("Bone " + i, edgeColor, lineThickness));

            for (int i = 0; i < 33; i++)
                joints.Add(MakeMarker("Landmark " + i, jointColor, jointDiameter));
        }

        private RectTransform MakeMarker(string name, Color color, float size)
        {
            var node = new GameObject(name, typeof(RectTransform), typeof(Image));
            node.transform.SetParent(transform, false);
            var image = node.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            var rect = node.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            rect.gameObject.SetActive(false);
            return rect;
        }

        public void Render(PoseFrame frame, bool mirrorX)
        {
            if (canvasRect == null) canvasRect = GetComponent<RectTransform>();
            var bounds = canvasRect.rect;
            VisibleLandmarkCount = 0;
            for (int i = 0; i < joints.Count; i++)
            {
                PoseLandmark p;
                bool good = frame.TryGetVisible(i, confidenceThreshold, out p) &&
                            p.x >= -0.1f && p.x <= 1.1f &&
                            p.y >= -0.1f && p.y <= 1.1f;
                visible[i] = good;
                joints[i].gameObject.SetActive(good);
                if (!good)
                {
                    wasVisible[i] = false;
                    continue;
                }

                Vector2 target = new Vector2(
                    ((mirrorX ? 1f - p.x : p.x) - 0.5f) * bounds.width,
                    (0.5f - p.y) * bounds.height
                );
                positions[i] = wasVisible[i]
                    ? Vector2.Lerp(positions[i], target, smoothing)
                    : target;
                joints[i].anchoredPosition = positions[i];
                wasVisible[i] = true;
                VisibleLandmarkCount++;
            }

            for (int i = 0; i < edges.Count; i++)
            {
                int a = EdgePairs[2 * i], b = EdgePairs[2 * i + 1];
                bool show = visible[a] && visible[b];
                var segment = edges[i];
                segment.gameObject.SetActive(show);
                if (!show) continue;
                Vector2 delta = positions[b] - positions[a];
                segment.anchoredPosition = (positions[a] + positions[b]) * 0.5f;
                segment.sizeDelta = new Vector2(delta.magnitude, lineThickness);
                segment.localRotation = Quaternion.Euler(0f, 0f,
                    Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            }
        }
    }
}
