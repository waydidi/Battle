using System.Collections.Generic;
using KingdomsEightBanners.Core;
using UnityEngine;

namespace KingdomsEightBanners.Character
{
    /// <summary>
    /// Camera-relative third-person movement: run, sprint (drains SP), jump, directional dodge
    /// (SP cost + cooldown) and smooth 360-degree rotation. Uses Unity's CharacterController so
    /// the player collides with walls, climbs steps and respects slope limits.
    /// All speeds come from StatFormulas, never from hand-typed values on the client.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputReader))]
    [RequireComponent(typeof(PlayerCharacter))]
    public sealed class ThirdPersonController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Camera used to make WASD camera-relative. Defaults to Camera.main.")]
        [SerializeField] private Transform cameraTransform;
        [Tooltip("Optional. Parameters used if present: Speed (float), Grounded (bool), Jump, Dodge (triggers).")]
        [SerializeField] private Animator animator;

        [Header("Feel")]
        [SerializeField] private float groundAcceleration = 40f;
        [SerializeField] private float airAcceleration = 8f;
        [SerializeField] private float turnSpeedDegrees = 720f;
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -25f;
        [Tooltip("Grace period after walking off a ledge during which jump still works.")]
        [SerializeField] private float coyoteTime = 0.12f;
        [Tooltip("Fraction of the dodge during which the player cannot be hit.")]
        [SerializeField, Range(0f, 1f)] private float dodgeInvulnerableFraction = 0.7f;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int GroundedHash = Animator.StringToHash("Grounded");
        private static readonly int JumpHash = Animator.StringToHash("Jump");
        private static readonly int DodgeHash = Animator.StringToHash("Dodge");

        private CharacterController _controller;
        private PlayerInputReader _input;
        private PlayerCharacter _character;
        private readonly HashSet<int> _animatorParameters = new HashSet<int>();

        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;
        private float _lastGroundedTime = float.NegativeInfinity;
        private float _movementLockedUntil;

        private Vector3 _dodgeDirection;
        private float _dodgeStartTime;

        public Cooldown DodgeCooldown { get; } = new Cooldown();
        public bool IsDodging { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsGrounded => _controller.isGrounded;
        public float HorizontalSpeed => _horizontalVelocity.magnitude;

        public bool IsInvulnerable =>
            IsDodging && Time.time - _dodgeStartTime < StatFormulas.DodgeDuration * dodgeInvulnerableFraction;

        /// <summary>Blocks walking (not falling) for a short time, e.g. during attack recovery.</summary>
        public void LockMovement(float seconds)
        {
            _movementLockedUntil = Mathf.Max(_movementLockedUntil, Time.time + seconds);
        }

        /// <summary>Instantly turns the character to face a world direction (used by attacks).</summary>
        public void FaceDirection(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(direction);
        }

        /// <summary>The camera-relative direction the player is currently pressing, or zero.</summary>
        public Vector3 DesiredMoveDirection()
        {
            Vector2 move = _input.Move;
            if (move.sqrMagnitude < 0.0001f) return Vector3.zero;

            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;
            if (cameraTransform != null)
            {
                forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
                right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            }

            return Vector3.ClampMagnitude(forward * move.y + right * move.x, 1f);
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<PlayerInputReader>();
            _character = GetComponent<PlayerCharacter>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                foreach (var parameter in animator.parameters) _animatorParameters.Add(parameter.nameHash);
            }
        }

        private void Start()
        {
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        }

        private void Update()
        {
            if (_controller.isGrounded) _lastGroundedTime = Time.time;

            if (_character.Sheet.Vitals.IsDead)
            {
                _horizontalVelocity = Vector3.zero;
                IsSprinting = false;
            }
            else if (IsDodging)
            {
                UpdateDodge();
            }
            else
            {
                if (_input.DodgePressed && TryStartDodge())
                {
                    UpdateDodge();
                }
                else
                {
                    UpdateLocomotion();
                    UpdateJump();
                }
            }

            ApplyGravity();
            var motion = (_horizontalVelocity + Vector3.up * _verticalVelocity) * Time.deltaTime;
            _controller.Move(motion);
            UpdateAnimator();
        }

        private void UpdateLocomotion()
        {
            var stats = _character.Sheet.Stats;
            Vector3 desired = Time.time < _movementLockedUntil ? Vector3.zero : DesiredMoveDirection();
            float inputAmount = desired.magnitude;

            IsSprinting = false;
            float targetSpeed = StatFormulas.RunSpeed(stats);
            if (_input.SprintHeld && inputAmount > 0.1f && _controller.isGrounded)
            {
                float cost = StatFormulas.SprintSpCostPerSecond(stats) * Time.deltaTime;
                if (_character.TrySpendSp(cost))
                {
                    IsSprinting = true;
                    targetSpeed = StatFormulas.SprintSpeed(stats);
                }
            }

            Vector3 targetVelocity = desired * targetSpeed;
            float acceleration = _controller.isGrounded ? groundAcceleration : airAcceleration;
            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetVelocity, acceleration * Time.deltaTime);

            if (inputAmount > 0.1f)
            {
                var targetRotation = Quaternion.LookRotation(desired);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, targetRotation, turnSpeedDegrees * Time.deltaTime);
            }
        }

        private void UpdateJump()
        {
            bool canJump = Time.time - _lastGroundedTime <= coyoteTime && _verticalVelocity <= 0.01f;
            if (_input.JumpPressed && canJump && Time.time >= _movementLockedUntil)
            {
                _verticalVelocity = Mathf.Sqrt(2f * jumpHeight * -gravity);
                _lastGroundedTime = float.NegativeInfinity;
                SetTrigger(JumpHash);
            }
        }

        private bool TryStartDodge()
        {
            var stats = _character.Sheet.Stats;
            double now = Time.timeAsDouble;
            if (!_controller.isGrounded || !DodgeCooldown.IsReady(now)) return false;
            if (!_character.TrySpendSp(StatFormulas.DodgeSpCost(stats))) return false;

            Vector3 direction = DesiredMoveDirection();
            // No direction held: hop backwards, away from what the character faces.
            _dodgeDirection = direction.sqrMagnitude > 0.01f ? direction.normalized : -transform.forward;
            if (direction.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(_dodgeDirection);

            DodgeCooldown.Start(now, StatFormulas.DodgeCooldown(stats));
            _dodgeStartTime = Time.time;
            IsDodging = true;
            IsSprinting = false;
            SetTrigger(DodgeHash);
            return true;
        }

        private void UpdateDodge()
        {
            if (Time.time - _dodgeStartTime >= StatFormulas.DodgeDuration)
            {
                IsDodging = false;
                // Leave the dodge at running speed so it blends back into locomotion.
                _horizontalVelocity = _dodgeDirection * Mathf.Min(_horizontalVelocity.magnitude,
                    StatFormulas.RunSpeed(_character.Sheet.Stats));
                return;
            }

            _horizontalVelocity = _dodgeDirection * (StatFormulas.DodgeDistance / StatFormulas.DodgeDuration);
        }

        private void ApplyGravity()
        {
            if (_controller.isGrounded && _verticalVelocity < 0f)
            {
                // Small downward push keeps the controller snapped to slopes and steps.
                _verticalVelocity = -2f;
            }
            else
            {
                _verticalVelocity += gravity * Time.deltaTime;
            }
        }

        private void UpdateAnimator()
        {
            if (animator == null) return;
            if (_animatorParameters.Contains(SpeedHash)) animator.SetFloat(SpeedHash, HorizontalSpeed);
            if (_animatorParameters.Contains(GroundedHash)) animator.SetBool(GroundedHash, _controller.isGrounded);
        }

        private void SetTrigger(int hash)
        {
            if (animator != null && _animatorParameters.Contains(hash)) animator.SetTrigger(hash);
        }
    }
}
