using BlobGame.Player.Forms;
using UnityEngine;

namespace BlobGame.Player.Animation
{
    /// <summary>
    /// Translates form-independent animation requests into Animator states.
    /// The active material profile supplies an AnimatorOverrideController.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public sealed class BlobAnimationDriver : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float defaultCrossFadeDuration = 0.08f;

        private Animator animator;
        private SpriteRenderer spriteRenderer;
        private BlobAnimationId? currentAnimation;

        public BlobAnimationId? CurrentAnimation => currentAnimation;

        private void Awake()
        {
            CacheComponents();
        }

        public void ApplyProfile(BlobMaterialProfile profile)
        {
            if (profile == null)
                return;

            CacheComponents();
            animator.runtimeAnimatorController = profile.AnimationController;
            Color tint = profile.SpriteTint;
            tint.a = spriteRenderer.color.a;
            spriteRenderer.color = tint;
            currentAnimation = null;
        }

        public void Play(BlobAnimationId animationId, float crossFadeDuration = -1f)
        {
            CacheComponents();
            if (animator.runtimeAnimatorController == null || currentAnimation == animationId)
                return;

            int stateHash = Animator.StringToHash($"Base Layer.{animationId}");
            if (!animator.HasState(0, stateHash))
            {
                Debug.LogWarning($"Animator does not contain the Blob animation state '{animationId}'.", this);
                return;
            }

            float duration = crossFadeDuration >= 0f ? crossFadeDuration : defaultCrossFadeDuration;
            if (duration <= 0f)
                animator.Play(stateHash, 0, 0f);
            else
                animator.CrossFade(stateHash, duration, 0, 0f);

            currentAnimation = animationId;
        }

        private void CacheComponents()
        {
            if (animator == null)
                animator = GetComponent<Animator>();
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }
}
