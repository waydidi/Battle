using UnityEngine;

namespace KingdomsEightBanners.Combat
{
    /// <summary>Small rising damage number that faces the camera and fades out.</summary>
    public sealed class FloatingCombatText : MonoBehaviour
    {
        private const float Lifetime = 0.9f;
        private const float RiseSpeed = 1.4f;

        private TextMesh _text;
        private Color _color;
        private float _age;

        public static void Spawn(Vector3 position, string message, Color color)
        {
            var go = new GameObject("DamageNumber");
            go.transform.position = position + Random.insideUnitSphere * 0.25f;

            var text = go.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.font = font;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            text.text = message;
            text.fontSize = 64;
            text.characterSize = 0.06f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = color;

            var popup = go.AddComponent<FloatingCombatText>();
            popup._text = text;
            popup._color = color;
        }

        private void LateUpdate()
        {
            _age += Time.deltaTime;
            if (_age >= Lifetime)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += Vector3.up * (RiseSpeed * Time.deltaTime);
            var cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;

            var c = _color;
            c.a = 1f - _age / Lifetime;
            _text.color = c;
        }
    }
}
