using UnityEngine;

namespace BlobGame.Player
{
    /// <summary>
    /// Presentation assets associated with one movement/material profile.
    /// The controller must preserve the common Blob animation state names.
    /// </summary>
    [CreateAssetMenu(menuName = "Blob/Player/Animation Profile", fileName = "BlobAnimationProfile")]
    public sealed class BlobAnimationProfile : ScriptableObject
    {
        [SerializeField] private RuntimeAnimatorController animatorController;

        public RuntimeAnimatorController AnimatorController => animatorController;
    }
}
