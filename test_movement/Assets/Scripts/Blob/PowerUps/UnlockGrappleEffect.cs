using UnityEngine;

namespace BlobGame.Player.PowerUps
{
    [CreateAssetMenu(menuName = "Blob/Power Ups/Effects/Unlock Grapple", fileName = "UnlockGrapple")]
    public sealed class UnlockGrappleEffect : BlobPowerUpEffect
    {
        [SerializeField] private GrappleConfig grappleConfig;
        [SerializeField] private BlobMovementConfig unlockMovementProfile;

        public override void Apply(BlobController player)
        {
            if (player == null)
                return;

            if (unlockMovementProfile != null)
                player.SetMovementConfig(unlockMovementProfile);

            Grapple2D grapple = player.GetComponent<Grapple2D>();
            if (grapple == null)
                grapple = player.gameObject.AddComponent<Grapple2D>();

            grapple.Unlock(grappleConfig);
        }
    }
}
