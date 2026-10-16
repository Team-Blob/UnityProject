namespace BlobGame.Player.StateMachine
{
    public abstract class BlobGroundedState : BlobState
    {
        protected BlobGroundedState(BlobController blob) : base(blob)
        {
        }

        /// <summary>
        /// Evaluates high-priority transitions shared by all grounded substates.
        /// The check order defines transition priority: leaving the ground before
        /// jumping. A null result means no shared transition was requested, so the
        /// concrete state should continue evaluating its own Tick logic.
        /// </summary>
        protected BlobState TickGrounded()
        {
            if (!Blob.Sensors.IsGrounded)
                return Blob.FallState;

            if (Blob.Input.JumpPressed)
                return Blob.JumpState;

            return null;
        }
    }
}
