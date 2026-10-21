using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace KingdomsEightBanners.UI
{
    /// <summary>
    /// Small helpers for building uGUI screens from code (placeholder look until real UI art exists).
    /// Colours follow the mockup: dark translucent panels with gold accents.
    /// </summary>
    public static class UiKit
    {
        public static readonly Color Panel = new Color(0.05f, 0.05f, 0.08f, 0.65f);
        public static readonly Color PanelSolid = new Color(0.07f, 0.06f, 0.06f, 0.94f);
        public static readonly Color Gold = new Color(0.95f, 0.76f, 0.36f);
        public static readonly Color GoldDim = new Color(0.55f, 0.43f, 0.22f);
        public static readonly Color TextMuted = new Color(0.80f, 0.78f, 0.72f);

        private static Font _font;
        private static Sprite _whiteSprite;

        public static Font Font
        {
            get
            {
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        /// <summary>Plain white sprite; needed for Filled images, which ignore fill without a sprite.</summary>
        public static Sprite WhiteSprite
        {
            get
            {
                if (_whiteSprite == null)
                {
                    var texture = Texture2D.whiteTexture;
                    _whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                }

                return _whiteSprite;
            }
        }

        public static RectTransform CreateCanvas(Transform parent, string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return (RectTransform)go.transform;
        }

        /// <summary>Buttons need an EventSystem in the scene; create one if the scene has none.</summary>
        public static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        public static Image CreateImage(RectTransform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Text CreateText(RectTransform parent, string name, int size, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = Font;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = Color.white;
            text.raycastTarget = false;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            return text;
        }

        public static Button CreateButton(RectTransform parent, string name, string label, Color color, int fontSize = 22)
        {
            var image = CreateImage(parent, name, color);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var text = CreateText(image.rectTransform, "Label", fontSize, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            text.text = label;
            return button;
        }

        /// <summary>Adds a thin coloured border around a rect (four child images).</summary>
        public static void AddBorder(RectTransform rect, Color color, float thickness = 2f)
        {
            Edge(rect, "Border Top", color, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -thickness), Vector2.zero);
            Edge(rect, "Border Bottom", color, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, thickness));
            Edge(rect, "Border Left", color, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(thickness, 0f));
            Edge(rect, "Border Right", color, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-thickness, 0f), Vector2.zero);
        }

        /// <summary>Anchors a rect at a point; position is the rect's top-left corner relative to that anchor.</summary>
        public static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        /// <summary>Places a rect by its centre, relative to the parent's top-left corner.</summary>
        public static void PlaceCentered(RectTransform rect, Vector2 centerFromTopLeft, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(centerFromTopLeft.x, -centerFromTopLeft.y);
            rect.sizeDelta = size;
        }

        public static void Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, 0f);
            rect.offsetMax = new Vector2(-padding, 0f);
        }

        /// <summary>Draws a straight line between two points given relative to the parent's top-left corner.</summary>
        public static Image Line(RectTransform parent, Vector2 from, Vector2 to, Color color, float thickness = 3f)
        {
            var line = CreateImage(parent, "Line", color);
            var rect = line.rectTransform;
            Vector2 delta = to - from;
            PlaceCentered(rect, (from + to) / 2f, new Vector2(delta.magnitude + thickness, thickness));
            // Screen y grows downwards in our "from top-left" coordinates, so negate it for the angle.
            rect.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            return line;
        }

        private static void Edge(RectTransform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var edge = CreateImage(parent, name, color);
            var rect = edge.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
