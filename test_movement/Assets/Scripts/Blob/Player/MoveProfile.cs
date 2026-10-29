using System.Collections.Generic;
using UnityEngine;

namespace BlobGame.Player
{
    /// <summary>Ordered list of movement profiles available for player cycling.</summary>
    [CreateAssetMenu(menuName = "Blob/Player/New Ability List", fileName = "MovementProfiles")]
    public sealed class BlobMovementProfileSet : ScriptableObject
    {
        [SerializeField] private List<BlobMovementConfig> profiles = new();
        public IReadOnlyList<BlobMovementConfig> Profiles => profiles;
    }
}
