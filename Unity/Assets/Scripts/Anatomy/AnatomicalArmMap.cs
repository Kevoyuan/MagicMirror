using System;
using MagicMirror.Motion;
using UnityEngine;

namespace MagicMirror.Anatomy
{
    /// <summary>
    /// User-authored correspondence from SIX verified anatomical bones
    /// to isolated mesh prefabs and joint-anchor coordinates.
    ///
    /// This is not auto-detected: human anatomical identity, left/right,
    /// local pivot/endpoints and asset licensing need external validation.
    /// </summary>
    [CreateAssetMenu(fileName = "AnatomicalArmMap",
        menuName = "MagicMirror/Anatomy/Verified Arm Model Map")]
    public sealed class AnatomicalArmMap : ScriptableObject
    {
        public enum BoneType { Humerus, Radius, Ulna }

        [Serializable]
        public sealed class BoneAsset
        {
            [Tooltip("Single verified anatomical bone root prefab, not an entire skeleton.")]
            public GameObject sourcePrefab;
            [Tooltip("Proximal anatomical anchor in sourcePrefab ROOT LOCAL coordinates.")]
            public Vector3 proximalLocal;
            [Tooltip("Distal anatomical anchor in sourcePrefab ROOT LOCAL coordinates.")]
            public Vector3 distalLocal;
            [Tooltip("Confirm bone identity, side and anchors have been inspected.")]
            public bool verified;
        }

        [Header("Source and license provenance")]
        [Tooltip("Exact source URL of the anatomical asset collection.")]
        public string sourceUrl =
            "https://github.com/LluisV/Z-Anatomy/tree/PC-Version/Resources/Models/FBX";
        [Tooltip("Exact revision/hash/date of the source files. Required.")]
        public string sourceRevision;
        [Tooltip("Credit and source license text for the specific imported meshes.")]
        public string attributionAndLicense;
        [Tooltip("Must separately verify asset-specific restrictions and attribution.")]
        public bool licensesReviewed;

        [Header("Subject's LEFT bones")]
        public BoneAsset leftHumerus = new BoneAsset();
        public BoneAsset leftRadius = new BoneAsset();
        public BoneAsset leftUlna = new BoneAsset();

        [Header("Subject's RIGHT bones")]
        public BoneAsset rightHumerus = new BoneAsset();
        public BoneAsset rightRadius = new BoneAsset();
        public BoneAsset rightUlna = new BoneAsset();

        public BoneAsset Get(ArmSide side, BoneType bone)
        {
            if (side == ArmSide.Left)
            {
                switch (bone)
                {
                    case BoneType.Humerus: return leftHumerus;
                    case BoneType.Radius: return leftRadius;
                    default: return leftUlna;
                }
            }

            switch (bone)
            {
                case BoneType.Humerus: return rightHumerus;
                case BoneType.Radius: return rightRadius;
                default: return rightUlna;
            }
        }

        public bool Validate(out string problem)
        {
            if (string.IsNullOrWhiteSpace(sourceUrl) ||
                string.IsNullOrWhiteSpace(sourceRevision) ||
                string.IsNullOrWhiteSpace(attributionAndLicense) || !licensesReviewed)
            {
                problem = "Missing reviewed source URL, revision, attribution or asset-specific license confirmation.";
                return false;
            }

            foreach (ArmSide side in Enum.GetValues(typeof(ArmSide)))
            {
                foreach (BoneType bone in Enum.GetValues(typeof(BoneType)))
                {
                    BoneAsset item = Get(side, bone);
                    string label = side + " " + bone;
                    if (item == null || item.sourcePrefab == null || !item.verified)
                    {
                        problem = label + ": missing prefab or identity/anchor verification.";
                        return false;
                    }

                    if ((item.proximalLocal - item.distalLocal).sqrMagnitude < 0.000001f)
                    {
                        problem = label + ": local proximal/distal anchors are identical.";
                        return false;
                    }

                    if ((item.sourcePrefab.transform.localScale - Vector3.one).sqrMagnitude > 0.0001f)
                    {
                        problem = label + ": prefab ROOT scale must be baked to (1,1,1).";
                        return false;
                    }

                    if (item.sourcePrefab.GetComponentsInChildren<Renderer>(true).Length == 0)
                    {
                        problem = label + ": no renderable mesh found inside source prefab.";
                        return false;
                    }
                }
            }

            problem = null;
            return true;
        }
    }
}
