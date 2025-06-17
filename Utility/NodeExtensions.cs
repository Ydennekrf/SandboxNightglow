using Godot;

public static class NodeExtensions
{
    public static void QueueFreeChildren(this Node n)
    {
        foreach (var c in n.GetChildren()) (c as Node)?.QueueFree();
    }
}