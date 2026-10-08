using BlobGame.Player.Animation;

namespace BlobGame.Player.StateMachine.States
{
    public sealed class BlobFallState : BlobState
    {
        public BlobFallState(BlobController blob) : base(blob)
        {
        }

        public override void Enter()
        {
            Blob.SetGravity(Blob.FallGravityScale);
            Blob.Animation.Play(BlobAnimationId.Fall);
        }

        public override BlobState Tick(float deltaTime)
        {
            if (!Blob.Sensors.IsGrounded || Blob.Body.linearVelocity.y > 0.05f)
                return null;

            return Blob.HasMoveInput ? Blob.MoveState : Blob.IdleState;
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            Blob.ApplyAirMovement();
            Blob.ClampFallSpeed();
        }
    }
}
