using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace KingdomsEightBanners.Character
{
    /// <summary>
    /// Collects keyboard/mouse input once per frame so gameplay scripts never read devices directly.
    /// PC bindings: WASD move, mouse look, Shift sprint, Space jump, Ctrl or Q dodge,
    /// Left mouse attack, 1-6 skills, K skill tree, X sit, Z sleep, mouse wheel zoom, Esc frees the cursor.
    /// Later a touch reader (virtual joystick) can fill the same fields for mobile.
    /// Works with the new Input System package or the legacy Input Manager.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        public const int SkillSlotCount = 6;

        [SerializeField] private float mouseSensitivity = 0.12f;
        [SerializeField] private bool lockCursorOnStart = true;

        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public float Zoom { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool DodgePressed { get; private set; }
        public bool AttackPressed { get; private set; }
        public bool ChatFocusPressed { get; private set; }
        public bool SkillPanelPressed { get; private set; }
        public bool SitPressed { get; private set; }
        public bool SleepPressed { get; private set; }

        private readonly bool[] _skillPressed = new bool[SkillSlotCount];

        /// <summary>Slot index is 0-based: key "1" is slot 0.</summary>
        public bool SkillPressed(int slot) => slot >= 0 && slot < SkillSlotCount && _skillPressed[slot];

        public bool CursorLocked => Cursor.lockState == CursorLockMode.Locked;

        /// <summary>Set by open menus (e.g. the skill window) so clicks go to the UI, not to attacks.</summary>
        public bool MenuOpen { get; set; }

        private void Start()
        {
            if (lockCursorOnStart) SetCursorLocked(true);
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            ReadInputSystem();
#else
            ReadLegacyInput();
#endif
            if (!CursorLocked)
            {
                // While the cursor is free (menus, alt-tab) the camera and attacks ignore the mouse.
                Look = Vector2.zero;
                if (AttackPressed)
                {
                    if (!MenuOpen) SetCursorLocked(true);
                    AttackPressed = false;
                }
            }
        }

        public static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

#if ENABLE_INPUT_SYSTEM
        private void ReadInputSystem()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null)
            {
                ClearFrame();
                return;
            }

            var move = Vector2.zero;
            if (keyboard.wKey.isPressed) move.y += 1f;
            if (keyboard.sKey.isPressed) move.y -= 1f;
            if (keyboard.dKey.isPressed) move.x += 1f;
            if (keyboard.aKey.isPressed) move.x -= 1f;
            Move = Vector2.ClampMagnitude(move, 1f);

            Look = mouse.delta.ReadValue() * mouseSensitivity;
            // Scroll units differ per platform, so treat any scroll as one zoom notch.
            float scroll = mouse.scroll.ReadValue().y;
            Zoom = Mathf.Abs(scroll) > 0.01f ? Mathf.Sign(scroll) : 0f;
            SprintHeld = keyboard.leftShiftKey.isPressed;
            JumpPressed = keyboard.spaceKey.wasPressedThisFrame;
            DodgePressed = keyboard.leftCtrlKey.wasPressedThisFrame || keyboard.qKey.wasPressedThisFrame;
            AttackPressed = mouse.leftButton.wasPressedThisFrame;
            ChatFocusPressed = keyboard.enterKey.wasPressedThisFrame;
            SkillPanelPressed = keyboard.kKey.wasPressedThisFrame;
            SitPressed = keyboard.xKey.wasPressedThisFrame;
            SleepPressed = keyboard.zKey.wasPressedThisFrame;

            _skillPressed[0] = keyboard.digit1Key.wasPressedThisFrame;
            _skillPressed[1] = keyboard.digit2Key.wasPressedThisFrame;
            _skillPressed[2] = keyboard.digit3Key.wasPressedThisFrame;
            _skillPressed[3] = keyboard.digit4Key.wasPressedThisFrame;
            _skillPressed[4] = keyboard.digit5Key.wasPressedThisFrame;
            _skillPressed[5] = keyboard.digit6Key.wasPressedThisFrame;

            if (keyboard.escapeKey.wasPressedThisFrame) SetCursorLocked(false);
        }
#else
        private void ReadLegacyInput()
        {
            Move = Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
            // Legacy mouse axes are already scaled; multiply so both input paths feel similar.
            Look = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * (mouseSensitivity * 20f);
            Zoom = Input.mouseScrollDelta.y;
            SprintHeld = Input.GetKey(KeyCode.LeftShift);
            JumpPressed = Input.GetKeyDown(KeyCode.Space);
            DodgePressed = Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.Q);
            AttackPressed = Input.GetMouseButtonDown(0);
            ChatFocusPressed = Input.GetKeyDown(KeyCode.Return);
            SkillPanelPressed = Input.GetKeyDown(KeyCode.K);
            SitPressed = Input.GetKeyDown(KeyCode.X);
            SleepPressed = Input.GetKeyDown(KeyCode.Z);

            for (int i = 0; i < SkillSlotCount; i++)
            {
                _skillPressed[i] = Input.GetKeyDown(KeyCode.Alpha1 + i);
            }

            if (Input.GetKeyDown(KeyCode.Escape)) SetCursorLocked(false);
        }
#endif

        private void ClearFrame()
        {
            Move = Vector2.zero;
            Look = Vector2.zero;
            Zoom = 0f;
            SprintHeld = JumpPressed = DodgePressed = AttackPressed = ChatFocusPressed = SkillPanelPressed = SitPressed = SleepPressed = false;
            for (int i = 0; i < SkillSlotCount; i++) _skillPressed[i] = false;
        }
    }
}
