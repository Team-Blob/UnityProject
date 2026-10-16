namespace BlobGame.Player.StateMachine
{
    public sealed class BlobStateMachine
    {
        public BlobState CurrentState { get; private set; }
        public bool IsRunning { get; private set; } = true;

        public void Initialize(BlobState startingState)
        {
            if (startingState == null)
                return;

            CurrentState = startingState;
            CurrentState.Enter();
        }

        public void Tick(float deltaTime)
        {
            if (!IsRunning || CurrentState == null)
                return;

            BlobState nextState = CurrentState.Tick(deltaTime);
            if (nextState == null || nextState == CurrentState)
                return;

            ChangeState(nextState);
        }

        public void FixedTick(float fixedDeltaTime)
        {
            if (!IsRunning || CurrentState == null)
                return;

            CurrentState.FixedTick(fixedDeltaTime);
        }

        public void ForceChangeState(BlobState nextState)
        {
            if (!IsRunning || nextState == null || nextState == CurrentState)
                return;

            ChangeState(nextState);
        }

        public void Stop()
        {
            IsRunning = false;
        }

        private void ChangeState(BlobState nextState)
        {
            CurrentState.Exit();
            CurrentState = nextState;
            CurrentState.Enter();
        }
    }
}
