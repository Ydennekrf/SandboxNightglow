namespace ethra.V1
{
    /// <summary>
    /// Published by DialogManager when a dialog tree opens.
    /// </summary>
    public readonly record struct DialogStartedEvent(string DialogTreeId);

    /// <summary>
    /// Published by DialogManager when the active dialog tree closes.
    /// </summary>
    public readonly record struct DialogEndedEvent(string DialogTreeId);

    /// <summary>
    /// Published by DialogManager whenever the active dialog node changes.
    /// </summary>
    /// <param name="DialogTreeId">Stable dialog tree ID.</param>
    /// <param name="NodeId">Stable node ID inside the dialog tree.</param>
    /// <param name="SpeakerName">Resolved speaker name for UI and animation routing.</param>
    /// <param name="IsPlayerSpeaker">True when the speaker should drive player dialog animation.</param>
    /// <param name="AnimationKey">Requested animation key, usually consumed by player state/actions.</param>
    public readonly record struct DialogNodeChangedEvent(
        string DialogTreeId,
        string NodeId,
        string SpeakerName,
        bool IsPlayerSpeaker,
        string AnimationKey);
}
