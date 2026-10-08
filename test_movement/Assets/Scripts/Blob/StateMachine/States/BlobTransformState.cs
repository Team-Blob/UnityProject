using BlobGame.Player.Animation;
using BlobGame.Player.Forms;

namespace BlobGame.Player.StateMachine.States
{
    /// <summary>
    /// Locks normal locomotion while a material transformation performs:
    /// old form FadeOut -> profile swap -> new form FadeIn.
    /// </summary>
    public sealed class BlobTransformState : BlobState
    {
        private enum TransformStage
        {
            FadeOut,
            FadeIn
        }

        private BlobMaterialProfile targetProfile;
        private TransformStage stage;
        private float stageElapsed;
        private float fadeOutDuration;

        public BlobTransformState(BlobController blob) : base(blob)
        {
        }

        public bool Prepare(BlobMaterialProfile profile)
        {
            if (profile == null || Blob.Materials.IsTransforming)
                return false;

            targetProfile = profile;
            return true;
        }

        public override void Enter()
        {
            stage = TransformStage.FadeOut;
            stageElapsed = 0f;
            fadeOutDuration = Blob.Materials.CurrentProfile != null
                ? Blob.Materials.CurrentProfile.TransformFadeOutDuration
                : 0.25f;

            Blob.Materials.SetTransforming(true);
            Blob.StopHorizontalMovement();
            Blob.Animation.Play(BlobAnimationId.TransformFadeOut, 0f);
        }

        public override BlobState Tick(float deltaTime)
        {
            stageElapsed += deltaTime;

            if (stage == TransformStage.FadeOut && stageElapsed >= fadeOutDuration)
            {
                Blob.Materials.ApplyProfile(targetProfile);
                Blob.Animation.Play(BlobAnimationId.TransformFadeIn, 0f);
                stage = TransformStage.FadeIn;
                stageElapsed = 0f;
                return null;
            }

            if (stage == TransformStage.FadeIn &&
                stageElapsed >= targetProfile.TransformFadeInDuration)
            {
                Blob.Materials.SetTransforming(false);
                return Blob.Sensors.IsGrounded
                    ? (Blob.HasMoveInput ? Blob.MoveState : Blob.IdleState)
                    : Blob.FallState;
            }

            return null;
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            Blob.StopHorizontalMovement();
        }

        public override void Exit()
        {
            Blob.Materials.SetTransforming(false);
            targetProfile = null;
        }
    }
}
