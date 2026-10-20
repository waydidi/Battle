using UnityEngine;

namespace KingdomsEightBanners.Character
{
    /// <summary>
    /// Over-the-shoulder orbit camera. Mouse rotates freely around the player (360 degrees yaw,
    /// clamped pitch), the mouse wheel changes distance, and a sphere cast pulls the camera
    /// in front of walls so it never clips through buildings.
    /// </summary>
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private PlayerInputReader input;

        [Header("Framing")]
        [Tooltip("Point on the character the camera orbits around (roughly the shoulders).")]
        [SerializeField] private Vector3 pivotOffset = new Vector3(0f, 1.6f, 0f);
        [Tooltip("Sideways offset for the over-the-shoulder look. 0 = centred.")]
        [SerializeField] private float shoulderOffset = 0.5f;
        [SerializeField] private float distance = 4.5f;
        [SerializeField] private float minDistance = 1.5f;
        [SerializeField] private float maxDistance = 9f;
        [SerializeField] private float zoomSpeed = 1.5f;

        [Header("Rotation")]
        [SerializeField] private float yawSpeed = 1f;
        [SerializeField] private float pitchSpeed = 1f;
        [SerializeField] private float minPitch = -35f;
        [SerializeField] private float maxPitch = 70f;
        [SerializeField] private bool invertY;

        [Header("Collision")]
        [SerializeField] private float collisionRadius = 0.25f;
        [SerializeField] private LayerMask collisionMask = ~0;
        [Tooltip("How fast the camera returns outward after an obstacle is gone.")]
        [SerializeField] private float returnSpeed = 6f;

        private readonly RaycastHit[] _hits = new RaycastHit[16];
        private float _yaw;
        private float _pitch = 15f;
        private float _currentDistance;

        public void SetTarget(Transform newTarget, PlayerInputReader newInput)
        {
            target = newTarget;
            input = newInput;
        }

        private void Start()
        {
            if (target != null) _yaw = target.eulerAngles.y;
            _currentDistance = distance;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            if (input != null)
            {
                Vector2 look = input.Look;
                _yaw += look.x * yawSpeed;
                _pitch += (invertY ? look.y : -look.y) * pitchSpeed;
                distance = Mathf.Clamp(distance - input.Zoom * zoomSpeed, minDistance, maxDistance);
            }

            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
            var rotation = Quaternion.Euler(_pitch, _yaw, 0f);

            Vector3 pivot = target.position + pivotOffset + rotation * Vector3.right * shoulderOffset;
            Vector3 back = rotation * Vector3.back;

            float allowed = ComputeUnobstructedDistance(pivot, back, distance);
            // Snap in immediately when blocked; ease back out when clear.
            _currentDistance = allowed < _currentDistance
                ? allowed
                : Mathf.MoveTowards(_currentDistance, allowed, returnSpeed * Time.deltaTime);

            transform.SetPositionAndRotation(pivot + back * _currentDistance, rotation);
        }

        private float ComputeUnobstructedDistance(Vector3 pivot, Vector3 direction, float wanted)
        {
            int count = Physics.SphereCastNonAlloc(
                pivot, collisionRadius, direction, _hits, wanted, collisionMask, QueryTriggerInteraction.Ignore);

            float best = wanted;
            for (int i = 0; i < count; i++)
            {
                var hit = _hits[i];
                // Ignore the player's own colliders; distance 0 means we started inside something.
                if (hit.collider.transform.IsChildOf(target) || hit.distance <= 0f) continue;
                best = Mathf.Min(best, hit.distance);
            }

            return Mathf.Max(best, 0.1f);
        }
    }
}
