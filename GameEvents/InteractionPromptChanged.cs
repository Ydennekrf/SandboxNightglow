using Godot;

/// <summary>
/// UI event payload published by interactable selectors/sources when the visible prompt should change.
/// </summary>
public readonly record struct InteractionPromptChanged(string PromptText, Node Source);
