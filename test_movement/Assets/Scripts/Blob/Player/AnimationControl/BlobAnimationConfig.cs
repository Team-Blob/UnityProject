using UnityEngine;

namespace BlobGame.Player
{
    [CreateAssetMenu(menuName = "Blob/Player/Animation/New Animation Config", fileName = "BlobAnimationConfig")]
    public sealed class BlobAnimationConfig : ScriptableObject
    {
        [SerializeField, Min(1f)] private float sustainedSprintAnimationSpeed = 1.65f;
        public float SustainedSprintAnimationSpeed => sustainedSprintAnimationSpeed;
    }
}
