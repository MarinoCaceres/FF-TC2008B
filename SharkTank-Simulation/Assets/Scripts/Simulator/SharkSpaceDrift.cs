using UnityEngine;

namespace SharkTank.Simulator
{
    /// <summary>
    /// Gently moves and rotates the decorative shark.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SharkSpaceDrift : MonoBehaviour
    {
        [Header("Movement toward -X")]
        [Tooltip("Distance from the shark's starting X position toward world -X.")]
        [Min(0f)]
        [SerializeField] private float negativeXTravelDistance = 8f;
        [Tooltip("Seconds for one full back-and-forth movement cycle.")]
        [Min(0.1f)]
        [SerializeField] private float fullCycleDuration = 18f;

        [Header("Floating")]
        [Tooltip("Distance the shark gently floats along its local up axis.")]
        [Min(0f)]
        [SerializeField] private float verticalFloatAmplitude = 0.2f;
        [Tooltip("Seconds for one full vertical float cycle.")]
        [Min(0.1f)]
        [SerializeField] private float verticalFloatPeriod = 6.5f;

        [Header("Rotation Degrees Per Second")]
        [Tooltip("Local rotation speed in degrees per second on each axis.")]
        [SerializeField] private Vector3 rotationDegreesPerSecond = new Vector3(45f, 90f, 30f);

        private Vector3 originPosition;
        private Quaternion originRotation;
        private float elapsedTime;
        private bool hasCapturedOrigin;

        private void Awake()
        {
            originPosition = transform.position;
            originRotation = transform.rotation;
            hasCapturedOrigin = true;
        }

        private void Update()
        {
            elapsedTime += Time.deltaTime;

            float cycleDuration = Mathf.Max(0.1f, fullCycleDuration);
            float progress = 0.5f - 0.5f * Mathf.Cos(elapsedTime * Mathf.PI * 2f / cycleDuration);
            float travelDistance = Mathf.Max(0f, negativeXTravelDistance);
            Vector3 horizontalPosition = originPosition + Vector3.left * (travelDistance * progress);

            float floatPeriod = Mathf.Max(0.1f, verticalFloatPeriod);
            float floatOffset = Mathf.Sin(elapsedTime * Mathf.PI * 2f / floatPeriod) * verticalFloatAmplitude;

            transform.position = horizontalPosition + originRotation * Vector3.up * floatOffset;
            transform.rotation = originRotation * Quaternion.Euler(rotationDegreesPerSecond * elapsedTime);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = hasCapturedOrigin ? originPosition : transform.position;
            Vector3 start = origin;
            Vector3 end = origin + Vector3.left * Mathf.Max(0f, negativeXTravelDistance);

            Gizmos.color = new Color(0.2f, 0.85f, 1f, 1f);
            Gizmos.DrawLine(start, end);
            Gizmos.DrawSphere(start, 0.25f);
            Gizmos.color = new Color(1f, 0.75f, 0.2f, 1f);
            Gizmos.DrawSphere(end, 0.25f);
        }
    }
}
