using KingdomsEightBanners.Core;
using NUnit.Framework;

namespace KingdomsEightBanners.Tests
{
    public class VitalsTests
    {
        [Test]
        public void TrySpendSp_FailsWithoutChangingSp_WhenNotEnough()
        {
            var vitals = new Vitals(100, 30);
            Assert.IsTrue(vitals.TrySpendSp(20f));
            Assert.IsFalse(vitals.TrySpendSp(20f));
            Assert.AreEqual(10f, vitals.Sp, 1e-4f);
        }

        [Test]
        public void TakeDamage_StopsAtZero_AndDeadCannotRegenerate()
        {
            var vitals = new Vitals(100, 30);
            Assert.AreEqual(100f, vitals.TakeDamage(250f), 1e-4f);
            Assert.IsTrue(vitals.IsDead);
            vitals.Regenerate(10f, 5f, 5f);
            Assert.AreEqual(0f, vitals.Hp);
        }

        [Test]
        public void Regenerate_DoesNotExceedMaximum()
        {
            var vitals = new Vitals(100, 50);
            vitals.TrySpendSp(10f);
            vitals.Regenerate(100f, 10f, 10f);
            Assert.AreEqual(50f, vitals.Sp, 1e-4f);
            Assert.AreEqual(100f, vitals.Hp, 1e-4f);
        }
    }

    public class ExperienceTrackTests
    {
        [Test]
        public void Add_LevelsUpAndCarriesOverflow()
        {
            var track = new ExperienceTrack(level => 100, maxLevel: 10);
            int gained = track.Add(250);
            Assert.AreEqual(2, gained);
            Assert.AreEqual(3, track.Level);
            Assert.AreEqual(50, track.Exp);
            Assert.AreEqual(0.5f, track.Fraction, 1e-6f);
        }

        [Test]
        public void Add_StopsAtMaxLevel()
        {
            var track = new ExperienceTrack(level => 10, maxLevel: 3);
            track.Add(1_000);
            Assert.AreEqual(3, track.Level);
            Assert.AreEqual(0, track.Exp);
            Assert.AreEqual(0, track.Add(500));
        }

        [Test]
        public void CharacterAndJobCurves_AreIndependent()
        {
            Assert.AreNotEqual(ExpCurves.Character(20), ExpCurves.Job(20));
            Assert.Greater(ExpCurves.Character(21), ExpCurves.Character(20));
        }

        [Test]
        public void StormTestCharacter_MatchesHudSpec()
        {
            var storm = CharacterSheet.CreateStormTestCharacter();
            Assert.AreEqual("Storm", storm.Name);
            Assert.AreEqual(68, storm.Level);
            Assert.AreEqual("warrior", storm.ClassId);
            Assert.AreEqual(storm.Vitals.MaxHp, storm.Vitals.Hp, 1e-4f);
            Assert.That(storm.CharacterLevel.Fraction, Is.InRange(0.41f, 0.43f));
        }

        [Test]
        public void GrantExp_LevelUpRaisesMaxHp()
        {
            var storm = CharacterSheet.CreateStormTestCharacter();
            int before = storm.Vitals.MaxHp;
            storm.GrantExp(storm.CharacterLevel.RequiredForNextLevel, 0);
            Assert.AreEqual(69, storm.Level);
            Assert.Greater(storm.Vitals.MaxHp, before);
        }
    }

    public class SkillRulesTests
    {
        private static CharacterStats Stats() => new CharacterStats(50, 50, 50, 10, 10, 10, 1);

        [Test]
        public void TryUse_SpendsSpAndStartsCooldown()
        {
            var skill = SkillDefinition.DesertSlash();
            var vitals = new Vitals(100, 100);
            var cooldown = new Cooldown();

            Assert.AreEqual(SkillUseResult.Success, SkillRules.TryUse(skill, Stats(), vitals, cooldown, now: 10.0));
            Assert.AreEqual(100f - skill.SpCost, vitals.Sp, 1e-4f);
            Assert.AreEqual(SkillUseResult.OnCooldown, SkillRules.TryUse(skill, Stats(), vitals, cooldown, now: 11.0));
            Assert.AreEqual(SkillUseResult.Success, SkillRules.TryUse(skill, Stats(), vitals, cooldown, now: 10.0 + skill.CooldownSeconds));
        }

        [Test]
        public void TryUse_FailsWithoutSp_AndDoesNotStartCooldown()
        {
            var skill = SkillDefinition.DesertSlash();
            var vitals = new Vitals(100, 5);
            var cooldown = new Cooldown();

            Assert.AreEqual(SkillUseResult.NotEnoughSp, SkillRules.TryUse(skill, Stats(), vitals, cooldown, 0.0));
            Assert.IsTrue(cooldown.IsReady(0.0));
        }

        [Test]
        public void TryUse_FailsWhenDead()
        {
            var vitals = new Vitals(100, 100);
            vitals.TakeDamage(100f);
            Assert.AreEqual(SkillUseResult.CasterDead,
                SkillRules.TryUse(SkillDefinition.BasicAttack(), Stats(), vitals, new Cooldown(), 0.0));
        }

        [Test]
        public void Damage_CritRollAboveChance_IsNotCritical_AndDefenseReducesDamage()
        {
            var stats = Stats();
            var skill = SkillDefinition.BasicAttack();
            var noDefense = DamageCalculator.Compute(stats, 10, skill, 0f, critRoll: 0.99f);
            var withDefense = DamageCalculator.Compute(stats, 10, skill, 100f, critRoll: 0.99f);
            var crit = DamageCalculator.Compute(stats, 10, skill, 0f, critRoll: 0.0f);

            Assert.IsFalse(noDefense.IsCritical);
            Assert.AreEqual(noDefense.Amount / 2f, withDefense.Amount, 1f);
            Assert.IsTrue(crit.IsCritical);
            Assert.Greater(crit.Amount, noDefense.Amount);
        }
    }
}
