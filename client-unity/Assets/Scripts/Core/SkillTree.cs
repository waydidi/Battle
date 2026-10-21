using System;
using System.Collections.Generic;

namespace KingdomsEightBanners.Core
{
    /// <summary>
    /// One numeric line in a skill's description, e.g. "Damage +18%".
    /// Value at level L is Base + PerLevel * L, so the UI can preview the next level.
    /// </summary>
    [Serializable]
    public sealed class SkillEffect
    {
        public string Label;
        public float Base;
        public float PerLevel;
        public string Unit;

        public SkillEffect(string label, float @base, float perLevel, string unit)
        {
            Label = label;
            Base = @base;
            PerLevel = perLevel;
            Unit = unit;
        }

        public float ValueAt(int level) => level <= 0 ? 0f : Base + PerLevel * level;
    }

    /// <summary>A node in a class skill tree.</summary>
    [Serializable]
    public sealed class SkillNode
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public int MaxLevel;
        /// <summary>Skill Points spent per level. Skill Points are NOT the blue SP (stamina) bar.</summary>
        public int CostPerLevel;
        public int RequiredCharacterLevel;
        /// <summary>Id of the node that must reach <see cref="PrerequisiteLevel"/> first, or null.</summary>
        public string PrerequisiteId;
        public int PrerequisiteLevel;
        public bool IsPassive;
        public string[] Tags = Array.Empty<string>();
        public SkillEffect[] Effects = Array.Empty<SkillEffect>();
    }

    [Serializable]
    public sealed class SkillBranch
    {
        public string Id;
        public string DisplayName;
        public string Description;
        /// <summary>Layout order: [0] top, [1] middle-left, [2] middle-right, [3] bottom-left, [4] bottom-right.</summary>
        public SkillNode[] Nodes;
    }

    [Serializable]
    public sealed class SkillTree
    {
        public string Id;
        public string ClassId;
        public string Origin;
        public string Title;
        public string Specialization;
        public SkillBranch[] Branches;

        private Dictionary<string, SkillNode> _byId;

        public SkillNode Find(string id)
        {
            if (_byId == null)
            {
                _byId = new Dictionary<string, SkillNode>();
                foreach (var branch in Branches)
                {
                    foreach (var node in branch.Nodes) _byId[node.Id] = node;
                }
            }

            return id != null && _byId.TryGetValue(id, out var found) ? found : null;
        }

        public IEnumerable<SkillNode> AllNodes()
        {
            foreach (var branch in Branches)
            {
                foreach (var node in branch.Nodes) yield return node;
            }
        }
    }

    public enum SkillLevelUpResult
    {
        Success,
        UnknownSkill,
        AlreadyMaxLevel,
        CharacterLevelTooLow,
        PrerequisiteNotMet,
        NotEnoughSkillPoints,
    }

    /// <summary>
    /// A character's progress in one skill tree: learned levels and unspent Skill Points.
    /// The client may call CanLevelUp to grey out buttons, but in multiplayer only the
    /// server's TryLevelUp counts.
    /// </summary>
    [Serializable]
    public sealed class SkillTreeState
    {
        private readonly Dictionary<string, int> _levels = new Dictionary<string, int>();

        public SkillTree Tree { get; }
        public int AvailableSkillPoints { get; private set; }

        public SkillTreeState(SkillTree tree, int availableSkillPoints)
        {
            Tree = tree ?? throw new ArgumentNullException(nameof(tree));
            if (availableSkillPoints < 0) throw new ArgumentOutOfRangeException(nameof(availableSkillPoints));
            AvailableSkillPoints = availableSkillPoints;
        }

        public int LevelOf(string skillId) => _levels.TryGetValue(skillId, out var level) ? level : 0;

        public void GrantSkillPoints(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            AvailableSkillPoints += amount;
        }

        /// <summary>True when prerequisites and level requirement allow learning the skill at all.</summary>
        public bool IsUnlocked(SkillNode node, int characterLevel)
        {
            if (characterLevel < node.RequiredCharacterLevel) return false;
            return node.PrerequisiteId == null || LevelOf(node.PrerequisiteId) >= node.PrerequisiteLevel;
        }

        public SkillLevelUpResult CanLevelUp(string skillId, int characterLevel)
        {
            var node = Tree.Find(skillId);
            if (node == null) return SkillLevelUpResult.UnknownSkill;
            if (LevelOf(skillId) >= node.MaxLevel) return SkillLevelUpResult.AlreadyMaxLevel;
            if (characterLevel < node.RequiredCharacterLevel) return SkillLevelUpResult.CharacterLevelTooLow;
            if (node.PrerequisiteId != null && LevelOf(node.PrerequisiteId) < node.PrerequisiteLevel)
                return SkillLevelUpResult.PrerequisiteNotMet;
            if (AvailableSkillPoints < node.CostPerLevel) return SkillLevelUpResult.NotEnoughSkillPoints;
            return SkillLevelUpResult.Success;
        }

        public SkillLevelUpResult TryLevelUp(string skillId, int characterLevel)
        {
            var result = CanLevelUp(skillId, characterLevel);
            if (result != SkillLevelUpResult.Success) return result;

            AvailableSkillPoints -= Tree.Find(skillId).CostPerLevel;
            _levels[skillId] = LevelOf(skillId) + 1;
            return SkillLevelUpResult.Success;
        }

        /// <summary>Sets a level directly, used when loading saved data. Does not touch Skill Points.</summary>
        public void SetLevel(string skillId, int level)
        {
            var node = Tree.Find(skillId) ?? throw new ArgumentException("Unknown skill " + skillId, nameof(skillId));
            if (level < 0 || level > node.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            _levels[skillId] = level;
        }

        /// <summary>Respec: forgets every skill and refunds all Skill Points spent. Fees/cooldowns are applied by the caller.</summary>
        public int ResetAll()
        {
            int refunded = 0;
            foreach (var pair in _levels)
            {
                refunded += pair.Value * Tree.Find(pair.Key).CostPerLevel;
            }

            _levels.Clear();
            AvailableSkillPoints += refunded;
            return refunded;
        }
    }
}
