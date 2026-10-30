using System;
using System.Collections.Generic;
using UnityEngine;

namespace FrogSea
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class FeedbackView : MonoBehaviour
    {
        [SerializeField] AudioSource source;
        [SerializeField] AudioClip jump, land, wall, goal;
        readonly List<IDisposable> subscriptions = new List<IDisposable>();
        public void Initialize(EventBus events)
        {
            Clear();
            subscriptions.Add(events.Subscribe<Jumped>(e => source.PlayOneShot(jump, e.FromWall ? 0.9f : 0.65f)));
            subscriptions.Add(events.Subscribe<Landed>(e => source.PlayOneShot(land, Mathf.Clamp01(e.Speed / 12))));
            subscriptions.Add(events.Subscribe<WallAttached>(_ => source.PlayOneShot(wall)));
            subscriptions.Add(events.Subscribe<GoalReached>(_ => source.PlayOneShot(goal)));
        }
        void Clear() { foreach (var token in subscriptions) token.Dispose(); subscriptions.Clear(); }
        void OnDestroy() => Clear();
    }
}
