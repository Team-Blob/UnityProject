using UnityEngine;

namespace BlobGame.Player.PowerUps
{
    /// <summary>Switches one player's active movement profile to a shared config asset.</summary>
    [CreateAssetMenu(menuName = "Blob/Abilities/Change Movement Config", fileName = "ChangeMovementConfig")]
    public sealed class ChangeBlobMovementConfigEffect : BlobPowerUpEffect
    {
        [SerializeField] private BlobMovementConfig movementConfig;

        public override void Apply(BlobController player)
        {
            if (player != null && movementConfig != null)
                player.SetMovementConfig(movementConfig);
        }
    }
}
