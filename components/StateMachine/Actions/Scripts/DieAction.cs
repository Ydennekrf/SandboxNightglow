using Godot;


public partial class DieAction : Node, IStateAction
{

        [Export] public float Duration = 0.25f;  

    private float _timer;

    public void Enter(Entity owner, BaseState _) => _timer = Duration;


    public void Execute(float dt, Entity o, BaseState state)
    {
           _timer -= dt;
        if (_timer < 0f)
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
        
    }

    public void Exit(Entity o)
    {
         
    }
}