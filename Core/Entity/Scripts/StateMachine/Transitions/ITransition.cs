namespace ethra.V1
{
    public interface IStateTransition
{
    BaseState Target { get; }
    bool ShouldTransition(Entity owner);
}
}