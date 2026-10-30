using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace FrogSea
{
    // Shared uGUI construction. Rectangles use a 1280 x 720 top-left design space.
    public static class GameUi
    {
        public static readonly Color Ink = new Color(0.9f, 0.92f, 0.81f);
        public static readonly Color Muted = new Color(0.56f, 0.67f, 0.63f);
        public static readonly Color Gold = new Color(0.88f, 0.74f, 0.44f);
        public static readonly Color PanelColor = new Color(0.045f, 0.09f, 0.11f, 0.96f);
        static Font font;
        public static RectTransform Canvas(Transform owner)
        {
            var root = new GameObject("UI Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(owner, false); root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
            if (EventSystem.current == null)
            {
                var events = new GameObject("UI EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(owner, false); events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            return root.GetComponent<RectTransform>();
        }
        public static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height); return rect;
        }
        public static RectTransform Panel(Transform parent, string name, float x, float y, float width, float height, Color color, bool blocks = true)
        {
            var rect = Rect(parent, name, x, y, width, height); var image = rect.gameObject.AddComponent<Image>();
            image.color = color; image.raycastTarget = blocks; return rect;
        }
        public static Text Text(Transform parent, string value, float x, float y, float width, float height, int size, Color color, bool bold = false)
        {
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var rect = Rect(parent, value, x, y, width, height); var label = rect.gameObject.AddComponent<Text>();
            label.font = font; label.text = value; label.fontSize = size; label.color = color;
            label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal; label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }
        public static Button Button(Transform parent, string label, float x, float y, float width, UnityAction action, bool accent = false)
        {
            var rect = Panel(parent, label + " button", x, y, width, 50, accent ? Gold : new Color(0.13f, 0.22f, 0.23f));
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>(); button.onClick.AddListener(action);
            var colors = button.colors; colors.highlightedColor = new Color(1.13f, 1.13f, 1.13f); colors.pressedColor = new Color(0.75f, 0.85f, 0.8f);
            button.colors = colors;
            var text = Text(rect, label, 10, 0, width - 20, 50, 17, accent ? PanelColor : Ink, true); text.alignment = TextAnchor.MiddleCenter;
            return button;
        }
    }
}
