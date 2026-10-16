namespace BlobGame.Player.StateMachine
{
    public abstract class BlobState
    {
        protected BlobController Blob { get; }

        protected BlobState(BlobController blob)
        {
            Blob = blob;
        }

        public virtual void Enter()
        {
        }

        /// <summary>
        /// Runs once per rendered frame. Its primary responsibilities are reading
        /// frame-based input, updating state timing or phases, and returning a
        /// requested transition. Returning null keeps the current state. A Tick
        /// may also cache one-shot physics requests for the next FixedTick.
        /// </summary>
        public abstract BlobState Tick(float deltaTime);

        /// <summary>
        /// Runs on Unity's fixed physics timestep. Its primary responsibility is
        /// applying Rigidbody2D velocity, gravity, and jump-cut changes, including
        /// consuming physics requests previously cached by Tick.
        /// </summary>
        public virtual void FixedTick(float fixedDeltaTime)
        {
        }

        public virtual void Exit()
        {
        }
    }
}
