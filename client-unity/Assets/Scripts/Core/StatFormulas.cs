using System;

namespace KingdomsEightBanners.Core
{
    /// <summary>
    /// Every derived number the game uses (speed, HP, SP, crit, cooldown reduction...)
    /// is computed here from saved stats. Clients may display these values but the
    /// authoritative server must compute them itself and never trust a client-sent value.
    ///
    /// Bonuses use diminishing returns: bonus = cap * score / (score + halfPoint).
    /// The bonus reaches half of its cap when score == halfPoint and can never exceed
    /// the cap no matter how many points are invested. All caps are documented in
    /// docs/CLASSES_AND_SKILLS.md.
    /// </summary>
    public static class StatFormulas
    {
        // ---- Movement -----------------------------------------------------------
        public const float BaseWalkSpeed = 2.2f;          // metres per second
        public const float BaseRunSpeed = 5.0f;
        public const float SprintMultiplier = 1.6f;
        public const float MaxMoveSpeedBonus = 0.25f;     // +25% run speed at most
        public const float MoveSpeedHalfPoint = 150f;
        /// <summary>Absolute ceiling for any run/sprint speed, buffs included.</summary>
        public const float MaxSprintSpeed = BaseRunSpeed * (1f + MaxMoveSpeedBonus) * SprintMultiplier;

        // ---- Stamina (SP) costs -------------------------------------------------
        public const float BaseSprintSpCostPerSecond = 8f;
        public const float MaxSprintEfficiency = 0.40f;   // sprint costs at most 40% less
        public const float SprintEfficiencyHalfPoint = 120f;
        public const float BaseDodgeSpCost = 20f;

        // ---- Dodge --------------------------------------------------------------
        public const float BaseDodgeCooldown = 0.8f;      // seconds
        public const float MaxDodgeCooldownReduction = 0.30f;
        public const float DodgeCooldownHalfPoint = 120f;
        public const float DodgeDistance = 4.5f;          // metres
        public const float DodgeDuration = 0.35f;         // seconds

        // ---- Combat -------------------------------------------------------------
        public const float BaseCritChance = 0.05f;
        public const float MaxCritBonus = 0.30f;          // total crit chance capped at 35%
        public const float CritHalfPoint = 150f;
        public const float CritDamageMultiplier = 1.5f;
        public const float MaxCooldownReduction = 0.25f;
        public const float CooldownReductionHalfPoint = 200f;

        // ---- Regeneration -------------------------------------------------------
        public const float BaseSpRegenPerSecond = 10f;
        public const float BaseHpRegenPerSecond = 1f;

        /// <summary>cap * score / (score + halfPoint), with score clamped at zero.</summary>
        public static float Diminishing(float score, float cap, float halfPoint)
        {
            if (halfPoint <= 0f) throw new ArgumentOutOfRangeException(nameof(halfPoint));
            if (score <= 0f) return 0f;
            return cap * score / (score + halfPoint);
        }

        /// <summary>AGI contributes fully and DEX at half weight towards movement.</summary>
        public static float MoveSpeedBonus(CharacterStats stats)
        {
            float score = stats.Agi + 0.5f * stats.Dex;
            return Diminishing(score, MaxMoveSpeedBonus, MoveSpeedHalfPoint);
        }

        public static float WalkSpeed(CharacterStats stats)
        {
            return BaseWalkSpeed * (1f + MoveSpeedBonus(stats));
        }

        public static float RunSpeed(CharacterStats stats)
        {
            return Math.Min(BaseRunSpeed * (1f + MoveSpeedBonus(stats)), MaxSprintSpeed);
        }

        public static float SprintSpeed(CharacterStats stats)
        {
            return Math.Min(RunSpeed(stats) * SprintMultiplier, MaxSprintSpeed);
        }

        public static float SprintSpCostPerSecond(CharacterStats stats)
        {
            float efficiency = Diminishing(stats.Agi, MaxSprintEfficiency, SprintEfficiencyHalfPoint);
            return BaseSprintSpCostPerSecond * (1f - efficiency);
        }

        public static float DodgeSpCost(CharacterStats stats)
        {
            float efficiency = Diminishing(stats.Agi, MaxSprintEfficiency, SprintEfficiencyHalfPoint);
            return BaseDodgeSpCost * (1f - efficiency);
        }

        public static float DodgeCooldown(CharacterStats stats)
        {
            float reduction = Diminishing(stats.Dex, MaxDodgeCooldownReduction, DodgeCooldownHalfPoint);
            return BaseDodgeCooldown * (1f - reduction);
        }

        public static int MaxHp(CharacterStats stats, int level)
        {
            return 100 + Math.Max(level, 1) * 20 + stats.Vit * 12;
        }

        public static int MaxSp(CharacterStats stats, int level)
        {
            return 50 + Math.Max(level, 1) * 4 + stats.Element * 2 + stats.Sense;
        }

        public static float PhysicalAttack(CharacterStats stats, int level)
        {
            return stats.Str * 2f + stats.Dex * 0.5f + Math.Max(level, 1);
        }

        public static float CritChance(CharacterStats stats)
        {
            return BaseCritChance + Diminishing(stats.Luck, MaxCritBonus, CritHalfPoint);
        }

        /// <summary>Fraction (0..MaxCooldownReduction) removed from skill cooldowns.</summary>
        public static float CooldownReduction(CharacterStats stats)
        {
            return Diminishing(stats.Dex, MaxCooldownReduction, CooldownReductionHalfPoint);
        }

        public static float SpRegenPerSecond(CharacterStats stats)
        {
            return BaseSpRegenPerSecond + stats.Vit * 0.05f;
        }

        public static float HpRegenPerSecond(CharacterStats stats)
        {
            return BaseHpRegenPerSecond + stats.Vit * 0.1f;
        }
    }
}
