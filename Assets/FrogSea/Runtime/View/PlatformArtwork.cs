using UnityEngine;

namespace FrogSea
{
    public sealed class PlatformArtwork : MonoBehaviour
    {
        [SerializeField] SpriteRenderer stone;
        [SerializeField] Transform top;
        [SerializeField] Transform shadow;
        [SerializeField] GameObject mossWall;
        [SerializeField] Color stoneColor = new Color(0.56f, 0.55f, 0.43f);
        [SerializeField] Color goalColor = new Color(0.88f, 0.75f, 0.43f);
        public void Configure(float width, bool sticky, bool goal)
        {
            stone.transform.localScale = new Vector3(width, 0.25f, 1);
            top.localScale = new Vector3(width + 0.04f, 0.09f, 1);
            shadow.localScale = new Vector3(width, 0.14f, 1);
            stone.color = goal ? goalColor : stoneColor;
            mossWall.SetActive(sticky);
        }
    }
}
