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

        public abstract BlobState Tick(float deltaTime);

        public virtual void FixedTick(float fixedDeltaTime)
        {
        }

        public virtual void Exit()
        {
        }
    }
}
