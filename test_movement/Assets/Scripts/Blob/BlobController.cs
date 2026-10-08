using BlobGame.Player.Animation;
using BlobGame.Player.Forms;
using BlobGame.Player.StateMachine;
using BlobGame.Player.StateMachine.States;
using UnityEngine;

namespace BlobGame.Player
{
    /// <summary>
    /// Coordinates player input, environment sensors, locomotion states,
    /// and Rigidbody2D movement operations.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    [RequireComponent(typeof(BlobInputReader), typeof(BlobSensors))]
    [RequireComponent(typeof(BlobAnimationDriver), typeof(BlobMaterialController))]
    public sealed class BlobController : MonoBehaviour
    {
        [Header("Movement")]
        [Tooltip("Horizontal movement speed while grounded.")]
        [SerializeField, Min(0f)] private float moveSpeed = 6f;
        [Tooltip("Fraction of grounded movement speed available while airborne.")]
        [SerializeField, Range(0f, 1f)] private float airControl = 0.8f;
        [Tooltip("Horizontal input values below this threshold are treated as zero.")]
        [SerializeField, Min(0f)] private float inputDeadZone = 0.05f;

        [Header("Jump")]
        [Tooltip("Gameplay weight used to evaluate the jump-speed curve.")]
        [SerializeField, Min(0f)] private float currentWeight = 3f;
        [Tooltip("Maps gameplay weight on the X axis to initial jump speed on the Y axis.")]
        [SerializeField] private AnimationCurve jumpSpeedByWeight = new(
            new Keyframe(1f, 15f),
            new Keyframe(2f, 14f),
            new Keyframe(3f, 12.5f),
            new Keyframe(4f, 11f),
            new Keyframe(5f, 9.5f));
        [Tooltip("Gravity used while jump is held and the player is rising. Lower values produce a softer, longer ascent.")]
        [SerializeField, Min(0f)] private float riseGravityScale = 3f;
        [Tooltip("Gravity used after jump is released during ascent. It should usually be higher than Rise Gravity Scale.")]
        [SerializeField, Min(0f)] private float releasedRiseGravityScale = 5f;
        [Tooltip("Gravity used while falling. A higher value produces a faster, more responsive descent.")]
        [SerializeField, Min(0f)] private float fallGravityScale = 7f;
        [Tooltip("Fraction of upward velocity retained when jump is released.")]
        [SerializeField, Range(0f, 1f)] private float jumpCutMultiplier = 0.6f;
        [Tooltip("Maximum downward speed, stored as a positive value.")]
        [SerializeField, Min(0f)] private float maxFallSpeed = 20f;

        private BlobStateMachine stateMachine;
        private float activeWeight;
        private float materialMoveSpeedMultiplier = 1f;
        private float materialAirControlMultiplier = 1f;
        private float materialJumpSpeedMultiplier = 1f;
        private float materialGravityMultiplier = 1f;

        public Rigidbody2D Body { get; private set; }
        public BlobInputReader Input { get; private set; }
        public BlobSensors Sensors { get; private set; }
        public BlobAnimationDriver Animation { get; private set; }
        public BlobMaterialController Materials { get; private set; }

        public BlobIdleState IdleState { get; private set; }
        public BlobMoveState MoveState { get; private set; }
        public BlobJumpState JumpState { get; private set; }
        public BlobFallState FallState { get; private set; }
        public BlobTransformState TransformState { get; private set; }

        public float MoveSpeed => moveSpeed * materialMoveSpeedMultiplier;
        public float AirMoveSpeed => MoveSpeed * airControl * materialAirControlMultiplier;
        public float CurrentWeight => activeWeight;
        public float RiseGravityScale => riseGravityScale;
        public float ReleasedRiseGravityScale => releasedRiseGravityScale;
        public float FallGravityScale => fallGravityScale;
        public bool HasMoveInput => Mathf.Abs(Input.MoveX) > inputDeadZone;

        /// <summary>
        /// Name of the active locomotion state, exposed for debugging.
        /// </summary>
        public string CurrentStateName => stateMachine?.CurrentState?.GetType().Name ?? "None";

        private void Awake()
        {
            Body = GetComponent<Rigidbody2D>();
            Input = GetComponent<BlobInputReader>();
            Sensors = GetComponent<BlobSensors>();
            Animation = GetComponent<BlobAnimationDriver>();
            Materials = GetComponent<BlobMaterialController>();
            activeWeight = currentWeight;

            stateMachine = new BlobStateMachine();

            // Create each state once and reuse it to avoid allocations during transitions.
            IdleState = new BlobIdleState(this);
            MoveState = new BlobMoveState(this);
            JumpState = new BlobJumpState(this);
            FallState = new BlobFallState(this);
            TransformState = new BlobTransformState(this);
        }

        private void Start()
        {
            Materials.Initialize();
            Sensors.Refresh();
            stateMachine.Initialize(Sensors.IsGrounded ? IdleState : FallState);
        }

        private void Update()
        {
            // Evaluate frame-based input and state transitions once per rendered frame.
            Sensors.Refresh();
            if (Input.CycleMaterialPressed)
                Materials.RequestNextMaterial();
            stateMachine.Tick(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            // Apply Rigidbody2D movement on Unity's fixed physics timestep.
            Sensors.Refresh();
            stateMachine.FixedTick(Time.fixedDeltaTime);
        }

        #region Movement Operations

        public void SetHorizontalVelocity(float speed)
        {
            Body.linearVelocity = new Vector2(speed, Body.linearVelocity.y);
        }

        public void StopHorizontalMovement()
        {
            SetHorizontalVelocity(0f);
        }

        public void ApplyAirMovement()
        {
            SetHorizontalVelocity(Input.MoveX * AirMoveSpeed);
        }

        #endregion

        #region Jump and Gravity Operations

        public float GetCurrentJumpSpeed()
        {
            return Mathf.Max(0f, jumpSpeedByWeight.Evaluate(activeWeight) * materialJumpSpeedMultiplier);
        }

        public void SetVerticalVelocity(float speed)
        {
            Body.linearVelocity = new Vector2(Body.linearVelocity.x, speed);
        }

        /// <summary>
        /// Reduces only the remaining upward velocity to produce a shorter jump.
        /// Has no effect after the player begins falling.
        /// </summary>
        public void CutJump()
        {
            float verticalSpeed = Body.linearVelocity.y;
            if (verticalSpeed > 0f)
                SetVerticalVelocity(verticalSpeed * jumpCutMultiplier);
        }

        public void SetGravity(float gravityScale)
        {
            Body.gravityScale = gravityScale * materialGravityMultiplier;
        }

        public void ClampFallSpeed()
        {
            if (Body.linearVelocity.y < -maxFallSpeed)
                SetVerticalVelocity(-maxFallSpeed);
        }

        #endregion

        #region Material Operations

        public bool RequestMaterialTransform(BlobMaterialProfile targetProfile)
        {
            if (stateMachine == null || stateMachine.CurrentState == null ||
                Materials.CurrentProfile == targetProfile ||
                !TransformState.Prepare(targetProfile))
            {
                return false;
            }

            stateMachine.ForceChangeState(TransformState);
            return stateMachine.CurrentState == TransformState;
        }

        public void ApplyMaterialTraits(BlobMaterialProfile profile)
        {
            if (profile == null)
                return;

            activeWeight = profile.Weight;
            materialMoveSpeedMultiplier = profile.MoveSpeedMultiplier;
            materialAirControlMultiplier = profile.AirControlMultiplier;
            materialJumpSpeedMultiplier = profile.JumpSpeedMultiplier;
            materialGravityMultiplier = profile.GravityMultiplier;
        }

        #endregion
    }
}
