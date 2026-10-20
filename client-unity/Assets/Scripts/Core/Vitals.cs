using System;

namespace KingdomsEightBanners.Core
{
    /// <summary>
    /// Current and maximum HP (red bar) and SP (blue bar).
    /// SP is the stamina/skill resource spent by sprinting, dodging and skills.
    /// </summary>
    [Serializable]
    public sealed class Vitals
    {
        public int MaxHp { get; private set; }
        public int MaxSp { get; private set; }
        public float Hp { get; private set; }
        public float Sp { get; private set; }

        public bool IsDead => Hp <= 0f;
        public float HpFraction => MaxHp > 0 ? Hp / MaxHp : 0f;
        public float SpFraction => MaxSp > 0 ? Sp / MaxSp : 0f;

        public Vitals(int maxHp, int maxSp)
        {
            SetMaximums(maxHp, maxSp);
            Hp = MaxHp;
            Sp = MaxSp;
        }

        /// <summary>Changes the maximums (e.g. after a level up) and clamps current values.</summary>
        public void SetMaximums(int maxHp, int maxSp)
        {
            if (maxHp < 1) throw new ArgumentOutOfRangeException(nameof(maxHp));
            if (maxSp < 0) throw new ArgumentOutOfRangeException(nameof(maxSp));
            MaxHp = maxHp;
            MaxSp = maxSp;
            Hp = Math.Min(Hp, MaxHp);
            Sp = Math.Min(Sp, MaxSp);
        }

        /// <summary>Spends SP only if the full amount is available.</summary>
        public bool TrySpendSp(float amount)
        {
            if (amount < 0f) throw new ArgumentOutOfRangeException(nameof(amount));
            if (IsDead || Sp < amount) return false;
            Sp -= amount;
            return true;
        }

        /// <summary>Applies damage and returns how much HP was actually removed.</summary>
        public float TakeDamage(float amount)
        {
            if (amount <= 0f || IsDead) return 0f;
            float applied = Math.Min(amount, Hp);
            Hp -= applied;
            return applied;
        }

        public float Heal(float amount)
        {
            if (amount <= 0f || IsDead) return 0f;
            float applied = Math.Min(amount, MaxHp - Hp);
            Hp += applied;
            return applied;
        }

        public void Regenerate(float deltaSeconds, float hpPerSecond, float spPerSecond)
        {
            if (deltaSeconds <= 0f || IsDead) return;
            Hp = Math.Min(MaxHp, Hp + hpPerSecond * deltaSeconds);
            Sp = Math.Min(MaxSp, Sp + spPerSecond * deltaSeconds);
        }

        public void RestoreFull()
        {
            Hp = MaxHp;
            Sp = MaxSp;
        }
    }
}
