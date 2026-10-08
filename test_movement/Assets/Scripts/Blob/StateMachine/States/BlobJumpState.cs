using BlobGame.Player.Animation;

namespace BlobGame.Player.StateMachine.States
{
    public sealed class BlobJumpState : BlobState
    {
        private bool jumpCutApplied;

        public BlobJumpState(BlobController blob) : base(blob)
        {
        }

        public override void Enter()
        {
            jumpCutApplied = false;
            Blob.SetGravity(Blob.RiseGravityScale);
            Blob.SetVerticalVelocity(Blob.GetCurrentJumpSpeed());
            Blob.Animation.Play(BlobAnimationId.JumpRise);
        }

        public override BlobState Tick(float deltaTime)
        {
            if (!jumpCutApplied && Blob.Input.JumpReleased && Blob.Body.linearVelocity.y > 0f)
            {
                jumpCutApplied = true;
                Blob.CutJump();
                Blob.SetGravity(Blob.ReleasedRiseGravityScale);
            }

            return Blob.Body.linearVelocity.y <= 0f ? Blob.FallState : null;
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            Blob.ApplyAirMovement();
        }
    }
}
