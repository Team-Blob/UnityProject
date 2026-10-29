using UnityEngine;

namespace BlobGame.Player.PowerUps
{
    /// <summary>Generic trigger pickup. Assign its reusable definition in the Inspector.</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class BlobPowerUpPickup : MonoBehaviour
    {
        [SerializeField] private BlobPowerUpDefinition definition;

        private void Reset()
        {
            Collider2D pickupCollider = GetComponent<Collider2D>();
            pickupCollider.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            BlobController player = other.GetComponentInParent<BlobController>();
            if (player == null || !player.PowerUps.Collect(definition))
                return;

            Destroy(gameObject);
        }
    }
}
