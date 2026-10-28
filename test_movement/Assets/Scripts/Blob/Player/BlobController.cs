using BlobGame.Player.StateMachine;
using BlobGame.Player.StateMachine.States;
using BlobGame.Player.PowerUps;
using UnityEngine;

namespace BlobGame.Player
{
    /// <summary>
    /// Coordinates player input, environment sensors, locomotion states,
    /// and Rigidbody2D movement operations.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    [RequireComponent(typeof(BlobInputReader), typeof(BlobSensors), typeof(BlobPowerUpManager))]
    public sealed class BlobController : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Optional shared movement tuning. When empty, the legacy values below are used.")]
        [SerializeField] private BlobMovementConfig movementConfig;
        [SerializeField] private BlobMovementProfileSet movementProfileSet;

       /* [Header("Movement")]
        [Tooltip("Horizontal movement speed while grounded.")]
        [SerializeField, Min(0f)] private float moveSpeed = 6f;
        [Tooltip("Fraction of grounded movement speed available while airborne.")]
        [SerializeField, Range(0f, 1f)] private float airControl = 0.8f;
        [Tooltip("Horizontal input values below this threshold are treated as zero.")]
        [SerializeField, Min(0f)] private float inputDeadZone = 0.05f;

        [Header("Sprint")]
        [Tooltip("Horizontal speed during the initial sprint burst.")]
        [SerializeField, Min(0f)] private float sprintBurstSpeed = 10f;
        [Tooltip("Duration of the initial sprint burst. Burst distance is speed multiplied by this duration.")]
        [SerializeField, Min(0f)] private float sprintBurstDuration = 0.18f;
        [Tooltip("Horizontal speed after the initial burst while sprint remains held.")]
        [SerializeField, Min(0f)] private float sustainedSprintSpeed = 8f;

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
        */

        private BlobStateMachine stateMachine;
        private Grapple2D grapple;
        private float movementSpeedMultiplier = 1f;
        private float airControlMultiplier = 1f;
        private float jumpSpeedMultiplier = 1f;
        private bool preserveGrappleMomentum;
        private int movementProfileIndex = -1;
        private bool profileSwitchRequiresRelease;
        private BlobMovementConfig pendingMovementConfig;

        public Rigidbody2D Body { get; private set; }
        public BlobInputReader Input { get; private set; }
        public BlobSensors Sensors { get; private set; }
        public BlobAnimationController Animation { get; private set; }
        public BlobPowerUpManager PowerUps { get; private set; }

        public BlobIdleState IdleState { get; private set; }
        public BlobMoveState MoveState { get; private set; }
        public BlobJumpState JumpState { get; private set; }
        public BlobFallState FallState { get; private set; }
        public BlobSprintState SprintState { get; private set; }
        public BlobTransformState TransformState { get; private set; }

        private float BaseMoveSpeed => movementConfig != null ? movementConfig.MoveSpeed : 6f;
        private float AirControl => movementConfig != null ? movementConfig.AirControl : .8f;
        private float InputDeadZone => movementConfig != null ? movementConfig.InputDeadZone : 0.05f;
        public float MoveSpeed => BaseMoveSpeed * movementSpeedMultiplier;
        public float AirMoveSpeed => MoveSpeed * AirControl * airControlMultiplier;
        public float SprintBurstSpeed => movementConfig != null ? movementConfig.SprintBurstSpeed : 20;
        public float SprintBurstDuration => movementConfig != null ? movementConfig.SprintBurstDuration : .36f;
        public float SustainedSprintSpeed => movementConfig != null ? movementConfig.SustainedSprintSpeed : 9;
        public float CurrentWeight => movementConfig != null ? movementConfig.CurrentWeight : 3;
        public float RiseGravityScale => movementConfig != null ? movementConfig.RiseGravityScale : 3;
        public float ReleasedRiseGravityScale => movementConfig != null ? movementConfig.ReleasedRiseGravityScale : 5;
        public float FallGravityScale => movementConfig != null ? movementConfig.FallGravityScale : 7;
        public bool CurrentProfileAllowsGrapple => movementConfig != null && movementConfig.AllowsGrapple;
        public BlobMovementConfig CurrentMovementConfig => movementConfig;
        internal BlobMovementConfig PendingMovementConfig => pendingMovementConfig;

        /// <summary>
        /// Requests a material/movement profile change. The request waits until
        /// Idle, then the Transform state performs the visual and gameplay swap.
        /// </summary>
        public void SetMovementConfig(BlobMovementConfig config)
        {
            if (config == null || config == movementConfig || config == pendingMovementConfig)
                return;

            pendingMovementConfig = config;
        }

        internal void ApplyPendingMovementConfig()
        {
            BlobMovementConfig config = pendingMovementConfig;
            if (config == null)
                return;

            movementConfig = config;
            movementProfileIndex = FindProfileIndex(config);
            pendingMovementConfig = null;

            if (!config.AllowsGrapple && IsGrappling)
                grapple.Release();
        }

        
        
        public bool HasMoveInput => Mathf.Abs(Input.MoveX) > InputDeadZone;
        public bool PreserveGrappleMomentum => preserveGrappleMomentum;
        public float FacingDirection { get; private set; } = 1f;
        
        public bool IsGrappling
        {
            get
            {
                if (grapple == null)
                    grapple = GetComponent<Grapple2D>();

                return grapple != null && grapple.IsAttached;
            }
        }

        /// <summary>
        /// Name of the active locomotion state, exposed for debugging.
        /// </summary>
        public string CurrentStateName => stateMachine?.CurrentState?.GetType().Name ?? "None";
        public BlobState CurrentState => stateMachine?.CurrentState;

        private void Awake()
        {
            if (movementProfileSet == null)
                movementProfileSet = Resources.Load<BlobMovementProfileSet>("MovementProfiles");
            if (movementConfig == null && movementProfileSet != null && movementProfileSet.Profiles.Count > 0)
                movementConfig = movementProfileSet.Profiles[0];
            movementProfileIndex = FindProfileIndex(movementConfig);

            Body = GetComponent<Rigidbody2D>();
            Input = GetComponent<BlobInputReader>();
            Sensors = GetComponent<BlobSensors>();
            PowerUps = GetComponent<BlobPowerUpManager>();
            Animation = GetComponent<BlobAnimationController>();

            stateMachine = new BlobStateMachine();

            // Create each state once and reuse it to avoid allocations during transitions.
            IdleState = new BlobIdleState(this);
            MoveState = new BlobMoveState(this);
            JumpState = new BlobJumpState(this);
            FallState = new BlobFallState(this);
            SprintState = new BlobSprintState(this);
            TransformState = new BlobTransformState(this);
        }

        private void Start()
        {
            Sensors.Refresh();
            stateMachine.Initialize(Sensors.IsGrounded ? IdleState : FallState);
            Animation?.Initialize(this);
        }

        private void Update()
        {
            // Evaluate frame-based input and state transitions once per rendered frame.
            Sensors.Refresh();

            if (preserveGrappleMomentum && Sensors.IsGrounded && stateMachine.CurrentState != JumpState)
                preserveGrappleMomentum = false;

            UpdateMovementProfileCycling();
            TryStartPendingTransformation();

            // Normal locomotion follows the latest movement input. Sprint updates
            // facing itself because its initial burst direction must stay locked.
            if (stateMachine.CurrentState != SprintState && HasMoveInput)
                SetFacingDirection(Input.MoveX);

            stateMachine.Tick(Time.deltaTime);
            Animation?.TickVisuals();
        }

        private void UpdateMovementProfileCycling()
        {
            if (!Input.ProfileSwitchHeld)
            {
                profileSwitchRequiresRelease = false;
                return;
            }

            if (profileSwitchRequiresRelease)
                return;

            // The test input is intentionally not queued: pressing it outside
            // Idle requires a release and a new press after returning to Idle.
            if (stateMachine.CurrentState != IdleState || pendingMovementConfig != null
                || Animation != null && Animation.IsTransforming)
            {
                if (Input.ProfileSwitchPressed)
                    profileSwitchRequiresRelease = true;
                return;
            }

            if (!Input.ProfileSwitchPressed)
                return;

            CycleMovementProfile();
            profileSwitchRequiresRelease = true;
        }

        private void TryStartPendingTransformation()
        {
            if (pendingMovementConfig == null || stateMachine.CurrentState != IdleState)
                return;

            if (Animation == null)
            {
                ApplyPendingMovementConfig();
                return;
            }

            stateMachine.ForceChangeState(TransformState);
        }

        private void CycleMovementProfile()
        {
            if (movementProfileSet == null || movementProfileSet.Profiles.Count == 0)
                return;

            int count = movementProfileSet.Profiles.Count;
            for (int attempt = 0; attempt < count; attempt++)
            {
                movementProfileIndex = (movementProfileIndex + 1 + count) % count;
                BlobMovementConfig candidate = movementProfileSet.Profiles[movementProfileIndex];
                if (candidate != null)
                {
                    SetMovementConfig(candidate);
                    return;
                }
            }
        }

        private int FindProfileIndex(BlobMovementConfig config)
        {
            if (config == null || movementProfileSet == null)
                return -1;

            for (int i = 0; i < movementProfileSet.Profiles.Count; i++)
            {
                if (movementProfileSet.Profiles[i] == config)
                    return i;
            }

            return -1;
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

        /// <summary>Starts a jump after releasing a grapple without discarding swing momentum.</summary>
        public void JumpFromGrapple()
        {
            if (stateMachine == null)
                return;

            preserveGrappleMomentum = true;
            stateMachine.ForceChangeState(JumpState);
        }

        public void PreserveMomentumAfterGrapple()
        {
            preserveGrappleMomentum = true;
        }

        public void SetFacingDirection(float horizontalDirection)
        {
            if (Mathf.Abs(horizontalDirection) > InputDeadZone)
                FacingDirection = Mathf.Sign(horizontalDirection);
        }

        /// <summary>Applies permanent run-time multipliers from collected upgrades.</summary>
        public void ApplyMovementUpgrade(float moveMultiplier, float airControlFactor, float jumpMultiplier)
        {
            movementSpeedMultiplier *= Mathf.Max(0f, moveMultiplier);
            airControlMultiplier *= Mathf.Max(0f, airControlFactor);
            jumpSpeedMultiplier *= Mathf.Max(0f, jumpMultiplier);
        }

        #endregion

        #region Jump and Gravity Operations

        public float GetCurrentJumpSpeed()
        {
            float jumpSpeed = movementConfig != null
                ? movementConfig.EvaluateJumpSpeed(CurrentWeight)
                : 12.5f;
            return Mathf.Max(0f, jumpSpeed * jumpSpeedMultiplier);
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
            {
                float cutMultiplier = movementConfig != null ? movementConfig.JumpCutMultiplier : 0.6f;
                SetVerticalVelocity(verticalSpeed * cutMultiplier);
            }
        }

        public void SetGravity(float gravityScale)
        {
            Body.gravityScale = gravityScale;
        }

        public void ClampFallSpeed()
        {
            float fallSpeed = movementConfig != null ? movementConfig.MaxFallSpeed : 20f;
            if (Body.linearVelocity.y < -fallSpeed)
                SetVerticalVelocity(-fallSpeed);
        }

        #endregion
    }
}
