namespace ethra.V1
{
    public readonly record struct CutsceneStartedEvent(string CutsceneId);
    public readonly record struct CutsceneEndedEvent(string CutsceneId, bool WasSkipped);
}
