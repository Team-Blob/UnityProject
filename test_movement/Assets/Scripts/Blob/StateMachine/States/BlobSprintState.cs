using UnityEngine;

namespace BlobGame.Player.StateMachine.States
{
    /// <summary>
    /// A directional burst that can be used on the ground or in the air. Holding
    /// sprint after a grounded burst continues into horizontal sustained sprint.
    /// </summary>
    public sealed class BlobSprintState : BlobState
    {
        private enum SprintPhase
        {
            Burst,
            Sustained
        }

        private SprintPhase phase;
        private Vector2 direction;
        private float burstElapsed;

        public bool IsBurstPhase => phase == SprintPhase.Burst;

        public BlobSprintState(BlobController blob) : base(blob)
        {
        }

        public override void Enter()
        {
            Vector2 inputDirection = new(Blob.Input.MoveX, Blob.Input.MoveY);
            direction = inputDirection.sqrMagnitude > 0.01f
                ? inputDirection.normalized
                : Vector2.right * Blob.FacingDirection;

            if (Mathf.Abs(direction.x) > Mathf.Epsilon)
                Blob.SetFacingDirection(direction.x);
            phase = SprintPhase.Burst;
            burstElapsed = 0f;
        }

        public override BlobState Tick(float deltaTime)
        {
            if (phase == SprintPhase.Burst)
            {
                burstElapsed += deltaTime;
                if (burstElapsed < Blob.SprintBurstDuration)
                    return null;

                if (!Blob.Sensors.IsGrounded)
                    return Blob.FallState;

                if (!Blob.Input.SprintHeld)
                    return GetNormalGroundedState();

                phase = SprintPhase.Sustained;
            }

            if (!Blob.Sensors.IsGrounded)
                return Blob.FallState;

            if (Blob.Input.JumpPressed)
                return Blob.JumpState;

            if (!Blob.Input.SprintHeld)
                return GetNormalGroundedState();

            // The burst direction is committed. Direction changes become available
            // only in the sustained phase.
            if (Blob.HasMoveInput)
            {
                direction.x = Mathf.Sign(Blob.Input.MoveX);
                Blob.SetFacingDirection(direction.x);
            }

            return null;
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            if (Blob.IsGrappling)
                return;

            if (phase == SprintPhase.Burst)
            {
                Blob.Body.linearVelocity = direction * Blob.SprintBurstSpeed;
                return;
            }

            Blob.SetHorizontalVelocity(direction.x * Blob.SustainedSprintSpeed);
        }

        private BlobState GetNormalGroundedState()
        {
            return Blob.HasMoveInput ? Blob.MoveState : Blob.IdleState;
        }
    }
}
