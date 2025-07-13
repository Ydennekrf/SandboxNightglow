public interface IStatusEffect
{

   
    /// Apply first tick immediately when the effect is added.
    void Start(Entity target);

    /// Called every frame with delta time.
    /// Return true when the effect has finished and should be removed.
    bool Tick(float delta, Entity target);
}