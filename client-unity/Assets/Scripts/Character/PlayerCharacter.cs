using KingdomsEightBanners.Core;
using UnityEngine;

namespace KingdomsEightBanners.Character
{
    /// <summary>
    /// Holds the character sheet (stats, levels, HP/SP) for the local player.
    /// OFFLINE PROTOTYPE ONLY: in multiplayer the server owns these numbers and
    /// this component only mirrors what the server sends.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class PlayerCharacter : MonoBehaviour
    {
        [Tooltip("Seconds after spending SP before it starts regenerating.")]
        [SerializeField] private float spRegenDelay = 1.0f;

        public CharacterSheet Sheet { get; private set; }

        private float _lastSpSpendTime = float.NegativeInfinity;

        private void Awake()
        {
            Sheet = CharacterSheet.CreateStormTestCharacter();
        }

        private void Update()
        {
            var stats = Sheet.Stats;
            bool spRegenAllowed = Time.time - _lastSpSpendTime >= spRegenDelay;
            Sheet.Vitals.Regenerate(
                Time.deltaTime,
                StatFormulas.HpRegenPerSecond(stats),
                spRegenAllowed ? StatFormulas.SpRegenPerSecond(stats) : 0f);
        }

        /// <summary>Spends SP and pauses SP regeneration briefly.</summary>
        public bool TrySpendSp(float amount)
        {
            if (!Sheet.Vitals.TrySpendSp(amount)) return false;
            if (amount > 0f) _lastSpSpendTime = Time.time;
            return true;
        }

        /// <summary>Called by the gameplay code after a skill already paid its SP cost.</summary>
        public void NotifySpSpent()
        {
            _lastSpSpendTime = Time.time;
        }
    }
}
