namespace BlobGame.Player.StateMachine
{
    public abstract class BlobGroundedState : BlobState
    {
        protected BlobGroundedState(BlobController blob) : base(blob)
        {
        }

        protected BlobState GetMovementTransition()
        {
            if (!Blob.Sensors.IsGrounded)
                return Blob.FallState;

            if (Blob.Input.JumpPressed)
                return Blob.JumpState;

            return null;
        }
    }
}
