using UnityEngine;

namespace BlobGame.Player
{
    [CreateAssetMenu(menuName = "Blob/Player/Grapple Config", fileName = "GrappleConfig")]
    public sealed class GrappleConfig : ScriptableObject
    {
        [SerializeField, Min(0.1f)] private float maxDistance = 12f;
        [SerializeField] private LayerMask grappleMask = ~0;
        [SerializeField, Min(0f)] private float swingAcceleration = 14f;
        [SerializeField] private Texture2D aimMarkerTexture;
        [SerializeField, Min(1f)] private float aimMarkerSize = 24f;
        [SerializeField, Min(0f)] private Material grappleMaterial = null;
        
        public float MaxDistance => maxDistance;
        public LayerMask GrappleMask => grappleMask;
        public float SwingAcceleration => swingAcceleration;
        public Texture2D AimMarkerTexture => aimMarkerTexture;
        public float AimMarkerSize => aimMarkerSize;
        public Material GrappleMaterial => grappleMaterial;
    }
}
