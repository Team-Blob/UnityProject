using UnityEngine;

namespace BlobGame.Player.PowerUps
{
    /// <summary>
    /// Base asset for an upgrade effect. Effects should change player runtime
    /// state; do not store per-player state on the shared ScriptableObject.
    /// </summary>
    public abstract class BlobPowerUpEffect : ScriptableObject
    {
        public abstract void Apply(BlobController player);
    }
}
