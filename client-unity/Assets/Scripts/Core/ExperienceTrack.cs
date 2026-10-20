using System;

namespace KingdomsEightBanners.Core
{
    /// <summary>EXP required to go from a level to the next one.</summary>
    public delegate long ExpCurve(int level);

    public static class ExpCurves
    {
        public const int MaxCharacterLevel = 99;
        public const int MaxJobLevel = 70;

        /// <summary>Character level curve (yellow bar). Tuning target, not final.</summary>
        public static long Character(int level)
        {
            return (long)Math.Round(100.0 * Math.Pow(Math.Max(level, 1), 2.2));
        }

        /// <summary>Job level curve (violet bar). Separate from character level.</summary>
        public static long Job(int level)
        {
            return (long)Math.Round(80.0 * Math.Pow(Math.Max(level, 1), 1.9));
        }
    }

    /// <summary>
    /// One level + EXP progress bar. Characters own two independent tracks:
    /// character level and job level.
    /// </summary>
    [Serializable]
    public sealed class ExperienceTrack
    {
        private readonly ExpCurve _curve;

        public int Level { get; private set; }
        public int MaxLevel { get; }
        /// <summary>EXP collected towards the next level.</summary>
        public long Exp { get; private set; }

        public bool IsMaxLevel => Level >= MaxLevel;
        public long RequiredForNextLevel => IsMaxLevel ? 0 : _curve(Level);
        public float Fraction => IsMaxLevel ? 1f : (float)Exp / RequiredForNextLevel;

        public ExperienceTrack(ExpCurve curve, int maxLevel, int level = 1, long exp = 0)
        {
            _curve = curve ?? throw new ArgumentNullException(nameof(curve));
            if (maxLevel < 1) throw new ArgumentOutOfRangeException(nameof(maxLevel));
            MaxLevel = maxLevel;
            Level = Math.Max(1, Math.Min(level, maxLevel));
            Exp = 0;
            Add(Math.Max(0, exp));
        }

        /// <summary>Adds EXP, applying any level ups. Returns the number of levels gained.</summary>
        public int Add(long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (IsMaxLevel) return 0;

            int gained = 0;
            Exp += amount;
            while (!IsMaxLevel && Exp >= RequiredForNextLevel)
            {
                Exp -= RequiredForNextLevel;
                Level++;
                gained++;
            }

            if (IsMaxLevel) Exp = 0;
            return gained;
        }
    }
}
