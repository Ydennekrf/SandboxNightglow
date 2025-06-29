public interface IPhaseAction
{
    void OnPhaseStart(Entity user, ComboPhase phase);
    void OnPhaseEnd  (Entity user, ComboPhase phase);
}