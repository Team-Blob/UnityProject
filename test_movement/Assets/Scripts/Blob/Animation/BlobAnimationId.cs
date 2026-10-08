namespace BlobGame.Player.Animation
{
    /// <summary>
    /// Form-independent animation requests emitted by gameplay states.
    /// Every Blob form maps these slots to its own clips.
    /// </summary>
    public enum BlobAnimationId
    {
        Idle,
        Move,
        JumpStart,
        JumpRise,
        Fall,
        Land,
        TransformFadeOut,
        TransformFadeIn,
        Interact,
        Hurt,
        Death,
        WallCling,
        WallSlide,
        WallJump,
        AbilityPrimary,
        AbilitySecondary
    }
}
