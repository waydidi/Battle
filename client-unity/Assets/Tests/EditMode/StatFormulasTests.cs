using KingdomsEightBanners.Core;
using NUnit.Framework;

namespace KingdomsEightBanners.Tests
{
    public class StatFormulasTests
    {
        private static CharacterStats WithAll(int value)
        {
            return new CharacterStats(value, value, value, value, value, value, value);
        }

        [Test]
        public void Stats_AreClampedToValidRange()
        {
            var stats = new CharacterStats(-5, 0, 5000, 1, 999, 1000, 50);
            Assert.AreEqual(CharacterStats.MinValue, stats.Str);
            Assert.AreEqual(CharacterStats.MinValue, stats.Agi);
            Assert.AreEqual(CharacterStats.MaxValue, stats.Vit);
            Assert.AreEqual(CharacterStats.MaxValue, stats.Sense);
            Assert.AreEqual(50, stats.Dex);
        }

        [Test]
        public void Diminishing_IsHalfCapAtHalfPoint_AndNeverExceedsCap()
        {
            Assert.AreEqual(0.5f, StatFormulas.Diminishing(100f, 1f, 100f), 1e-6f);
            Assert.Less(StatFormulas.Diminishing(1_000_000f, 1f, 100f), 1f);
            Assert.AreEqual(0f, StatFormulas.Diminishing(-10f, 1f, 100f));
        }

        [Test]
        public void RunSpeed_IncreasesWithAgi()
        {
            Assert.Greater(StatFormulas.RunSpeed(WithAll(80)), StatFormulas.RunSpeed(WithAll(10)));
        }

        [Test]
        public void SprintSpeed_NeverExceedsHardCap_EvenAtMaxStats()
        {
            var maxed = WithAll(CharacterStats.MaxValue);
            Assert.LessOrEqual(StatFormulas.SprintSpeed(maxed), StatFormulas.MaxSprintSpeed);
            Assert.LessOrEqual(StatFormulas.RunSpeed(maxed), StatFormulas.BaseRunSpeed * (1f + StatFormulas.MaxMoveSpeedBonus));
        }

        [Test]
        public void SprintIsFasterThanRun_RunIsFasterThanWalk()
        {
            var stats = WithAll(50);
            Assert.Greater(StatFormulas.SprintSpeed(stats), StatFormulas.RunSpeed(stats));
            Assert.Greater(StatFormulas.RunSpeed(stats), StatFormulas.WalkSpeed(stats));
        }

        [Test]
        public void CritChance_IsCapped()
        {
            var maxed = WithAll(CharacterStats.MaxValue);
            Assert.Less(StatFormulas.CritChance(maxed), StatFormulas.BaseCritChance + StatFormulas.MaxCritBonus);
            Assert.GreaterOrEqual(StatFormulas.CritChance(WithAll(1)), StatFormulas.BaseCritChance);
        }

        [Test]
        public void CooldownReduction_IsCapped()
        {
            Assert.Less(StatFormulas.CooldownReduction(WithAll(CharacterStats.MaxValue)), StatFormulas.MaxCooldownReduction);
        }

        [Test]
        public void SpCosts_StayPositive_AtMaxAgi()
        {
            var maxed = WithAll(CharacterStats.MaxValue);
            Assert.Greater(StatFormulas.SprintSpCostPerSecond(maxed), 0f);
            Assert.Greater(StatFormulas.DodgeSpCost(maxed), 0f);
            Assert.Greater(StatFormulas.DodgeCooldown(maxed), 0f);
        }

        [Test]
        public void MaxHp_GrowsWithVitAndLevel()
        {
            Assert.Greater(StatFormulas.MaxHp(WithAll(50), 10), StatFormulas.MaxHp(WithAll(10), 10));
            Assert.Greater(StatFormulas.MaxHp(WithAll(10), 20), StatFormulas.MaxHp(WithAll(10), 10));
        }
    }
}
