using System;

namespace BlobGame.Player.Forms
{
    [Flags]
    public enum BlobAbility
    {
        None = 0,
        WallCling = 1 << 0,
        Grow = 1 << 1
    }
}
