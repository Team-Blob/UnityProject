using BlobGame.Player.StateMachine;
using UnityEngine;

namespace BlobGame.Player
{
    /// <summary>
    /// Translates the gameplay state machine into presentation-only animation.
    /// Gameplay remains authoritative: one-shot clips may finish or be interrupted
    /// without delaying movement, jumping, landing, or state transitions.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public sealed class BlobAnimationController : MonoBehaviour
    {
        private const string Idle = "Idle";
        private const string Move = "Move";
        private const string JumpPrepare = "JumpPrepare";
        private const string JumpRise = "JumpRise";
        private const string JumpApex = "JumpApex";
        private const string Fall = "Fall";
        private const string Land = "Land";
        private const string SprintBurst = "SprintBurst";

        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Playback")]
        [SerializeField] private BlobAnimationConfig config;
        [Tooltip("Playback multiplier used by the normal Move clip during sustained sprint.")]
        [SerializeField, Min(1f)] private float sustainedSprintAnimationSpeed = 1.65f;

        private BlobController blob;
        private BlobState previousGameplayState;
        private int currentVisualStateHash;
        private VisualSequence sequence;

        private enum VisualSequence
        {
            None,
            JumpPrepare,
            JumpApex,
            Land
        }

        private void Reset()
        {
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void OnValidate()
        {
            if (animator == null)
                animator = GetComponent<Animator>();

            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Initialize(BlobController owner)
        {
            blob = owner;
            OnValidate();
            previousGameplayState = null;
            sequence = VisualSequence.None;
            TickVisuals();
        }

        public void TickVisuals()
        {
            if (blob == null || animator == null || spriteRenderer == null || blob.CurrentState == null)
                return;

            spriteRenderer.flipX = blob.FacingDirection < 0f;

            BlobState gameplayState = blob.CurrentState;
            if (gameplayState != previousGameplayState)
                EnterGameplayState(previousGameplayState, gameplayState);
            else
                UpdateCurrentVisual(gameplayState);

            previousGameplayState = gameplayState;
        }

        private void EnterGameplayState(BlobState previousState, BlobState currentState)
        {
            if (currentState == blob.JumpState)
            {
                Play(JumpPrepare);
                sequence = VisualSequence.JumpPrepare;
                return;
            }

            if (currentState == blob.FallState)
            {
                if (previousState == blob.JumpState)
                {
                    Play(JumpApex);
                    sequence = VisualSequence.JumpApex;
                }
                else
                {
                    Play(Fall);
                    sequence = VisualSequence.None;
                }

                return;
            }

            if (previousState == blob.FallState)
            {
                Play(Land);
                sequence = VisualSequence.Land;
                return;
            }

            sequence = VisualSequence.None;
            PlayGroundedState(currentState);
        }

        private void UpdateCurrentVisual(BlobState gameplayState)
        {
            if (sequence == VisualSequence.JumpPrepare)
            {
                if (gameplayState != blob.JumpState)
                    return;

                if (HasCurrentClipFinished())
                {
                    Play(JumpRise);
                    sequence = VisualSequence.None;
                }

                return;
            }

            if (sequence == VisualSequence.JumpApex)
            {
                if (gameplayState != blob.FallState)
                    return;

                if (HasCurrentClipFinished())
                {
                    Play(Fall);
                    sequence = VisualSequence.None;
                }

                return;
            }

            if (sequence == VisualSequence.Land)
            {
                // Movement and sprint are allowed to interrupt the landing squash.
                if (gameplayState != blob.IdleState || HasCurrentClipFinished())
                {
                    sequence = VisualSequence.None;
                    PlayGroundedState(gameplayState);
                }

                return;
            }

            if (gameplayState == blob.SprintState)
            {
                int desiredHash = blob.SprintState.IsBurstPhase
                    ? Animator.StringToHash(SprintBurst)
                    : Animator.StringToHash(Move);

                if (desiredHash != currentVisualStateHash)
                    PlayGroundedState(gameplayState);
            }
        }

        private void PlayGroundedState(BlobState gameplayState)
        {
            if (gameplayState == blob.IdleState)
            {
                Play(Idle);
                return;
            }

            if (gameplayState == blob.MoveState)
            {
                Play(Move);
                return;
            }

            if (gameplayState == blob.SprintState)
            {
                if (blob.SprintState.IsBurstPhase)
                    Play(SprintBurst);
                else
                    Play(Move, config != null ? config.SustainedSprintAnimationSpeed : sustainedSprintAnimationSpeed);
            }
        }

        private void Play(string stateName, float playbackSpeed = 1f)
        {
            int stateHash = Animator.StringToHash(stateName);
            animator.speed = playbackSpeed;
            animator.Play(stateHash, 0, 0f);
            currentVisualStateHash = stateHash;
        }

        private bool HasCurrentClipFinished()
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            return stateInfo.shortNameHash == currentVisualStateHash && stateInfo.normalizedTime >= 1f;
        }
    }
}
