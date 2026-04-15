

using System;

namespace ethra.V1
{
    public interface IEvent
    {
        void Subscribe<T>(GameEvent evt, Action<T> handler);

        void Unsubscribe<T>(GameEvent evt, Action<T> handler);

        void Publish<T>(GameEvent evt, T payload);

        void Subscribe(GameEvent evt, Action handler);
        void Unsubscribe(GameEvent evt, Action handler);

        void Publish(GameEvent evt);
    }
}