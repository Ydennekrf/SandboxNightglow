using Godot;

public enum Facing { Down, Left, Right, Up }


    public static class FacingExtensions
{
    public static Vector2 ToVector(this Facing facing)
    {
        return facing switch
        {
            Facing.Left  => Vector2.Left,
            Facing.Right => Vector2.Right,
            Facing.Up    => Vector2.Up,
            _            => Vector2.Down
        };
    }
}