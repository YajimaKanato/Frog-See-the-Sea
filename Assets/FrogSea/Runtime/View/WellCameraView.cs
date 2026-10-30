using UnityEngine;

namespace FrogSea
{
    [RequireComponent(typeof(Camera))]
    public sealed class WellCameraView : MonoBehaviour
    {
        FrogModel model;
        float circumference = 32;
        public void Initialize(FrogModel model, float circumference = 32)
        {
            this.model = model;
            this.circumference = circumference;
            var camera = GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 6.7f;
            camera.nearClipPlane = 0.1f; camera.farClipPlane = 50;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.07f, 0.12f, 0.15f);
            transform.SetPositionAndRotation(new Vector3(0, 2.8f, -10), Quaternion.identity);
        }
        void LateUpdate()
        {
            if (model == null) return;
            // Keep the view narrower than one lap; wrap occurs outside the visible region.
            var camera = GetComponent<Camera>();
            camera.orthographicSize = Mathf.Min(6.7f, (circumference / 2 - 2) / camera.aspect);
            float target = Mathf.Max(2.8f, model.Position.y + 2.4f);
            if (model.State == FrogState.Finished) target = model.Position.y + 3.5f;
            float y = Mathf.Lerp(transform.position.y, target, 1 - Mathf.Exp(-Time.deltaTime * 7));
            transform.position = new Vector3(0, y, -10);
        }
    }
}
