using UnityEngine;

namespace BlobGame.Player
{
    /// <summary>
    /// Shared, designer-authored locomotion tuning. Keep this asset immutable at
    /// runtime; per-player upgrades belong on BlobController's runtime state.
    /// </summary>
    [CreateAssetMenu(menuName = "Blob/Player/Movement Config", fileName = "BlobMovementConfig")]
    public sealed class BlobMovementConfig : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 6f;
        [SerializeField, Range(0f, 1f)] private float airControl = 0.8f;
        [SerializeField, Min(0f)] private float inputDeadZone = 0.05f;

        [Header("Sprint")]
        [SerializeField, Min(0f)] private float sprintBurstSpeed = 10f;
        [SerializeField, Min(0f)] private float sprintBurstDuration = 0.18f;
        [SerializeField, Min(0f)] private float sustainedSprintSpeed = 8f;

        [Header("Jump")]
        [SerializeField, Min(0f)] private float currentWeight = 3f;
        [SerializeField] private AnimationCurve jumpSpeedByWeight = new(
            new Keyframe(1f, 15f), new Keyframe(2f, 14f), new Keyframe(3f, 12.5f),
            new Keyframe(4f, 11f), new Keyframe(5f, 9.5f));
        [SerializeField, Min(0f)] private float riseGravityScale = 3f;
        [SerializeField, Min(0f)] private float releasedRiseGravityScale = 5f;
        [SerializeField, Min(0f)] private float fallGravityScale = 7f;
        [SerializeField, Range(0f, 1f)] private float jumpCutMultiplier = 0.6f;
        [SerializeField, Min(0f)] private float maxFallSpeed = 20f;

        [Header("Abilities")]
        [SerializeField] private bool allowsGrapple;

        public float MoveSpeed => moveSpeed;
        public float AirControl => airControl;
        public float InputDeadZone => inputDeadZone;
        public float SprintBurstSpeed => sprintBurstSpeed;
        public float SprintBurstDuration => sprintBurstDuration;
        public float SustainedSprintSpeed => sustainedSprintSpeed;
        public float CurrentWeight => currentWeight;
        public float RiseGravityScale => riseGravityScale;
        public float ReleasedRiseGravityScale => releasedRiseGravityScale;
        public float FallGravityScale => fallGravityScale;
        public float JumpCutMultiplier => jumpCutMultiplier;
        public float MaxFallSpeed => maxFallSpeed;
        public bool AllowsGrapple => allowsGrapple;
        

        public float EvaluateJumpSpeed(float weight) => jumpSpeedByWeight.Evaluate(weight);
    }
}
