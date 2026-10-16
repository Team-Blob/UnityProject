using UnityEngine;

namespace BlobGame.Player
{
    /// <summary>
    /// Samples the environment around the player and exposes locomotion facts,
    /// such as whether the player is currently grounded.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class BlobSensors : MonoBehaviour
    {
        [Tooltip("Thickness of the overlap box placed below the player.")]
        [SerializeField, Min(0.01f)] private float groundCheckDepth = 0.12f;
        [Tooltip("Ground-check width as a fraction of the player collider width.")]
        [SerializeField, Range(0.1f, 1f)] private float groundCheckWidth = 0.75f;
        [Tooltip("Physics layers that are allowed to count as ground.")]
        [SerializeField] private LayerMask groundMask = ~0;

        private readonly Collider2D[] groundHits = new Collider2D[8];
        private Collider2D bodyCollider;
        private ContactFilter2D groundFilter;

        /// <summary>
        /// Whether a valid non-trigger ground collider was detected below the player.
        /// </summary>
        public bool IsGrounded { get; private set; }

        private void Awake()
        {
            bodyCollider = GetComponent<Collider2D>();
            groundFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = groundMask,
                useTriggers = false
            };
        }

        /// <summary>
        /// Refreshes the cached environment results used by the locomotion state machine.
        /// </summary>
        public void Refresh()
        {
            if (bodyCollider == null)
            {
                IsGrounded = false;
                return;
            }

            Bounds bounds = bodyCollider.bounds;

            // Place a thin overlap box slightly below the collider's lower bound.
            Vector2 checkSize = new(bounds.size.x * groundCheckWidth, groundCheckDepth);
            Vector2 checkCenter = new(bounds.center.x, bounds.min.y - groundCheckDepth * 0.45f);
            int hitCount = Physics2D.OverlapBox(checkCenter, checkSize, 0f, groundFilter, groundHits);

            IsGrounded = false;
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = groundHits[i];

                // Ignore the body collider and colliders under the same root so the
                // player's own child hitboxes cannot be detected as ground.
                if (hit != null && hit != bodyCollider && hit.transform.root != transform.root)
                {
                    IsGrounded = true;
                    break;
                }
            }
        }

        /// <summary>
        /// draw the checkbox
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Collider2D target = bodyCollider != null ? bodyCollider : GetComponent<Collider2D>();
            if (target == null)
                return;

            Bounds bounds = target.bounds;
            Vector3 center = new(bounds.center.x, bounds.min.y - groundCheckDepth * 0.45f, 0f);
            Vector3 size = new(bounds.size.x * groundCheckWidth, groundCheckDepth, 0.01f);
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(center, size);
        }
    }
}
