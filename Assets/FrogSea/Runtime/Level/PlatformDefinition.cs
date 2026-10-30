using UnityEngine;

namespace FrogSea
{
    // Move this prefab instance to edit the route; Width edits both art and collision data.
    public sealed class PlatformDefinition : MonoBehaviour
    {
        [Min(0.5f), SerializeField] float width = 2.7f;
        [SerializeField] bool sticky;
        [SerializeField] bool goal;
        [SerializeField] PlatformArtwork artwork;
        public Ledge Data => new Ledge(transform.position.x, transform.position.y, width, sticky);
        public void RefreshArtwork() { if (artwork != null) artwork.Configure(width, sticky, goal); }
        void OnValidate() => RefreshArtwork();
        public void Display(Ledge data, float playerX, float circumference, float halfWidth, float playerY, bool isGoal)
        {
            float x = StageModel.WrappedDelta(data.X, playerX, circumference);
            bool visible = Mathf.Abs(x) < halfWidth + data.Width / 2 + 1 && Mathf.Abs(data.Y - playerY) < 11;
            artwork.gameObject.SetActive(visible);
            if (!visible) return;
            artwork.transform.position = new Vector3(x, data.Y, 0);
            artwork.Configure(data.Width, data.Sticky, isGoal);
        }
        public void ResetDisplay()
        {
            artwork.transform.localPosition = Vector3.zero; artwork.gameObject.SetActive(true); RefreshArtwork();
        }
    }
}
