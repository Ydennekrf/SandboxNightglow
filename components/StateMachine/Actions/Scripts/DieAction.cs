using Godot;


public partial class DieAction : Node, IStateAction
{
    public void Enter(Entity o, BaseState state)
    {
        if (o is Enemy enemy)
        {
            enemy.Die();
        }

        if (o is Player player)
        {
            player.Die();
        }

    }

    public void Execute(float dt, Entity o, BaseState state)
    {

        
    }

    public void Exit(Entity o)
    {

    }
}