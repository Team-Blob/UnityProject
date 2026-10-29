using System.Collections.Generic;
using UnityEngine;

namespace BlobGame.Player.PowerUps
{
    /// <summary>Reusable asset describing a collectible player upgrade.</summary>
    [CreateAssetMenu(menuName = "Abilities/Define Ability", fileName = "Definition")]
    public sealed class BlobPowerUpDefinition : ScriptableObject
    {
        [Tooltip("Stable unique key used to prevent collecting this upgrade more than once.")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [TextArea, SerializeField] private string description;
        [SerializeField] private List<BlobPowerUpEffect> effects = new();
        

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public IReadOnlyList<BlobPowerUpEffect> Effects => effects;
    }
}
