using System.Collections.Generic;
using System.Text;
using KingdomsEightBanners.Character;
using KingdomsEightBanners.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomsEightBanners.UI
{
    /// <summary>
    /// The Skill window from the UI mockup: tabs (Skills / Passive / Awaken / City Lineage),
    /// the Warrior - Sunspire City tree in three branches, and a details panel with a
    /// next-level preview and a Level Up button. Press K or the HUD's Skill button to open it.
    /// Rules come from Core.SkillTreeState; in multiplayer the Level Up click becomes a
    /// request that the server validates.
    /// </summary>
    public sealed class SkillWindow : MonoBehaviour
    {
        [SerializeField] private PlayerCharacter player;
        [SerializeField] private bool openOnStart;

        private static readonly Vector2 WindowSize = new Vector2(1010f, 820f);
        private static readonly Color[] BranchColors =
        {
            new Color(0.20f, 0.14f, 0.08f, 0.95f), // Sand Blade
            new Color(0.08f, 0.13f, 0.22f, 0.95f), // Caravan Guard
            new Color(0.24f, 0.08f, 0.06f, 0.95f), // Sunfire Combat Arts
        };
        private static readonly Color[] BranchAccents =
        {
            new Color(0.93f, 0.62f, 0.22f),
            new Color(0.38f, 0.62f, 0.95f),
            new Color(0.95f, 0.36f, 0.20f),
        };
        private static readonly Color LockedColor = new Color(0.22f, 0.22f, 0.22f, 1f);
        private static readonly Color UnlearnedColor = new Color(0.35f, 0.30f, 0.24f, 1f);

        // Node centres inside a 318 x 440 branch column, in mockup order (top, mid L, mid R, bottom L, bottom R).
        private static readonly Vector2[] NodeCenters =
        {
            new Vector2(159f, 115f), new Vector2(75f, 245f), new Vector2(243f, 245f),
            new Vector2(75f, 362f), new Vector2(243f, 362f),
        };
        private const float NodeSize = 64f;

        private sealed class NodeView
        {
            public SkillNode Node;
            public int Branch;
            public Image Background;
            public Text Level;
            public Text Lock;
            public RectTransform SelectionFrame;
        }

        private readonly List<NodeView> _nodes = new List<NodeView>();
        private readonly List<Image> _tabs = new List<Image>();
        private GameObject _window;
        private Text _skillPoints;
        private Text _detailName, _detailTags, _detailDescription, _detailLevel, _detailNext, _detailRequired, _detailMessage;
        private Image _detailIcon;
        private Text _detailIconLabel;
        private Button _levelUpButton;
        private Text _comingSoon;
        private GameObject _treeRoot;
        private NodeView _selected;
        private PlayerInputReader _input;

        public bool IsOpen => _window != null && _window.activeSelf;

        public void SetPlayer(PlayerCharacter target)
        {
            player = target;
        }

        private void Start()
        {
            if (player == null) player = FindAnyObjectByType<PlayerCharacter>();
            if (player == null)
            {
                Debug.LogWarning("SkillWindow: no PlayerCharacter found in the scene.");
                enabled = false;
                return;
            }

            _input = player.GetComponent<PlayerInputReader>();
            UiKit.EnsureEventSystem();
            Build();
            Select(_nodes.Find(n => n.Node.Id == "sunfire_cleave") ?? _nodes[0]);
            _window.SetActive(false);
            if (openOnStart) Toggle();
        }

        private void Update()
        {
            if (_input != null && _input.SkillPanelPressed) Toggle();
        }

        public void Toggle()
        {
            if (_window == null) return;
            bool open = !_window.activeSelf;
            _window.SetActive(open);
            if (_input != null) _input.MenuOpen = open;
            PlayerInputReader.SetCursorLocked(!open);
            if (open) Refresh();
        }

        // ------------------------------------------------------------------ state → view

        private void Refresh()
        {
            var state = player.Skills;
            int characterLevel = player.Sheet.Level;
            _skillPoints.text = "Skill Points   <size=30><b>" + state.AvailableSkillPoints + "</b></size>";

            foreach (var view in _nodes)
            {
                int level = state.LevelOf(view.Node.Id);
                bool unlocked = state.IsUnlocked(view.Node, characterLevel);
                view.Background.color = !unlocked ? LockedColor : level > 0 ? BranchAccents[view.Branch] : UnlearnedColor;
                view.Level.text = (level > 0 ? "Lv. " : "") + level + "/" + view.Node.MaxLevel;
                view.Level.color = unlocked ? Color.white : UiKit.TextMuted;
                view.Lock.gameObject.SetActive(!unlocked);
                view.SelectionFrame.gameObject.SetActive(view == _selected);
            }

            RefreshDetails();
        }

        private void RefreshDetails()
        {
            if (_selected == null) return;
            var node = _selected.Node;
            var state = player.Skills;
            int level = state.LevelOf(node.Id);

            _detailIcon.color = BranchAccents[_selected.Branch];
            _detailIconLabel.text = Initials(node.DisplayName);
            _detailName.text = node.DisplayName;
            _detailTags.text = "<color=#e8a33a>[" + string.Join("]  [", node.Tags) + "]</color>";
            _detailDescription.text = node.Description;
            _detailLevel.text = "Lv. " + level + " / " + node.MaxLevel;

            var next = new StringBuilder();
            if (level >= node.MaxLevel)
            {
                next.Append("<color=#f2c94c>Max level reached</color>");
            }
            else
            {
                next.Append("<color=#7ed957>Next Level</color>");
                foreach (var effect in node.Effects)
                {
                    next.Append("\n• ").Append(effect.Label).Append(' ')
                        .Append(Format(effect.ValueAt(level), effect.Unit))
                        .Append(" <color=#7ed957>(-> ").Append(Format(effect.ValueAt(level + 1), effect.Unit)).Append(")</color>");
                }
            }

            _detailNext.text = next.ToString();
            _detailRequired.text = "Required  <b>" + node.CostPerLevel + "</b> Skill Point" + (node.CostPerLevel == 1 ? "" : "s");

            var check = state.CanLevelUp(node.Id, player.Sheet.Level);
            _levelUpButton.interactable = check == SkillLevelUpResult.Success;
            _detailMessage.text = check == SkillLevelUpResult.Success ? "" : Describe(check, node);
        }

        private static string Format(float value, string unit)
        {
            string number = Mathf.Approximately(value, Mathf.Round(value)) ? value.ToString("0") : value.ToString("0.#");
            return (unit == "%" && value > 0f ? "+" : "") + number + unit;
        }

        private static string Describe(SkillLevelUpResult result, SkillNode node)
        {
            switch (result)
            {
                case SkillLevelUpResult.AlreadyMaxLevel: return "Already at max level.";
                case SkillLevelUpResult.CharacterLevelTooLow: return "Requires character Lv." + node.RequiredCharacterLevel + ".";
                case SkillLevelUpResult.PrerequisiteNotMet: return "Requires a previous skill at Lv." + node.PrerequisiteLevel + ".";
                case SkillLevelUpResult.NotEnoughSkillPoints: return "Not enough Skill Points.";
                default: return "";
            }
        }

        private void Select(NodeView view)
        {
            _selected = view;
            Refresh();
        }

        private void LevelUpSelected()
        {
            if (_selected == null) return;
            // OFFLINE PROTOTYPE: applied locally. Online this sends a request and waits for the server.
            player.Skills.TryLevelUp(_selected.Node.Id, player.Sheet.Level);
            Refresh();
        }

        private void SelectTab(int index)
        {
            for (int i = 0; i < _tabs.Count; i++) _tabs[i].color = i == index ? UiKit.GoldDim : UiKit.Panel;
            _treeRoot.SetActive(index == 0);
            _comingSoon.gameObject.SetActive(index != 0);
        }

        private static string Initials(string name)
        {
            var parts = name.Split(' ');
            return parts.Length > 1 ? parts[0].Substring(0, 1) + parts[1].Substring(0, 1) : name.Substring(0, 2);
        }

        // ------------------------------------------------------------------ building

        private void Build()
        {
            var canvas = UiKit.CreateCanvas(transform, "Skill Window Canvas", 10);
            var window = UiKit.CreateImage(canvas, "Skill Window", UiKit.PanelSolid);
            window.raycastTarget = true; // stop clicks from reaching anything behind the window
            _window = window.gameObject;
            var rect = window.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-40f, -20f);
            rect.sizeDelta = WindowSize;
            UiKit.AddBorder(rect, UiKit.Gold);

            var title = UiKit.CreateText(rect, "Title", 46, TextAnchor.MiddleLeft);
            UiKit.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(30f, -12f), new Vector2(400f, 60f));
            title.text = "Skill";
            title.color = UiKit.Gold;

            var close = UiKit.CreateButton(rect, "Close", "X", new Color(0.3f, 0.1f, 0.1f, 0.9f), 26);
            UiKit.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(-60f, -16f), new Vector2(44f, 44f));
            close.onClick.AddListener(Toggle);

            string[] tabNames = { "Skills", "Passive", "Awaken", "City Lineage" };
            for (int i = 0; i < tabNames.Length; i++)
            {
                int index = i;
                var tab = UiKit.CreateButton(rect, tabNames[i] + " Tab", tabNames[i], UiKit.Panel, 24);
                UiKit.Place((RectTransform)tab.transform, new Vector2(0f, 1f), new Vector2(20f + i * 196f, -80f), new Vector2(186f, 50f));
                UiKit.AddBorder((RectTransform)tab.transform, UiKit.GoldDim);
                tab.onClick.AddListener(() => SelectTab(index));
                _tabs.Add((Image)tab.targetGraphic);
            }

            var tree = player.Skills.Tree;
            var heading = UiKit.CreateText(rect, "Tree Title", 32, TextAnchor.MiddleLeft);
            UiKit.Place(heading.rectTransform, new Vector2(0f, 1f), new Vector2(30f, -142f), new Vector2(700f, 40f));
            heading.text = tree.Title;
            heading.color = UiKit.Gold;

            var specialization = UiKit.CreateText(rect, "Specialization", 22, TextAnchor.MiddleLeft);
            UiKit.Place(specialization.rectTransform, new Vector2(0f, 1f), new Vector2(30f, -180f), new Vector2(700f, 30f));
            specialization.text = "City-born specialization: <color=#6fb7ff>" + tree.Specialization + "</color>";
            specialization.color = UiKit.TextMuted;

            var pointsBox = UiKit.CreateImage(rect, "Skill Points", UiKit.Panel);
            UiKit.Place(pointsBox.rectTransform, new Vector2(1f, 1f), new Vector2(-250f, -146f), new Vector2(220f, 50f));
            UiKit.AddBorder(pointsBox.rectTransform, UiKit.GoldDim);
            _skillPoints = UiKit.CreateText(pointsBox.rectTransform, "Value", 22, TextAnchor.MiddleCenter);
            UiKit.Stretch(_skillPoints.rectTransform);

            _treeRoot = new GameObject("Tree", typeof(RectTransform));
            var treeRect = (RectTransform)_treeRoot.transform;
            treeRect.SetParent(rect, false);
            UiKit.Stretch(treeRect);
            for (int b = 0; b < tree.Branches.Length; b++) BuildBranch(treeRect, tree.Branches[b], b);

            _comingSoon = UiKit.CreateText(rect, "Coming Soon", 26, TextAnchor.MiddleCenter);
            UiKit.Place(_comingSoon.rectTransform, new Vector2(0f, 1f), new Vector2(0f, -400f), new Vector2(WindowSize.x, 40f));
            _comingSoon.text = "Unlocks in Phase 6 (passives, awakening and city lineage).";
            _comingSoon.color = UiKit.TextMuted;

            BuildDetails(rect);
            SelectTab(0);
        }

        private void BuildBranch(RectTransform parent, SkillBranch branch, int index)
        {
            var column = UiKit.CreateImage(parent, branch.DisplayName, BranchColors[index]);
            var rect = column.rectTransform;
            UiKit.Place(rect, new Vector2(0f, 1f), new Vector2(20f + index * 326f, -222f), new Vector2(318f, 440f));
            UiKit.AddBorder(rect, UiKit.GoldDim);

            var name = UiKit.CreateText(rect, "Name", 26, TextAnchor.UpperCenter);
            UiKit.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, -10f), new Vector2(318f, 32f));
            name.text = branch.DisplayName;
            name.color = UiKit.Gold;

            var description = UiKit.CreateText(rect, "Description", 15, TextAnchor.UpperCenter);
            UiKit.Place(description.rectTransform, new Vector2(0f, 1f), new Vector2(10f, -44f), new Vector2(298f, 40f));
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            description.text = branch.Description;
            description.color = UiKit.TextMuted;

            // Connectors first so they are drawn behind the nodes.
            var lineColor = new Color(0.85f, 0.62f, 0.25f, 0.9f);
            float half = NodeSize / 2f;
            float barY = NodeCenters[0].y + half + 49f; // just below the top node's name and level labels
            UiKit.Line(rect, NodeCenters[0] + new Vector2(0f, half + 40f), new Vector2(NodeCenters[0].x, barY), lineColor);
            UiKit.Line(rect, new Vector2(NodeCenters[1].x, barY), new Vector2(NodeCenters[2].x, barY), lineColor);
            UiKit.Line(rect, new Vector2(NodeCenters[1].x, barY), NodeCenters[1] - new Vector2(0f, half), lineColor);
            UiKit.Line(rect, new Vector2(NodeCenters[2].x, barY), NodeCenters[2] - new Vector2(0f, half), lineColor);
            UiKit.Line(rect, NodeCenters[1] + new Vector2(0f, half + 40f), NodeCenters[3] - new Vector2(0f, half), lineColor);
            UiKit.Line(rect, NodeCenters[2] + new Vector2(0f, half + 40f), NodeCenters[4] - new Vector2(0f, half), lineColor);

            for (int i = 0; i < branch.Nodes.Length && i < NodeCenters.Length; i++)
            {
                _nodes.Add(BuildNode(rect, branch.Nodes[i], index, NodeCenters[i]));
            }
        }

        private NodeView BuildNode(RectTransform parent, SkillNode node, int branch, Vector2 center)
        {
            var view = new NodeView { Node = node, Branch = branch };

            var frame = UiKit.CreateImage(parent, node.DisplayName + " Selection", UiKit.Gold);
            UiKit.PlaceCentered(frame.rectTransform, center, new Vector2(NodeSize + 10f, NodeSize + 10f));
            view.SelectionFrame = frame.rectTransform;

            var button = UiKit.CreateButton(parent, node.DisplayName, Initials(node.DisplayName), UnlearnedColor, 24);
            var rect = (RectTransform)button.transform;
            UiKit.PlaceCentered(rect, center, new Vector2(NodeSize, NodeSize));
            UiKit.AddBorder(rect, UiKit.GoldDim);
            button.onClick.AddListener(() => Select(view));
            view.Background = (Image)button.targetGraphic;

            var lockLabel = UiKit.CreateText(rect, "Lock", 12, TextAnchor.LowerRight);
            UiKit.Stretch(lockLabel.rectTransform, 3f);
            lockLabel.text = "LOCK";
            lockLabel.color = new Color(1f, 0.85f, 0.5f);
            view.Lock = lockLabel;

            var name = UiKit.CreateText(parent, "Name", 16, TextAnchor.UpperCenter);
            UiKit.PlaceCentered(name.rectTransform, center + new Vector2(0f, NodeSize / 2f + 12f), new Vector2(150f, 20f));
            name.text = node.DisplayName;

            view.Level = UiKit.CreateText(parent, "Level", 15, TextAnchor.UpperCenter);
            UiKit.PlaceCentered(view.Level.rectTransform, center + new Vector2(0f, NodeSize / 2f + 30f), new Vector2(150f, 20f));
            return view;
        }

        private void BuildDetails(RectTransform parent)
        {
            var panel = UiKit.CreateImage(parent, "Details", new Color(0.10f, 0.08f, 0.07f, 0.97f));
            var rect = panel.rectTransform;
            UiKit.Place(rect, new Vector2(0f, 1f), new Vector2(20f, -674f), new Vector2(WindowSize.x - 40f, 130f));
            UiKit.AddBorder(rect, UiKit.GoldDim);

            _detailIcon = UiKit.CreateImage(rect, "Icon", UiKit.GoldDim);
            UiKit.Place(_detailIcon.rectTransform, new Vector2(0f, 1f), new Vector2(14f, -14f), new Vector2(100f, 100f));
            _detailIconLabel = UiKit.CreateText(_detailIcon.rectTransform, "Initials", 38, TextAnchor.MiddleCenter);
            UiKit.Stretch(_detailIconLabel.rectTransform);

            _detailName = UiKit.CreateText(rect, "Name", 28, TextAnchor.UpperLeft);
            UiKit.Place(_detailName.rectTransform, new Vector2(0f, 1f), new Vector2(130f, -10f), new Vector2(330f, 34f));
            _detailName.color = UiKit.Gold;

            _detailTags = UiKit.CreateText(rect, "Tags", 16, TextAnchor.UpperLeft);
            UiKit.Place(_detailTags.rectTransform, new Vector2(0f, 1f), new Vector2(130f, -46f), new Vector2(330f, 22f));

            _detailDescription = UiKit.CreateText(rect, "Description", 15, TextAnchor.UpperLeft);
            UiKit.Place(_detailDescription.rectTransform, new Vector2(0f, 1f), new Vector2(130f, -72f), new Vector2(330f, 54f));
            _detailDescription.horizontalOverflow = HorizontalWrapMode.Wrap;
            _detailDescription.color = UiKit.TextMuted;

            _detailLevel = UiKit.CreateText(rect, "Level", 22, TextAnchor.UpperLeft);
            UiKit.Place(_detailLevel.rectTransform, new Vector2(0f, 1f), new Vector2(480f, -10f), new Vector2(220f, 28f));
            _detailLevel.color = UiKit.Gold;

            _detailNext = UiKit.CreateText(rect, "Next Level", 15, TextAnchor.UpperLeft);
            UiKit.Place(_detailNext.rectTransform, new Vector2(0f, 1f), new Vector2(480f, -40f), new Vector2(260f, 86f));

            _detailRequired = UiKit.CreateText(rect, "Required", 18, TextAnchor.MiddleCenter);
            UiKit.Place(_detailRequired.rectTransform, new Vector2(0f, 1f), new Vector2(760f, -10f), new Vector2(190f, 30f));

            _levelUpButton = UiKit.CreateButton(rect, "Level Up", "Level Up", new Color(0.62f, 0.45f, 0.18f, 1f), 26);
            UiKit.Place((RectTransform)_levelUpButton.transform, new Vector2(0f, 1f), new Vector2(760f, -46f), new Vector2(190f, 50f));
            UiKit.AddBorder((RectTransform)_levelUpButton.transform, UiKit.Gold);
            _levelUpButton.onClick.AddListener(LevelUpSelected);

            _detailMessage = UiKit.CreateText(rect, "Message", 14, TextAnchor.UpperCenter);
            UiKit.Place(_detailMessage.rectTransform, new Vector2(0f, 1f), new Vector2(740f, -100f), new Vector2(230f, 24f));
            _detailMessage.color = new Color(1f, 0.6f, 0.5f);
        }
    }
}
