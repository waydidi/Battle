using System;
using KingdomsEightBanners.Core;
using UnityEngine;

namespace KingdomsEightBanners.Combat
{
    /// <summary>
    /// Anything that can be hit: training dummies now, slimes and players later.
    /// OFFLINE PROTOTYPE: in multiplayer only the server applies damage; clients play effects.
    /// </summary>
    public sealed class Damageable : MonoBehaviour
    {
        [SerializeField] private string displayName = "Training Dummy";
        [SerializeField] private int maxHp = 3000;
        [SerializeField] private float defense = 40f;
        [Tooltip("Seconds until the target comes back after being destroyed. 0 = never.")]
        [SerializeField] private float respawnSeconds = 3f;
        [SerializeField] private Renderer flashRenderer;
        [SerializeField] private Color flashColor = new Color(1f, 0.35f, 0.25f);

        private Vitals _vitals;
        private Color _baseColor;
        private float _flashUntil;
        private float _respawnAt;

        public event Action<Damageable, DamageResult> Damaged;
        public event Action<Damageable> Died;

        public string DisplayName => displayName;
        public float Defense => defense;
        public Vitals Vitals => _vitals;
        public bool IsDead => _vitals.IsDead;

        private void Awake()
        {
            _vitals = new Vitals(maxHp, 0);
            if (flashRenderer == null) flashRenderer = GetComponentInChildren<Renderer>();
            if (flashRenderer != null) _baseColor = flashRenderer.material.color;
        }

        public void ApplyDamage(DamageResult damage)
        {
            if (IsDead) return;
            _vitals.TakeDamage(damage.Amount);
            _flashUntil = Time.time + 0.12f;
            Damaged?.Invoke(this, damage);
            FloatingCombatText.Spawn(
                transform.position + Vector3.up * 2.2f,
                damage.IsCritical ? damage.Amount + "!" : damage.Amount.ToString(),
                damage.IsCritical ? new Color(1f, 0.8f, 0.2f) : Color.white);

            if (IsDead)
            {
                Died?.Invoke(this);
                if (respawnSeconds > 0f) _respawnAt = Time.time + respawnSeconds;
                SetVisible(false);
            }
        }

        private void Update()
        {
            if (flashRenderer != null)
            {
                flashRenderer.material.color = Time.time < _flashUntil ? flashColor : _baseColor;
            }

            if (IsDead && respawnSeconds > 0f && Time.time >= _respawnAt)
            {
                _vitals = new Vitals(maxHp, 0);
                SetVisible(true);
            }
        }

        private void SetVisible(bool visible)
        {
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = visible;
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = visible;
        }
    }
}
