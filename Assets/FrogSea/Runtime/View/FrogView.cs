using System;
using UnityEngine;

namespace FrogSea
{
    public sealed class FrogView : MonoBehaviour
    {
        [SerializeField] Transform artwork;
        FrogModel model;
        IDisposable jumped, landed, attached;
        float pulse;
        public void Initialize(FrogModel model, EventBus events)
        {
            DisposeSubscriptions(); this.model = model; pulse = 0;
            transform.position = Vector3.zero;
            jumped = events.Subscribe<Jumped>(_ => pulse = -0.18f);
            landed = events.Subscribe<Landed>(e => pulse = Mathf.Min(0.28f, e.Speed * 0.015f));
            attached = events.Subscribe<WallAttached>(_ => pulse = 0.12f);
        }
        void LateUpdate()
        {
            if (model == null) return;
            transform.position = new Vector3(0, model.Position.y, 0);
            pulse = Mathf.Lerp(pulse, 0, Time.deltaTime * 12);
            artwork.localScale = new Vector3(1 + pulse, 1 - pulse, 1);
            artwork.localRotation = Quaternion.Euler(0, 0, Mathf.Clamp(-model.Velocity.x * 2, -15, 15));
        }
        void DisposeSubscriptions() { jumped?.Dispose(); landed?.Dispose(); attached?.Dispose(); }
        void OnDestroy() => DisposeSubscriptions();
    }
}
