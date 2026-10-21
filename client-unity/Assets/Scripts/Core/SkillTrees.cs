namespace KingdomsEightBanners.Core
{
    /// <summary>
    /// Built-in skill tree data. Later this moves to versioned server data / ScriptableObjects,
    /// but keeping it in code for now makes it testable and identical on client and server.
    /// </summary>
    public static class SkillTrees
    {
        public const string DesertVanguardId = "warrior_sunspire_desert_vanguard";

        /// <summary>
        /// Warrior - Sunspire City skill tree (city-born specialization: Desert Vanguard).
        /// Three branches of five skills. Costs: top 1, middle 2, bottom 3 Skill Points per level.
        /// </summary>
        public static SkillTree DesertVanguard()
        {
            return new SkillTree
            {
                Id = DesertVanguardId,
                ClassId = "warrior",
                Origin = "sunspire",
                Title = "Warrior - Sunspire City Skill Tree",
                Specialization = "Desert Vanguard",
                Branches = new[]
                {
                    new SkillBranch
                    {
                        Id = "sand_blade",
                        DisplayName = "Sand Blade",
                        Description = "Dueling techniques infused with desert winds.",
                        Nodes = new[]
                        {
                            Node("desert_slash", "Desert Slash", 5, 1, 1, null, 0,
                                "A fast forward slash that kicks up stinging sand.",
                                new[] { "Active", "Melee" },
                                new SkillEffect("Damage", 140f, 20f, "%")),
                            Node("sand_step", "Sand Step", 3, 1, 20, "desert_slash", 1,
                                "A short sand-cloaked dash that resets your guard.",
                                new[] { "Active", "Mobility" },
                                new SkillEffect("Dash distance", 3f, 0.5f, " m"),
                                new SkillEffect("Cooldown", 9f, -1f, " s")),
                            Node("whirlwind_cut", "Whirlwind Cut", 5, 2, 25, "desert_slash", 2,
                                "Spin with your blade, striking every enemy around you.",
                                new[] { "Active", "AoE" },
                                new SkillEffect("Damage", 90f, 15f, "%")),
                            Node("sandstorm_aura", "Sandstorm Aura", 3, 3, 45, "sand_step", 3,
                                "A swirling aura that slows nearby enemies.",
                                new[] { "Active", "Aura" },
                                new SkillEffect("Enemy slow", 10f, 5f, "%"),
                                new SkillEffect("Duration", 6f, 2f, " s")),
                            Node("mirage_strike", "Mirage Strike", 5, 3, 50, "whirlwind_cut", 3,
                                "Leave a mirage behind and strike from the side.",
                                new[] { "Active", "Melee" },
                                new SkillEffect("Damage", 180f, 25f, "%")),
                        },
                    },
                    new SkillBranch
                    {
                        Id = "caravan_guard",
                        DisplayName = "Caravan Guard",
                        Description = "Unwavering defense to protect the city and its people.",
                        Nodes = new[]
                        {
                            Node("guard_stance", "Guard Stance", 5, 1, 1, null, 0,
                                "Raise your guard, reducing incoming damage.",
                                new[] { "Active", "Defense" },
                                new SkillEffect("Damage reduction", 15f, 5f, "%")),
                            Node("shield_bash", "Shield Bash", 3, 1, 20, "guard_stance", 1,
                                "Bash an enemy, interrupting and staggering it.",
                                new[] { "Active", "Stagger" },
                                new SkillEffect("Stagger", 0.6f, 0.2f, " s")),
                            Node("caravan_bulwark", "Caravan Bulwark", 5, 2, 70, "guard_stance", 3,
                                "Awakened defense: share part of your guard with allies.",
                                new[] { "Passive", "Party" },
                                new SkillEffect("Ally damage reduction", 4f, 2f, "%")),
                            Node("desert_charge", "Desert Charge", 5, 3, 40, "shield_bash", 1,
                                "Charge forward, knocking enemies aside.",
                                new[] { "Active", "Mobility" },
                                new SkillEffect("Damage", 120f, 15f, "%"),
                                new SkillEffect("Charge distance", 6f, 0.5f, " m")),
                            Node("phalanx_wall", "Phalanx Wall", 3, 3, 70, "caravan_bulwark", 1,
                                "Plant your shield and become an unbreakable wall.",
                                new[] { "Active", "Defense" },
                                new SkillEffect("Block duration", 2f, 0.5f, " s")),
                        },
                    },
                    new SkillBranch
                    {
                        Id = "sunfire_arts",
                        DisplayName = "Sunfire Combat Arts",
                        Description = "Channel the blessing of the Sun God into devastating power.",
                        Nodes = new[]
                        {
                            Node("sunfire_strike", "Sunfire Strike", 5, 1, 10, null, 0,
                                "An overhead strike wreathed in solar fire.",
                                new[] { "Active", "Fire" },
                                new SkillEffect("Damage", 160f, 20f, "%")),
                            Node("spear_throw", "Spear Throw", 3, 1, 30, "sunfire_strike", 2,
                                "Hurl a burning spear at a distant enemy.",
                                new[] { "Active", "Ranged" },
                                new SkillEffect("Damage", 110f, 20f, "%"),
                                new SkillEffect("Range", 12f, 2f, " m")),
                            Node("sunfire_cleave", "Sunfire Cleave", 5, 2, 35, "sunfire_strike", 3,
                                "Unleash a mighty arc of sunfire, dealing heavy damage to nearby enemies and inflicting Burn.",
                                new[] { "Active", "AoE" },
                                new SkillEffect("Damage", 2f, 8f, "%"),
                                new SkillEffect("Burn duration", 0f, 1f, " s")),
                            Node("solar_ascension", "Solar Ascension", 3, 3, 60, "spear_throw", 3,
                                "Leap skyward and crash down in a burst of light.",
                                new[] { "Active", "AoE" },
                                new SkillEffect("Damage", 220f, 30f, "%")),
                            Node("blazing_domain", "Blazing Domain", 5, 3, 70, "sunfire_cleave", 3,
                                "Ultimate: the ground becomes a sea of sunfire.",
                                new[] { "Active", "Ultimate" },
                                new SkillEffect("Damage per second", 40f, 10f, "%"),
                                new SkillEffect("Duration", 5f, 1f, " s")),
                        },
                    },
                },
            };
        }

        /// <summary>Storm's Lv.68 test progress, matching the UI mockup.</summary>
        public static SkillTreeState StormTestProgress()
        {
            var state = new SkillTreeState(DesertVanguard(), availableSkillPoints: 3);
            state.SetLevel("desert_slash", 3);
            state.SetLevel("sand_step", 1);
            state.SetLevel("whirlwind_cut", 2);
            state.SetLevel("guard_stance", 3);
            state.SetLevel("shield_bash", 1);
            state.SetLevel("desert_charge", 2);
            state.SetLevel("sunfire_strike", 4);
            state.SetLevel("spear_throw", 1);
            state.SetLevel("sunfire_cleave", 2);
            return state;
        }

        private static SkillNode Node(
            string id, string name, int maxLevel, int cost, int requiredLevel,
            string prerequisiteId, int prerequisiteLevel, string description, string[] tags,
            params SkillEffect[] effects)
        {
            return new SkillNode
            {
                Id = id,
                DisplayName = name,
                Description = description,
                MaxLevel = maxLevel,
                CostPerLevel = cost,
                RequiredCharacterLevel = requiredLevel,
                PrerequisiteId = prerequisiteId,
                PrerequisiteLevel = prerequisiteLevel,
                IsPassive = System.Array.IndexOf(tags, "Passive") >= 0,
                Tags = tags,
                Effects = effects,
            };
        }
    }
}
