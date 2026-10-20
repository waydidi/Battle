using System;

namespace KingdomsEightBanners.Core
{
    /// <summary>
    /// Everything that describes one character's progression. In the offline prototype the
    /// client owns this object; in multiplayer it is loaded from and saved by the server.
    /// </summary>
    [Serializable]
    public sealed class CharacterSheet
    {
        public string Name { get; }
        public string ClassId { get; }
        public string Origin { get; }
        public CharacterStats Stats { get; }
        public ExperienceTrack CharacterLevel { get; }
        public ExperienceTrack JobLevel { get; }
        public Vitals Vitals { get; }

        public CharacterSheet(
            string name, string classId, string origin, CharacterStats stats,
            ExperienceTrack characterLevel, ExperienceTrack jobLevel)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            ClassId = classId ?? throw new ArgumentNullException(nameof(classId));
            Origin = origin ?? throw new ArgumentNullException(nameof(origin));
            Stats = stats ?? throw new ArgumentNullException(nameof(stats));
            CharacterLevel = characterLevel ?? throw new ArgumentNullException(nameof(characterLevel));
            JobLevel = jobLevel ?? throw new ArgumentNullException(nameof(jobLevel));
            Vitals = new Vitals(
                StatFormulas.MaxHp(Stats, CharacterLevel.Level),
                StatFormulas.MaxSp(Stats, CharacterLevel.Level));
        }

        public int Level => CharacterLevel.Level;

        /// <summary>Grants EXP to both tracks and refreshes HP/SP maximums on level up.</summary>
        public void GrantExp(long characterExp, long jobExp)
        {
            int gained = CharacterLevel.Add(characterExp);
            JobLevel.Add(jobExp);
            if (gained > 0)
            {
                Vitals.SetMaximums(StatFormulas.MaxHp(Stats, Level), StatFormulas.MaxSp(Stats, Level));
                Vitals.RestoreFull();
            }
        }

        /// <summary>The Level 68 male Warrior "Storm" used for prototype testing.</summary>
        public static CharacterSheet CreateStormTestCharacter()
        {
            var stats = new CharacterStats(str: 90, agi: 75, vit: 70, luck: 30, element: 40, sense: 45, dex: 60);
            var characterLevel = new ExperienceTrack(ExpCurves.Character, ExpCurves.MaxCharacterLevel, 68);
            characterLevel.Add(characterLevel.RequiredForNextLevel * 42 / 100);
            var jobLevel = new ExperienceTrack(ExpCurves.Job, ExpCurves.MaxJobLevel, 41);
            jobLevel.Add(jobLevel.RequiredForNextLevel * 67 / 100);
            return new CharacterSheet("Storm", "warrior", "sunspire", stats, characterLevel, jobLevel);
        }
    }
}
