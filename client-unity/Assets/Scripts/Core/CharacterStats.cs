using System;

namespace KingdomsEightBanners.Core
{
    /// <summary>The seven primary stats. See docs/CLASSES_AND_SKILLS.md.</summary>
    public enum StatType
    {
        Str,
        Agi,
        Vit,
        Luck,
        Element,
        Sense,
        Dex,
    }

    /// <summary>
    /// Plain data holder for the seven primary stats. Contains no Unity code so the
    /// same class can run inside the client, the future server and plain .NET tests.
    /// Values are always clamped to [MinValue, MaxValue].
    /// </summary>
    [Serializable]
    public sealed class CharacterStats
    {
        public const int MinValue = 1;
        public const int MaxValue = 999;

        public static readonly int Count = Enum.GetValues(typeof(StatType)).Length;

        private readonly int[] _values = new int[Count];

        public CharacterStats()
        {
            for (int i = 0; i < Count; i++)
            {
                _values[i] = MinValue;
            }
        }

        public CharacterStats(int str, int agi, int vit, int luck, int element, int sense, int dex)
        {
            Set(StatType.Str, str);
            Set(StatType.Agi, agi);
            Set(StatType.Vit, vit);
            Set(StatType.Luck, luck);
            Set(StatType.Element, element);
            Set(StatType.Sense, sense);
            Set(StatType.Dex, dex);
        }

        public int this[StatType stat] => Get(stat);

        public int Str => Get(StatType.Str);
        public int Agi => Get(StatType.Agi);
        public int Vit => Get(StatType.Vit);
        public int Luck => Get(StatType.Luck);
        public int Element => Get(StatType.Element);
        public int Sense => Get(StatType.Sense);
        public int Dex => Get(StatType.Dex);

        public int Get(StatType stat) => _values[(int)stat];

        public void Set(StatType stat, int value)
        {
            _values[(int)stat] = Clamp(value);
        }

        public CharacterStats Clone()
        {
            var copy = new CharacterStats();
            Array.Copy(_values, copy._values, Count);
            return copy;
        }

        private static int Clamp(int value)
        {
            if (value < MinValue) return MinValue;
            if (value > MaxValue) return MaxValue;
            return value;
        }
    }
}
