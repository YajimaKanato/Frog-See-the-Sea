using UnityEngine;

namespace FrogSea
{
    // Everything is scene/prefab-authored. Endless mode reuses a preplaced visual pool.
    public sealed class WellView : MonoBehaviour
    {
        [SerializeField] StageLayout[] stages;
        [SerializeField] Transform backdrop;
        [SerializeField] GameObject floor;
        [SerializeField] Transform sea;
        [SerializeField] GameObject endlessRoot;
        [SerializeField] PlatformDefinition[] endlessPool;
        [SerializeField] Camera viewCamera;
        StageLayout selected;
        StageModel stage;
        FrogModel frog;
        float circumference;
        public StageModel Prepare(GameMode mode, int number, float circumference)
        {
            this.circumference = circumference;
            foreach (var layout in stages) { layout.ResetDisplay(); layout.gameObject.SetActive(false); }
            selected = mode == GameMode.Stages ? stages[Mathf.Clamp(number - 1, 0, stages.Length - 1)] : null;
            if (selected != null) selected.gameObject.SetActive(true);
            stage = selected != null ? selected.ReadModel(circumference) : new StageModel(mode, number, circumference);
            endlessRoot.SetActive(mode == GameMode.Endless);
            foreach (var slot in endlessPool) slot.gameObject.SetActive(false);
            backdrop.gameObject.SetActive(true); floor.SetActive(true); sea.gameObject.SetActive(false);
            backdrop.localPosition = new Vector3(0, 36, 0);
            return stage;
        }
        public void Bind(FrogModel model) { frog = model; Draw(); }
        void LateUpdate() { if (frog != null && frog.State != FrogState.Finished) Draw(); }
        void Draw()
        {
            backdrop.localPosition = new Vector3(-Mathf.Repeat(frog.Position.x * 0.5f, 4), frog.Position.y + 30, 0);
            float half = viewCamera.orthographicSize * viewCamera.aspect;
            if (selected != null)
            {
                foreach (var platform in selected.Platforms)
                {
                    var data = platform.Data;
                    platform.Display(data, frog.Position.x, circumference, half, frog.Position.y, data.Y >= stage.GoalHeight);
                }
            }
            else
            {
                int used = 0;
                foreach (var ledge in stage.Ledges)
                {
                    if (ledge.Y == 0 || Mathf.Abs(ledge.Y - frog.Position.y) > 10) continue;
                    if (used >= endlessPool.Length) { Debug.LogError("Endless platform pool is too small.", this); break; }
                    var slot = endlessPool[used++]; slot.gameObject.SetActive(true);
                    slot.Display(ledge, frog.Position.x, circumference, half, frog.Position.y, false);
                }
                for (int i = used; i < endlessPool.Length; i++) endlessPool[i].gameObject.SetActive(false);
            }
        }
        public void RevealSea()
        {
            foreach (var layout in stages) layout.gameObject.SetActive(false);
            endlessRoot.SetActive(false); backdrop.gameObject.SetActive(false); floor.SetActive(false);
            sea.position = new Vector3(0, stage.GoalHeight, 0); sea.gameObject.SetActive(true);
        }
    }
}
