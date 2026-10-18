using System.Collections.Generic;
using UnityEngine;

namespace BlobGame.Player.PowerUps
{
    /// <summary>Owns upgrades collected by one player during the current run.</summary>
    [DisallowMultipleComponent]
    public sealed class BlobPowerUpManager : MonoBehaviour
    {
        private readonly HashSet<string> collectedIds = new();
        private BlobController player;

        private void Awake()
        {
            player = GetComponent<BlobController>();
        }

        /// <summary>
        /// Applies a definition once. Returns false when the definition is
        /// missing or has no stable ID; duplicate pickups are harmless.
        /// </summary>
        public bool Collect(BlobPowerUpDefinition definition)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.Id))
            {
                Debug.LogWarning("Cannot collect a power-up with no definition or ID.", this);
                return false;
            }

            if (!collectedIds.Add(definition.Id))
                return true;

            foreach (BlobPowerUpEffect effect in definition.Effects)
            {
                if (effect != null)
                    effect.Apply(player);
            }

            return true;
        }

        public bool HasCollected(string id)
        {
            return !string.IsNullOrWhiteSpace(id) && collectedIds.Contains(id);
        }
    }
}
