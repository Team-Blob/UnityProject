using UnityEngine;

namespace BlobGame.Player.Forms
{
    /// <summary>
    /// Data-driven definition of one Blob material form.
    /// A form affects physics traits, presentation, and available abilities.
    /// </summary>
    [CreateAssetMenu(fileName = "BlobMaterial", menuName = "Blob/Material Profile")]
    public sealed class BlobMaterialProfile : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private BlobMaterialId materialId;

        [Header("Physics Traits")]
        [SerializeField, Min(0f)] private float weight = 3f;
        [SerializeField, Min(0f)] private float moveSpeedMultiplier = 1f;
        [SerializeField, Min(0f)] private float airControlMultiplier = 1f;
        [SerializeField, Min(0f)] private float jumpSpeedMultiplier = 1f;
        [SerializeField, Min(0f)] private float gravityMultiplier = 1f;

        [Header("Presentation")]
        [SerializeField] private AnimatorOverrideController animationController;
        [SerializeField] private Color spriteTint = Color.black;
        [SerializeField, Min(0f)] private float transformFadeOutDuration = 0.25f;
        [SerializeField, Min(0f)] private float transformFadeInDuration = 0.25f;

        [Header("Capabilities")]
        [SerializeField] private BlobAbility abilities;

        public BlobMaterialId MaterialId => materialId;
        public float Weight => weight;
        public float MoveSpeedMultiplier => moveSpeedMultiplier;
        public float AirControlMultiplier => airControlMultiplier;
        public float JumpSpeedMultiplier => jumpSpeedMultiplier;
        public float GravityMultiplier => gravityMultiplier;
        public AnimatorOverrideController AnimationController => animationController;
        public Color SpriteTint => spriteTint;
        public float TransformFadeOutDuration => transformFadeOutDuration;
        public float TransformFadeInDuration => transformFadeInDuration;
        public BlobAbility Abilities => abilities;

        //maybe just use bool not function
        public bool HasAbility(BlobAbility ability)
        {
            return ability != BlobAbility.None && (abilities & ability) == ability;
        }
    }
}
