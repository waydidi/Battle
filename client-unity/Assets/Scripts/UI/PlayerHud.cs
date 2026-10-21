using KingdomsEightBanners.Character;
using KingdomsEightBanners.Combat;
using KingdomsEightBanners.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomsEightBanners.UI
{
    /// <summary>
    /// Builds the prototype HUD in code so no prefab setup is needed:
    ///   top-left   : portrait, "Storm  Lv.68", red HP, blue SP, yellow EXP, violet Job EXP
    ///   bottom-mid : skill bar (Attack, Desert Slash, Dodge) with cooldown overlays
    ///   top-right  : Shop / Event / Skill / Clan / Menu buttons (Skill opens the skill tree)
    ///   bottom-left: chat panel placeholder (real chat arrives with multiplayer)
    /// Later, artists can replace this with a designed prefab that uses the same data.
    /// </summary>
    public sealed class PlayerHud : MonoBehaviour
    {
        [SerializeField] private PlayerCharacter player;

        private static readonly Color HpColor = new Color(0.85f, 0.16f, 0.16f);
        private static readonly Color SpColor = new Color(0.20f, 0.45f, 0.95f);
        private static readonly Color ExpColor = new Color(0.95f, 0.80f, 0.20f);
        private static readonly Color JobExpColor = new Color(0.62f, 0.35f, 0.90f);
        private static readonly Color PanelColor = UiKit.Panel;

                private Text _nameLabel;
        private Bar _hp, _sp, _exp, _jobExp;
        private SlotView _attackSlot, _skillSlot, _dodgeSlot;
        private Text _message;
        private PlayerCombat _combat;
        private ThirdPersonController _movement;

        private sealed class Bar
        {
            public RectTransform Fill;
            public Text Label;

            public void Set(float fraction, string text)
            {
                Fill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
                Label.text = text;
            }
        }

        private sealed class SlotView
        {
            public Image CooldownOverlay;
            public Text Timer;

            public void Set(float remainingFraction, double remainingSeconds)
            {
                CooldownOverlay.fillAmount = remainingFraction;
                Timer.text = remainingSeconds > 0.05 ? remainingSeconds.ToString("0.0") : "";
            }
        }

        public void SetPlayer(PlayerCharacter target)
        {
            player = target;
        }

        private void Start()
        {
            if (player == null) player = FindAnyObjectByType<PlayerCharacter>();
            if (player == null)
            {
                Debug.LogWarning("PlayerHud: no PlayerCharacter found in the scene.");
                enabled = false;
                return;
            }

            _combat = player.GetComponent<PlayerCombat>();
            _movement = player.GetComponent<ThirdPersonController>();
            BuildCanvas();
        }

        private void Update()
        {
            var sheet = player.Sheet;
            var vitals = sheet.Vitals;
            _nameLabel.text = sheet.Name + "   Lv." + sheet.Level;
            _hp.Set(vitals.HpFraction, "HP " + Mathf.CeilToInt(vitals.Hp) + " / " + vitals.MaxHp);
            _sp.Set(vitals.SpFraction, "SP " + Mathf.FloorToInt(vitals.Sp) + " / " + vitals.MaxSp);
            _exp.Set(sheet.CharacterLevel.Fraction, "EXP " + (sheet.CharacterLevel.Fraction * 100f).ToString("0.0") + "%");
            _jobExp.Set(sheet.JobLevel.Fraction,
                "Job Lv." + sheet.JobLevel.Level + "  " + (sheet.JobLevel.Fraction * 100f).ToString("0.0") + "%");

            double now = Time.timeAsDouble;
            if (_combat != null)
            {
                UpdateSlot(_attackSlot, _combat.BasicAttack.Cooldown, now);
                if (_combat.Skills[0] != null) UpdateSlot(_skillSlot, _combat.Skills[0].Cooldown, now);
                bool showMessage = Time.time - _combat.LastMessageTime < 1.5f;
                _message.text = showMessage ? _combat.LastMessage : "";
            }

            if (_movement != null) UpdateSlot(_dodgeSlot, _movement.DodgeCooldown, now);
        }

        private static void UpdateSlot(SlotView view, Cooldown cooldown, double now)
        {
            view.Set(cooldown.RemainingFraction(now), cooldown.Remaining(now));
        }

        // ------------------------------------------------------------------ building

        private void BuildCanvas()
        {
            var root = UiKit.CreateCanvas(transform, "HUD Canvas", 0);
            UiKit.EnsureEventSystem();

            BuildCharacterPanel(root);
            BuildMenuBar(root);
            BuildSkillBar(root);
            BuildChatPlaceholder(root);

            _message = UiKit.CreateText(root, "Message", 26, TextAnchor.MiddleCenter);
            UiKit.Place(_message.rectTransform, new Vector2(0.5f, 0f), new Vector2(-400f, 190f), new Vector2(800f, 40f));
        }

        private void BuildCharacterPanel(RectTransform root)
        {
            var panel = UiKit.CreateImage(root, "Character Panel", PanelColor);
            UiKit.Place(panel.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(460f, 150f));

            var portrait = UiKit.CreateImage(panel.rectTransform, "Portrait", new Color(0.55f, 0.42f, 0.25f));
            UiKit.Place(portrait.rectTransform, new Vector2(0f, 1f), new Vector2(10f, -10f), new Vector2(110f, 130f));
            var initial = UiKit.CreateText(portrait.rectTransform, "Initial", 64, TextAnchor.MiddleCenter);
            UiKit.Stretch(initial.rectTransform);
            initial.text = "S";

            _nameLabel = UiKit.CreateText(panel.rectTransform, "Name", 26, TextAnchor.MiddleLeft);
            UiKit.Place(_nameLabel.rectTransform, new Vector2(0f, 1f), new Vector2(132f, -8f), new Vector2(320f, 32f));

            _hp = CreateBar(panel.rectTransform, "HP", HpColor, -44f, 24f);
            _sp = CreateBar(panel.rectTransform, "SP", SpColor, -72f, 24f);
            _exp = CreateBar(panel.rectTransform, "EXP", ExpColor, -100f, 18f);
            _jobExp = CreateBar(panel.rectTransform, "Job EXP", JobExpColor, -122f, 18f);
        }

        private Bar CreateBar(RectTransform parent, string name, Color color, float y, float height)
        {
            var background = UiKit.CreateImage(parent, name + " Bar", new Color(0f, 0f, 0f, 0.6f));
            UiKit.Place(background.rectTransform, new Vector2(0f, 1f), new Vector2(132f, y), new Vector2(316f, height));

            var fill = UiKit.CreateImage(background.rectTransform, "Fill", color);
            var fillRect = fill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;

            var label = UiKit.CreateText(background.rectTransform, "Label", Mathf.RoundToInt(height * 0.7f), TextAnchor.MiddleCenter);
            UiKit.Stretch(label.rectTransform);
            return new Bar { Fill = fillRect, Label = label };
        }

        /// <summary>Top-right row: Shop, Event, Skill, Clan, Menu. Only Skill works in the prototype.</summary>
        private void BuildMenuBar(RectTransform root)
        {
            string[] labels = { "Shop", "Event", "Skill", "Clan", "Menu" };
            for (int i = 0; i < labels.Length; i++)
            {
                var button = UiKit.CreateButton(root, labels[i] + " Button", labels[i], UiKit.Panel, 18);
                UiKit.Place((RectTransform)button.transform, new Vector2(1f, 1f),
                    new Vector2(-24f - (labels.Length - i) * 84f, -24f), new Vector2(76f, 56f));
                UiKit.AddBorder((RectTransform)button.transform, UiKit.GoldDim);
                if (labels[i] == "Skill") button.onClick.AddListener(ToggleSkillWindow);
            }

            var location = UiKit.CreateText(root, "Location", 22, TextAnchor.MiddleRight);
            UiKit.Place(location.rectTransform, new Vector2(1f, 1f), new Vector2(-324f, -88f), new Vector2(300f, 30f));
            location.text = "Sunspire City  <color=#f2c94c>CH.1</color>";
        }

        private void ToggleSkillWindow()
        {
            var window = FindAnyObjectByType<SkillWindow>();
            if (window != null) window.Toggle();
        }

        private void BuildSkillBar(RectTransform root)
        {
            var bar = UiKit.CreateImage(root, "Skill Bar", PanelColor);
            UiKit.Place(bar.rectTransform, new Vector2(0.5f, 0f), new Vector2(-150f, 24f + 90f), new Vector2(300f, 90f));
            _attackSlot = CreateSlot(bar.rectTransform, 0, "LMB", "Attack");
            _skillSlot = CreateSlot(bar.rectTransform, 1, "1", "Desert\nSlash");
            _dodgeSlot = CreateSlot(bar.rectTransform, 2, "Ctrl/Q", "Dodge");
        }

        private SlotView CreateSlot(RectTransform parent, int index, string key, string title)
        {
            var slot = UiKit.CreateImage(parent, title + " Slot", new Color(0.30f, 0.24f, 0.16f, 0.95f));
            UiKit.Place(slot.rectTransform, new Vector2(0f, 1f), new Vector2(15f + index * 95f, -10f), new Vector2(80f, 70f));

            var name = UiKit.CreateText(slot.rectTransform, "Title", 15, TextAnchor.MiddleCenter);
            UiKit.Stretch(name.rectTransform);
            name.text = title;

            var overlay = UiKit.CreateImage(slot.rectTransform, "Cooldown", new Color(0f, 0f, 0f, 0.7f));
            UiKit.Stretch(overlay.rectTransform);
            // Filled images only draw partially when they have a sprite, so give it a plain white one.
            overlay.sprite = UiKit.WhiteSprite;
            overlay.type = Image.Type.Filled;
            overlay.fillMethod = Image.FillMethod.Radial360;
            overlay.fillOrigin = (int)Image.Origin360.Top;
            overlay.fillClockwise = false;
            overlay.fillAmount = 0f;

            var timer = UiKit.CreateText(slot.rectTransform, "Timer", 24, TextAnchor.MiddleCenter);
            UiKit.Stretch(timer.rectTransform);

            var keyLabel = UiKit.CreateText(slot.rectTransform, "Key", 13, TextAnchor.UpperLeft);
            UiKit.Place(keyLabel.rectTransform, new Vector2(0f, 1f), new Vector2(4f, -2f), new Vector2(70f, 18f));
            keyLabel.text = key;

            return new SlotView { CooldownOverlay = overlay, Timer = timer };
        }

        private void BuildChatPlaceholder(RectTransform root)
        {
            var chat = UiKit.CreateImage(root, "Chat Panel", PanelColor);
            UiKit.Place(chat.rectTransform, new Vector2(0f, 0f), new Vector2(24f, 24f + 220f), new Vector2(480f, 220f));

            var tabs = UiKit.CreateText(chat.rectTransform, "Tabs", 18, TextAnchor.UpperLeft);
            UiKit.Place(tabs.rectTransform, new Vector2(0f, 1f), new Vector2(10f, -6f), new Vector2(460f, 26f));
            tabs.text = "[World]  Party   Clan   System";

            var log = UiKit.CreateText(chat.rectTransform, "Log", 17, TextAnchor.UpperLeft);
            UiKit.Place(log.rectTransform, new Vector2(0f, 1f), new Vector2(10f, -36f), new Vector2(460f, 140f));
            log.text = "<color=#f2c94c>[System]</color> Welcome to Sunspire, Storm.\n" +
                       "<color=#f2c94c>[System]</color> WASD move, Shift sprint, Space jump, Ctrl/Q dodge.\n" +
                       "<color=#f2c94c>[System]</color> Left mouse attack, 1 Desert Slash, K skill tree, Esc frees cursor.\n" +
                       "<color=#9b9b9b>Online chat arrives in Phase 3.</color>";
            log.supportRichText = true;

            var input = UiKit.CreateImage(chat.rectTransform, "Input", new Color(0f, 0f, 0f, 0.5f));
            UiKit.Place(input.rectTransform, new Vector2(0f, 0f), new Vector2(8f, 8f + 30f), new Vector2(464f, 30f));
            var hint = UiKit.CreateText(input.rectTransform, "Hint", 16, TextAnchor.MiddleLeft);
            UiKit.Stretch(hint.rectTransform, 8f);
            hint.text = "Press Enter to chat (offline)";
            hint.color = new Color(1f, 1f, 1f, 0.45f);
        }
    }
}
