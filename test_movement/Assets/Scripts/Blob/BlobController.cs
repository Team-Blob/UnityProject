using BlobGame.Player.StateMachine;
using BlobGame.Player.StateMachine.States;
using UnityEngine;

namespace BlobGame.Player
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    [RequireComponent(typeof(BlobInputReader), typeof(BlobSensors))]
    public sealed class BlobController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 6f;
        [SerializeField, Range(0f, 1f)] private float airControl = 0.8f;
        [SerializeField, Min(0f)] private float inputDeadZone = 0.05f;

        [Header("Jump")]
        [SerializeField, Min(0f)] private float currentWeight = 3f;
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
        [SerializeField, Range(0f, 1f)] private float jumpCutMultiplier = 0.6f;
        [SerializeField, Min(0f)] private float maxFallSpeed = 20f;

        private BlobStateMachine stateMachine;

        public Rigidbody2D Body { get; private set; }
        public BlobInputReader Input { get; private set; }
        public BlobSensors Sensors { get; private set; }

        public BlobIdleState IdleState { get; private set; }
        public BlobMoveState MoveState { get; private set; }
        public BlobJumpState JumpState { get; private set; }
        public BlobFallState FallState { get; private set; }

        public float MoveSpeed => moveSpeed;
        public float AirMoveSpeed => moveSpeed * airControl;
        public float CurrentWeight => currentWeight;
        public float RiseGravityScale => riseGravityScale;
        public float ReleasedRiseGravityScale => releasedRiseGravityScale;
        public float FallGravityScale => fallGravityScale;
        public bool HasMoveInput => Mathf.Abs(Input.MoveX) > inputDeadZone;
        public string CurrentStateName => stateMachine?.CurrentState?.GetType().Name ?? "None";

        private void Awake()
        {
            Body = GetComponent<Rigidbody2D>();
            Input = GetComponent<BlobInputReader>();
            Sensors = GetComponent<BlobSensors>();

            stateMachine = new BlobStateMachine();
            IdleState = new BlobIdleState(this);
            MoveState = new BlobMoveState(this);
            JumpState = new BlobJumpState(this);
            FallState = new BlobFallState(this);
        }

        private void Start()
        {
            Sensors.Refresh();
            stateMachine.Initialize(Sensors.IsGrounded ? IdleState : FallState);
        }

        private void Update()
        {
            // Evaluate frame-based input and state transitions once per rendered frame.
            Sensors.Refresh();
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
            return Mathf.Max(0f, jumpSpeedByWeight.Evaluate(currentWeight));
        }

        public void SetVerticalVelocity(float speed)
        {
            Body.linearVelocity = new Vector2(Body.linearVelocity.x, speed);
        }

        public void CutJump()
        {
            float verticalSpeed = Body.linearVelocity.y;
            if (verticalSpeed > 0f)
                SetVerticalVelocity(verticalSpeed * jumpCutMultiplier);
        }

        public void SetGravity(float gravityScale)
        {
            Body.gravityScale = gravityScale;
        }

        public void ClampFallSpeed()
        {
            if (Body.linearVelocity.y < -maxFallSpeed)
                SetVerticalVelocity(-maxFallSpeed);
        }

        #endregion
    }
}
