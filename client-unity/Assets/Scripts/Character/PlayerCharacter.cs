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
        [Tooltip("HP/SP regeneration multiplier while sitting.")]
        [SerializeField] private float sitRegenMultiplier = 2f;
        [Tooltip("HP/SP regeneration multiplier while sleeping.")]
        [SerializeField] private float sleepRegenMultiplier = 3f;

        public CharacterSheet Sheet { get; private set; }
        /// <summary>Learned skills and unspent Skill Points (not the blue SP stamina bar).</summary>
        public SkillTreeState Skills { get; private set; }

        private float _lastSpSpendTime = float.NegativeInfinity;
        private ThirdPersonController _movement;

        private void Awake()
        {
            Sheet = CharacterSheet.CreateStormTestCharacter();
            Skills = SkillTrees.StormTestProgress();
            _movement = GetComponent<ThirdPersonController>();
        }

        private void Update()
        {
            var stats = Sheet.Stats;
            bool spRegenAllowed = Time.time - _lastSpSpendTime >= spRegenDelay;
            float multiplier = RegenMultiplier();
            Sheet.Vitals.Regenerate(
                Time.deltaTime,
                StatFormulas.HpRegenPerSecond(stats) * multiplier,
                spRegenAllowed ? StatFormulas.SpRegenPerSecond(stats) * multiplier : 0f);
        }

        private float RegenMultiplier()
        {
            if (_movement == null) return 1f;
            switch (_movement.Rest)
            {
                case RestState.Sitting: return sitRegenMultiplier;
                case RestState.Sleeping: return sleepRegenMultiplier;
                default: return 1f;
            }
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
