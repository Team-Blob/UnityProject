using UnityEngine;

namespace BlobGame.Player.PowerUps
{
    /// <summary>Asset effect for permanent movement and jump stat upgrades.</summary>
    [CreateAssetMenu(menuName = "Blob/Power Ups/Effects/Modify Movement", fileName = "ModifyMovement")]
    public sealed class ModifyBlobMovementEffect : BlobPowerUpEffect
    {
        [SerializeField, Min(0f)] private float moveSpeedMultiplier = 1f;
        [SerializeField, Min(0f)] private float airControlMultiplier = 1f;
        [SerializeField, Min(0f)] private float jumpSpeedMultiplier = 1f;

        public override void Apply(BlobController player)
        {
            if (player != null)
                player.ApplyMovementUpgrade(moveSpeedMultiplier, airControlMultiplier, jumpSpeedMultiplier);
        }
    }
}
