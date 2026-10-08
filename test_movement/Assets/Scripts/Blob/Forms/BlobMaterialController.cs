using BlobGame.Player.Animation;
using UnityEngine;

namespace BlobGame.Player.Forms
{
    /// <summary>
    /// Owns the current Blob material and applies its three concerns:
    /// physics traits, animation/visual presentation, and capability flags.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BlobController), typeof(BlobAnimationDriver))]
    public sealed class BlobMaterialController : MonoBehaviour
    {
        [SerializeField] private BlobMaterialId initialMaterial = BlobMaterialId.Default;
        [SerializeField] private BlobMaterialProfile[] profiles;

        private BlobController blob;
        private BlobAnimationDriver animationDriver;
        private bool initialized;

        public BlobMaterialProfile CurrentProfile { get; private set; }
        public BlobMaterialId CurrentMaterial => CurrentProfile != null
            ? CurrentProfile.MaterialId
            : initialMaterial;
        public bool IsTransforming { get; private set; }

        private void Awake()
        {
            blob = GetComponent<BlobController>();
            animationDriver = GetComponent<BlobAnimationDriver>();
        }

        public void Initialize()
        {
            if (initialized)
                return;

            initialized = true;
            if (TryGetProfile(initialMaterial, out BlobMaterialProfile profile))
                ApplyProfile(profile);
            else
                Debug.LogError($"No Blob material profile is configured for '{initialMaterial}'.", this);
        }

        public bool HasAbility(BlobAbility ability)
        {
            return CurrentProfile != null && CurrentProfile.HasAbility(ability);
        }

        public bool RequestTransform(BlobMaterialId targetMaterial)
        {
            Initialize();
            if (IsTransforming || CurrentMaterial == targetMaterial)
                return false;

            if (!TryGetProfile(targetMaterial, out BlobMaterialProfile targetProfile))
            {
                Debug.LogWarning($"No Blob material profile is configured for '{targetMaterial}'.", this);
                return false;
            }

            return blob.RequestMaterialTransform(targetProfile);
        }

        /// <summary>
        /// Requests the next distinct, non-null profile in the configured profile list.
        /// Intended for prototype input and debugging rather than final game rules.
        /// </summary>
        public bool RequestNextMaterial()
        {
            Initialize();
            if (IsTransforming || profiles == null || profiles.Length < 2)
                return false;

            int currentIndex = -1;
            for (int i = 0; i < profiles.Length; i++)
            {
                if (profiles[i] == CurrentProfile)
                {
                    currentIndex = i;
                    break;
                }
            }

            for (int offset = 1; offset <= profiles.Length; offset++)
            {
                int index = (currentIndex + offset + profiles.Length) % profiles.Length;
                BlobMaterialProfile candidate = profiles[index];
                if (candidate != null && candidate.MaterialId != CurrentMaterial)
                    return RequestTransform(candidate.MaterialId);
            }

            return false;
        }

        [ContextMenu("Transform To Next Material (Play Mode)")]
        private void TransformToNextMaterialForDebug()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Blob material transformation can only be tested in Play Mode.", this);
                return;
            }

            RequestNextMaterial();
        }

        internal void ApplyProfile(BlobMaterialProfile profile)
        {
            if (profile == null)
                return;

            CurrentProfile = profile;
            blob.ApplyMaterialTraits(profile);
            animationDriver.ApplyProfile(profile);
        }

        internal void SetTransforming(bool value)
        {
            IsTransforming = value;
        }

        private bool TryGetProfile(BlobMaterialId materialId, out BlobMaterialProfile profile)
        {
            if (profiles != null)
            {
                foreach (BlobMaterialProfile candidate in profiles)
                {
                    if (candidate != null && candidate.MaterialId == materialId)
                    {
                        profile = candidate;
                        return true;
                    }
                }
            }

            profile = null;
            return false;
        }
    }
}
