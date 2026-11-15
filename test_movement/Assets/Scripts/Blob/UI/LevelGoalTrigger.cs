using BlobGame.Player;
using UnityEngine;

namespace BlobGame.UI
{
    /// <summary>Shows completion once when the player reaches this smiley marker.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class LevelGoalTrigger : MonoBehaviour
    {
        [SerializeField] private UIController uiController;

        private bool consumed;

        private void Reset()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (consumed || uiController == null)
                return;

            // A player collider may be on a child of the Rigidbody2D or controller.
            BlobController player = other.attachedRigidbody != null
                ? other.attachedRigidbody.GetComponentInParent<BlobController>()
                : other.GetComponentInParent<BlobController>();

            if (player == null)
                return;

            // Each marker has its own latch; reloading the scene resets both goals.
            consumed = true;
            uiController.ShowLevelComplete();
        }
    }
}
