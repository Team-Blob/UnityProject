using UnityEngine;

namespace BlobGame.Player
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class BlobSensors : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float groundCheckDepth = 0.12f;
        [SerializeField, Range(0.1f, 1f)] private float groundCheckWidth = 0.75f;
        [SerializeField] private LayerMask groundMask = ~0;

        private readonly Collider2D[] groundHits = new Collider2D[8];
        private Collider2D bodyCollider;
        private ContactFilter2D groundFilter;

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

        public void Refresh()
        {
            if (bodyCollider == null)
            {
                IsGrounded = false;
                return;
            }

            Bounds bounds = bodyCollider.bounds;
            Vector2 checkSize = new(bounds.size.x * groundCheckWidth, groundCheckDepth);
            Vector2 checkCenter = new(bounds.center.x, bounds.min.y - groundCheckDepth * 0.45f);
            int hitCount = Physics2D.OverlapBox(checkCenter, checkSize, 0f, groundFilter, groundHits);

            IsGrounded = false;
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = groundHits[i];
                if (hit != null && hit != bodyCollider && hit.transform.root != transform.root)
                {
                    IsGrounded = true;
                    break;
                }
            }
        }

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
