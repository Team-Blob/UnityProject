using UnityEngine;

namespace BlobGame.Player.StateMachine.States
{
    /// <summary>
    /// A grounded-only, two-phase sprint. A tap completes the committed Burst,
    /// while holding sprint continues into Sustained movement. Burst locks its
    /// initial direction; Sustained permits direction changes. Releasing sprint
    /// returns to Idle or Move. Jumping or leaving the ground takes priority and
    /// exits the state, so this state never provides an airborne sprint.
    /// </summary>
    public sealed class BlobSprintState : BlobGroundedState
    {
        private enum SprintPhase
        {
            Burst,
            Sustained
        }

        private SprintPhase phase;
        private float direction;
        private float burstElapsed;

        public BlobSprintState(BlobController blob) : base(blob)
        {
        }

        public override void Enter()
        {
            direction = Blob.HasMoveInput
                ? Mathf.Sign(Blob.Input.MoveX)
                : Blob.FacingDirection;

            Blob.SetFacingDirection(direction);
            phase = SprintPhase.Burst;
            burstElapsed = 0f;
        }

        public override BlobState Tick(float deltaTime)
        {
            BlobState transition = TickGrounded();
            if (transition != null)
                return transition;

            if (phase == SprintPhase.Burst)
            {
                burstElapsed += deltaTime;
                if (burstElapsed < Blob.SprintBurstDuration)
                    return null;

                phase = SprintPhase.Sustained;

                // A tap always completes the burst, then returns to normal movement.
                if (!Blob.Input.SprintHeld)
                    return GetNormalGroundedState();
            }

            if (!Blob.Input.SprintHeld)
                return GetNormalGroundedState();

            // The burst direction is committed. Direction changes become available
            // only in the sustained phase.
            if (Blob.HasMoveInput)
            {
                direction = Mathf.Sign(Blob.Input.MoveX);
                Blob.SetFacingDirection(direction);
            }

            return null;
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            float speed = phase == SprintPhase.Burst
                ? Blob.SprintBurstSpeed
                : Blob.SustainedSprintSpeed;

            Blob.SetHorizontalVelocity(direction * speed);
        }

        private BlobState GetNormalGroundedState()
        {
            return Blob.HasMoveInput ? Blob.MoveState : Blob.IdleState;
        }
    }
}
