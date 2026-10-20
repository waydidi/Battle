using System.Collections.Generic;
using KingdomsEightBanners.Character;
using KingdomsEightBanners.Core;
using UnityEngine;

namespace KingdomsEightBanners.Combat
{
    /// <summary>
    /// Light attack (left mouse) and the first Warrior skill, Desert Slash (key 1).
    /// Cooldowns and SP costs go through Core.SkillRules, the same rules the server will run.
    /// OFFLINE PROTOTYPE: hits are resolved locally; in multiplayer the client only sends
    /// "I pressed skill X facing Y" and the server decides what was hit.
    /// </summary>
    [RequireComponent(typeof(PlayerCharacter))]
    [RequireComponent(typeof(PlayerInputReader))]
    [RequireComponent(typeof(ThirdPersonController))]
    public sealed class PlayerCombat : MonoBehaviour
    {
        public sealed class SkillSlot
        {
            public SkillDefinition Skill;
            public Cooldown Cooldown = new Cooldown();
            /// <summary>Half-angle of the hit cone in degrees.</summary>
            public float HalfArc;
        }

        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField] private Transform cameraTransform;

        private readonly Collider[] _overlap = new Collider[32];
        private readonly HashSet<Damageable> _alreadyHit = new HashSet<Damageable>();

        private PlayerCharacter _character;
        private PlayerInputReader _input;
        private ThirdPersonController _movement;

        public SkillSlot BasicAttack { get; private set; }
        /// <summary>Skill bar slots for keys 1-6. Empty slots are null.</summary>
        public SkillSlot[] Skills { get; } = new SkillSlot[PlayerInputReader.SkillSlotCount];
        /// <summary>Short feedback for the HUD, e.g. "Not enough SP".</summary>
        public string LastMessage { get; private set; } = "";
        public float LastMessageTime { get; private set; } = float.NegativeInfinity;

        private void Awake()
        {
            _character = GetComponent<PlayerCharacter>();
            _input = GetComponent<PlayerInputReader>();
            _movement = GetComponent<ThirdPersonController>();

            BasicAttack = new SkillSlot { Skill = SkillDefinition.BasicAttack(), HalfArc = 60f };
            Skills[0] = new SkillSlot { Skill = SkillDefinition.DesertSlash(), HalfArc = 100f };
        }

        private void Start()
        {
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        }

        private void Update()
        {
            if (_movement.IsDodging) return;

            if (_input.AttackPressed) TryUse(BasicAttack);
            for (int i = 0; i < Skills.Length; i++)
            {
                if (Skills[i] != null && _input.SkillPressed(i)) TryUse(Skills[i]);
            }
        }

        private void TryUse(SkillSlot slot)
        {
            var sheet = _character.Sheet;
            var result = SkillRules.TryUse(slot.Skill, sheet.Stats, sheet.Vitals, slot.Cooldown, Time.timeAsDouble);
            if (result != SkillUseResult.Success)
            {
                // The basic attack is spammed constantly; only report problems with real skills.
                if (slot != BasicAttack) ShowMessage(Describe(result, slot.Skill));
                return;
            }

            if (slot.Skill.SpCost > 0f) _character.NotifySpSpent();

            // Action-combat convention: attacks go where the camera looks.
            if (cameraTransform != null) _movement.FaceDirection(cameraTransform.forward);
            _movement.LockMovement(slot.Skill.CastTime + slot.Skill.RecoveryTime);
            ResolveHits(slot);
        }

        private void ResolveHits(SkillSlot slot)
        {
            var skill = slot.Skill;
            Vector3 origin = transform.position + Vector3.up;
            int count = Physics.OverlapSphereNonAlloc(origin, skill.Range, _overlap, hitMask, QueryTriggerInteraction.Collide);

            _alreadyHit.Clear();
            for (int i = 0; i < count; i++)
            {
                var target = _overlap[i].GetComponentInParent<Damageable>();
                if (target == null || target.IsDead || !_alreadyHit.Add(target)) continue;

                Vector3 toTarget = Vector3.ProjectOnPlane(target.transform.position - transform.position, Vector3.up);
                if (toTarget.sqrMagnitude > 0.01f && Vector3.Angle(transform.forward, toTarget) > slot.HalfArc) continue;

                var sheet = _character.Sheet;
                var damage = DamageCalculator.Compute(sheet.Stats, sheet.Level, skill, target.Defense, Random.value * 0.9999f);
                target.ApplyDamage(damage);
            }
        }

        private void ShowMessage(string message)
        {
            LastMessage = message;
            LastMessageTime = Time.time;
        }

        private static string Describe(SkillUseResult result, SkillDefinition skill)
        {
            switch (result)
            {
                case SkillUseResult.NotEnoughSp: return "Not enough SP for " + skill.DisplayName;
                case SkillUseResult.OnCooldown: return skill.DisplayName + " is on cooldown";
                case SkillUseResult.CasterDead: return "You cannot act while defeated";
                default: return "";
            }
        }
    }
}
