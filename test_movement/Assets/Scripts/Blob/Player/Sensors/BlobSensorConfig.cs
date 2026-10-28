using UnityEngine;

namespace BlobGame.Player
{
    [CreateAssetMenu(menuName = "Blob/Player/Sensor Config", fileName = "BlobSensorConfig")]
    public sealed class BlobSensorConfig : ScriptableObject
    {
        [SerializeField, Min(0.01f)] private float groundCheckDepth = 0.12f;
        [SerializeField, Range(0.1f, 1f)] private float groundCheckWidth = 0.75f;
        [SerializeField] private LayerMask groundMask = ~0;
        public float GroundCheckDepth => groundCheckDepth;
        public float GroundCheckWidth => groundCheckWidth;
        public LayerMask GroundMask => groundMask;
    }
}
