using System.Collections.Generic;
using KingdomsEightBanners.Core;
using NUnit.Framework;

namespace KingdomsEightBanners.Tests
{
    public class SkillTreeTests
    {
        private const int StormLevel = 68;

        [Test]
        public void DesertVanguard_HasThreeBranchesOfFiveUniqueSkills()
        {
            var tree = SkillTrees.DesertVanguard();
            Assert.AreEqual(3, tree.Branches.Length);
            var ids = new HashSet<string>();
            foreach (var branch in tree.Branches)
            {
                Assert.AreEqual(5, branch.Nodes.Length, branch.Id);
                foreach (var node in branch.Nodes) Assert.IsTrue(ids.Add(node.Id), "duplicate " + node.Id);
            }
        }

        [Test]
        public void DesertVanguard_PrerequisitesExistAndStayInsideTheirBranch()
        {
            var tree = SkillTrees.DesertVanguard();
            foreach (var branch in tree.Branches)
            {
                var branchIds = new HashSet<string>();
                foreach (var node in branch.Nodes) branchIds.Add(node.Id);
                foreach (var node in branch.Nodes)
                {
                    if (node.PrerequisiteId == null) continue;
                    Assert.IsTrue(branchIds.Contains(node.PrerequisiteId), node.Id);
                    Assert.LessOrEqual(node.PrerequisiteLevel, tree.Find(node.PrerequisiteId).MaxLevel, node.Id);
                }
            }
        }

        [Test]
        public void StormProgress_MatchesMockupLocks()
        {
            var state = SkillTrees.StormTestProgress();
            var tree = state.Tree;
            Assert.AreEqual(3, state.AvailableSkillPoints);

            string[] locked = { "sandstorm_aura", "mirage_strike", "caravan_bulwark", "phalanx_wall", "solar_ascension", "blazing_domain" };
            foreach (var id in locked) Assert.IsFalse(state.IsUnlocked(tree.Find(id), StormLevel), id);
            Assert.IsTrue(state.IsUnlocked(tree.Find("sunfire_cleave"), StormLevel));
        }

        [Test]
        public void SunfireCleave_NextLevelPreviewMatchesMockup()
        {
            var cleave = SkillTrees.DesertVanguard().Find("sunfire_cleave");
            Assert.AreEqual(2, cleave.CostPerLevel);
            Assert.AreEqual(18f, cleave.Effects[0].ValueAt(2), 1e-4f);
            Assert.AreEqual(26f, cleave.Effects[0].ValueAt(3), 1e-4f);
            Assert.AreEqual(2f, cleave.Effects[1].ValueAt(2), 1e-4f);
            Assert.AreEqual(3f, cleave.Effects[1].ValueAt(3), 1e-4f);
        }

        [Test]
        public void TryLevelUp_SpendsSkillPoints()
        {
            var state = SkillTrees.StormTestProgress();
            Assert.AreEqual(SkillLevelUpResult.Success, state.TryLevelUp("sunfire_cleave", StormLevel));
            Assert.AreEqual(3, state.LevelOf("sunfire_cleave"));
            Assert.AreEqual(1, state.AvailableSkillPoints);
            Assert.AreEqual(SkillLevelUpResult.NotEnoughSkillPoints, state.TryLevelUp("sunfire_cleave", StormLevel));
            Assert.AreEqual(3, state.LevelOf("sunfire_cleave"));
        }

        [Test]
        public void TryLevelUp_RejectsLockedMaxedAndUnknown()
        {
            var state = SkillTrees.StormTestProgress();
            state.GrantSkillPoints(100);
            Assert.AreEqual(SkillLevelUpResult.PrerequisiteNotMet, state.TryLevelUp("mirage_strike", StormLevel));
            Assert.AreEqual(SkillLevelUpResult.CharacterLevelTooLow, state.TryLevelUp("caravan_bulwark", StormLevel));
            Assert.AreEqual(SkillLevelUpResult.UnknownSkill, state.TryLevelUp("not_a_skill", StormLevel));

            Assert.AreEqual(SkillLevelUpResult.Success, state.TryLevelUp("sunfire_strike", StormLevel));
            Assert.AreEqual(SkillLevelUpResult.AlreadyMaxLevel, state.TryLevelUp("sunfire_strike", StormLevel));
        }

        [Test]
        public void LevellingPrerequisite_UnlocksNextSkill()
        {
            var state = SkillTrees.StormTestProgress();
            state.GrantSkillPoints(10);
            Assert.AreEqual(SkillLevelUpResult.Success, state.TryLevelUp("whirlwind_cut", StormLevel));
            Assert.IsTrue(state.IsUnlocked(state.Tree.Find("mirage_strike"), StormLevel));
        }

        [Test]
        public void ResetAll_RefundsEverySpentPoint()
        {
            var state = new SkillTreeState(SkillTrees.DesertVanguard(), 10);
            state.TryLevelUp("desert_slash", StormLevel);   // 1
            state.TryLevelUp("desert_slash", StormLevel);   // 1
            state.TryLevelUp("whirlwind_cut", StormLevel);  // 2
            Assert.AreEqual(6, state.AvailableSkillPoints);
            Assert.AreEqual(4, state.ResetAll());
            Assert.AreEqual(10, state.AvailableSkillPoints);
            Assert.AreEqual(0, state.LevelOf("desert_slash"));
        }
    }
}
