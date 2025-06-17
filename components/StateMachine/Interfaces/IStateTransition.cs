using Godot;
using System;

public interface IStateTransition
{
    BaseState Target { get; }
    bool ShouldTransition(Entity owner);
}
