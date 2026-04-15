using Godot;
using System;
using System.Collections.Generic;

namespace ethra.V1
{
    public partial class EventManager : IEvent
{
	 private readonly Dictionary<GameEvent,List<Delegate>> _listeners = new();


    public void Subscribe<T>(GameEvent evt, Action<T> handler)
    {
        if (!_listeners.TryGetValue(evt, out var list))
            _listeners[evt] = list = new List<Delegate>();

        list.Add(handler);
    }


    public void Unsubscribe<T>(GameEvent evt, Action<T> handler)
    {
        if (_listeners.TryGetValue(evt, out var list))
            list.Remove(handler);
    }


    public void Publish<T>(GameEvent evt, T payload)
    {
        if (!_listeners.TryGetValue(evt, out var list)) return;

        // iterate over a copy to allow listeners to unsubscribe inside the handler
        foreach (var d in list.ToArray())
        {
            if (d is Action<T> callback)
                callback.Invoke(payload);
        }
    }


    public void Subscribe(GameEvent evt, Action handler) =>
        Subscribe<object>(evt, _ => handler());

    public void Unsubscribe(GameEvent evt, Action handler) =>
        Unsubscribe<object>(evt, _ => handler());

        

    public void Publish(GameEvent evt) =>
        Publish<object>(evt, null);
}
}

