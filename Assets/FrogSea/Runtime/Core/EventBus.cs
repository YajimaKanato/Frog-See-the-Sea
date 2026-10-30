using System;
using System.Collections.Generic;

namespace FrogSea
{
    /// <summary>Scene-owned typed events. Dispose subscriptions together with their view.</summary>
    public sealed class EventBus
    {
        readonly Dictionary<Type, Delegate> listeners = new Dictionary<Type, Delegate>();
        public IDisposable Subscribe<T>(Action<T> listener)
        {
            listeners.TryGetValue(typeof(T), out var existing);
            listeners[typeof(T)] = Delegate.Combine(existing, listener);
            return new Subscription(() =>
            {
                if (listeners.TryGetValue(typeof(T), out var current))
                {
                    var remaining = Delegate.Remove(current, listener);
                    if (remaining == null) listeners.Remove(typeof(T));
                    else listeners[typeof(T)] = remaining;
                }
            });
        }
        public void Publish<T>(T message)
        {
            if (listeners.TryGetValue(typeof(T), out var listener)) ((Action<T>)listener)(message);
        }
        sealed class Subscription : IDisposable
        {
            Action dispose;
            public Subscription(Action dispose) { this.dispose = dispose; }
            public void Dispose() { var action = dispose; dispose = null; action?.Invoke(); }
        }
    }
    public readonly struct Jumped
    {
        public readonly bool FromWall;
        public readonly float Power;
        public Jumped(bool fromWall, float power) { FromWall = fromWall; Power = power; }
    }
    public readonly struct Landed { public readonly float Speed; public Landed(float speed) { Speed = speed; } }
    public readonly struct WallAttached { }
    public readonly struct Fell { }
    public readonly struct GoalReached { }
    public readonly struct RunStarted { }
}
