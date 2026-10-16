namespace BlobGame.Player.StateMachine.States
{
    public sealed class BlobJumpState : BlobState
    {
        private bool jumpCutApplied;
        private bool jumpCutRequested;

        public BlobJumpState(BlobController blob) : base(blob)
        {
        }

        public override void Enter()
        {
            jumpCutApplied = false;
            jumpCutRequested = false;
            Blob.SetGravity(Blob.RiseGravityScale);
            Blob.SetVerticalVelocity(Blob.GetCurrentJumpSpeed());
        }

        public override BlobState Tick(float deltaTime)
        {
            if (!jumpCutApplied && Blob.Input.JumpReleased && Blob.Body.linearVelocity.y > 0f)
                jumpCutRequested = true;

            return Blob.Body.linearVelocity.y <= 0f ? Blob.FallState : null;
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            if (jumpCutRequested)
            {
                if (!jumpCutApplied && Blob.Body.linearVelocity.y > 0f)
                {
                    Blob.CutJump();
                    Blob.SetGravity(Blob.ReleasedRiseGravityScale);
                    jumpCutApplied = true;
                }

                // Consume the request even if ascent ended before this physics step.
                jumpCutRequested = false;
            }

            Blob.ApplyAirMovement();
        }
    }
}
