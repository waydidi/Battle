using System;

namespace KingdomsEightBanners.Core
{
    /// <summary>Tracks when an action becomes usable again. Time is passed in, never read globally.</summary>
    [Serializable]
    public sealed class Cooldown
    {
        private double _readyAt;
        private double _duration;

        public bool IsReady(double now) => now >= _readyAt;
        public double Remaining(double now) => Math.Max(0.0, _readyAt - now);

        /// <summary>1 when just started, 0 when ready. Handy for UI fill images.</summary>
        public float RemainingFraction(double now)
        {
            return _duration <= 0.0 ? 0f : (float)(Remaining(now) / _duration);
        }

        public void Start(double now, double duration)
        {
            if (duration < 0.0) throw new ArgumentOutOfRangeException(nameof(duration));
            _duration = duration;
            _readyAt = now + duration;
        }

        public void Reset()
        {
            _readyAt = 0.0;
            _duration = 0.0;
        }
    }

    /// <summary>Static description of a skill. Will move to ScriptableObjects / server data later.</summary>
    [Serializable]
    public sealed class SkillDefinition
    {
        public string Id;
        public string DisplayName;
        public float SpCost;
        public float CooldownSeconds;
        public float DamageMultiplier;
        public float Range;
        public float CastTime;
        public float RecoveryTime;

        /// <summary>The always-available light attack. No SP cost, short cooldown.</summary>
        public static SkillDefinition BasicAttack() => new SkillDefinition
        {
            Id = "basic_attack",
            DisplayName = "Attack",
            SpCost = 0f,
            CooldownSeconds = 0.45f,
            DamageMultiplier = 1.0f,
            Range = 2.2f,
            CastTime = 0.1f,
            RecoveryTime = 0.25f,
        };

        /// <summary>First Desert Vanguard skill (Sunspire birthplace).</summary>
        public static SkillDefinition DesertSlash() => new SkillDefinition
        {
            Id = "desert_slash",
            DisplayName = "Desert Slash",
            SpCost = 15f,
            CooldownSeconds = 4f,
            DamageMultiplier = 2.4f,
            Range = 3.0f,
            CastTime = 0.15f,
            RecoveryTime = 0.4f,
        };
    }

    public enum SkillUseResult
    {
        Success,
        CasterDead,
        OnCooldown,
        NotEnoughSp,
    }

    /// <summary>
    /// Validates and pays for a skill use. The same rules will run on the server,
    /// which is the only place where a skill use actually counts in multiplayer.
    /// </summary>
    public static class SkillRules
    {
        public static double EffectiveCooldown(SkillDefinition skill, CharacterStats stats)
        {
            return skill.CooldownSeconds * (1.0 - StatFormulas.CooldownReduction(stats));
        }

        public static SkillUseResult TryUse(
            SkillDefinition skill, CharacterStats stats, Vitals vitals, Cooldown cooldown, double now)
        {
            if (vitals.IsDead) return SkillUseResult.CasterDead;
            if (!cooldown.IsReady(now)) return SkillUseResult.OnCooldown;
            if (!vitals.TrySpendSp(skill.SpCost)) return SkillUseResult.NotEnoughSp;

            cooldown.Start(now, EffectiveCooldown(skill, stats));
            return SkillUseResult.Success;
        }
    }

    public struct DamageResult
    {
        public int Amount;
        public bool IsCritical;
    }

    public static class DamageCalculator
    {
        /// <summary>
        /// Computes hit damage. <paramref name="critRoll"/> must be in [0,1) and is passed in
        /// so results are deterministic and testable (the server owns the random source).
        /// </summary>
        public static DamageResult Compute(
            CharacterStats attacker, int attackerLevel, SkillDefinition skill, float targetDefense, float critRoll)
        {
            float raw = StatFormulas.PhysicalAttack(attacker, attackerLevel) * skill.DamageMultiplier;
            // Defense reduces damage with diminishing returns: 100 defense halves damage.
            float mitigated = raw * 100f / (100f + Math.Max(0f, targetDefense));
            bool crit = critRoll < StatFormulas.CritChance(attacker);
            if (crit) mitigated *= StatFormulas.CritDamageMultiplier;

            return new DamageResult
            {
                Amount = Math.Max(1, (int)Math.Round(mitigated)),
                IsCritical = crit,
            };
        }
    }
}
