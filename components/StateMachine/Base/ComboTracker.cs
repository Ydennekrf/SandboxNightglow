
using System;
using System.Collections.Generic;
public class ComboTracker
{
    public IList<ComboPhase> Phases;
    public int Index;
    public float TimerRemaining;

    public ComboTracker(IList<ComboPhase> phases)
    {
        Phases = phases;
        Reset();
    }

    public void Reset()
    {
        Index = 0;
        TimerRemaining = 0;
    }

    public ComboPhase Current => Index < Phases.Count ? Phases[Index] : null;
}