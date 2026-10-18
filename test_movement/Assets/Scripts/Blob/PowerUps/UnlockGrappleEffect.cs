using UnityEngine;

namespace BlobGame.Player.PowerUps
{
    [CreateAssetMenu(menuName = "Blob/Power Ups/Effects/Unlock Grapple", fileName = "UnlockGrapple")]
    public sealed class UnlockGrappleEffect : BlobPowerUpEffect
    {
        public override void Apply(BlobController player)
        {
            if (player == null)
                return;

            Grapple2D grapple = player.GetComponent<Grapple2D>();
            if (grapple == null)
                grapple = player.gameObject.AddComponent<Grapple2D>();

            grapple.enabled = true;
        }
    }
}
