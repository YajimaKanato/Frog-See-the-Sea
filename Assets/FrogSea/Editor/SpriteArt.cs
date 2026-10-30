using UnityEngine;

namespace FrogSea
{
    // Code-authored 2D sprites; replace artwork independently of the model.
    public static class SpriteArt
    {
        static Sprite square, frog;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { Release(square); Release(frog); square = frog = null; }
        static void Release(Sprite sprite)
        { if (sprite != null) { Object.Destroy(sprite.texture); Object.Destroy(sprite); } }
        public static Sprite Square
        {
            get
            {
                if (square == null)
                {
                    var texture = new Texture2D(1, 1); texture.SetPixel(0, 0, Color.white); texture.Apply();
                    square = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1);
                }
                return square;
            }
        }
        public static Sprite Frog
        {
            get
            {
                if (frog != null) return frog;
                string[] pixels = {
                    "........................", "......dddd....dddd......", ".....dggggd..dggggd.....",
                    "....dggwwggddggwwggd....", "....dggwkwggggwkwggd....", "....dggwkwggggwkwggd....",
                    "....dggggggggggggggd....", "...dggggggggggggggggd...", "..dggggggggggggggggggd..",
                    "..dgggggbbbbbbbbgggggd..", "..dggggbbbbbbbbbbggggd..", "...dgggbbbbbbbbbbgggd...",
                    "...dgggbbbbbbbbbbgggd...", "....dgggbbbbbbbbgggd....", "...dgggggbbbbbbgggggd...",
                    "..dgggggddddddddgggggd..", ".dgggggd........dgggggd.", ".dddddd..........dddddd.",
                };
                var texture = new Texture2D(24, pixels.Length, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                for (int y = 0; y < pixels.Length; y++)
                for (int x = 0; x < 24; x++)
                {
                    char c = pixels[y][x];
                    var color = c == 'g' ? new Color(0.55f, 0.76f, 0.34f) : c == 'b' ? new Color(0.88f, 0.87f, 0.52f) :
                        c == 'd' ? new Color(0.13f, 0.28f, 0.22f) : c == 'w' ? new Color(0.97f, 0.96f, 0.8f) :
                        c == 'k' ? new Color(0.07f, 0.14f, 0.16f) : Color.clear;
                    texture.SetPixel(x, pixels.Length - y - 1, color);
                }
                texture.Apply(); frog = Sprite.Create(texture, new Rect(0, 0, 24, pixels.Length), new Vector2(0.5f, 0), 24);
                return frog;
            }
        }
        public static SpriteRenderer Block(Transform parent, string name, Vector2 position, Vector2 size, Color color, int order)
        {
            var go = new GameObject(name, typeof(SpriteRenderer)); go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = new Vector3(size.x, size.y, 1);
            var renderer = go.GetComponent<SpriteRenderer>(); renderer.sprite = Square; renderer.color = color; renderer.sortingOrder = order;
            return renderer;
        }
    }
}
