using UnityEngine;

namespace VehiclePhysics
{
    /// <summary>
    /// Minimal steering controller: maps Unity input axis "Horizontal" to front wheel steer angles.
    /// Includes mild speed-based reduction so steering is less sensitive at high speed.
    /// </summary>
    public class SteeringInput : MonoBehaviour
    {
        public SuspensionSystem suspension;
        public Rigidbody rb;

        [Header("Steering")]
        [Tooltip("Maximum steer angle at low speed (degrees)")]
        public float maxSteerDeg = 30f;

        [Tooltip("Speed (m/s) at which steering reduces to minFactor of max.")]
        public float speedRef = 30f; // ~108 km/h

        [Tooltip("Minimum fraction of steering kept at high speed (0..1)")]
        [Range(0.1f, 1f)] public float minFactor = 0.5f;

        [Tooltip("Steering speed in degrees per second (framerate-independent)")]
        public float steerSpeed = 200f;

        [Tooltip("Return-to-center speed in degrees per second when no input")]
        public float returnSpeed = 300f;

        private float _currentSteerDeg;

        void Reset()
        {
            if (suspension == null) suspension = GetComponentInParent<SuspensionSystem>();
            if (rb == null) rb = GetComponentInParent<Rigidbody>();
        }

        void Awake()
        {
            if (suspension == null) suspension = GetComponentInParent<SuspensionSystem>();
            if (rb == null) rb = GetComponentInParent<Rigidbody>();
        }

        void Update()
        {
            if (suspension == null) return;

            float steerInput = Input.GetAxisRaw("Horizontal");
            float speed = rb != null ? rb.linearVelocity.magnitude : 0f;

            // Speed-based reduction: linear from 1 at 0 m/s to minFactor at >= speedRef
            float factor = 1f - Mathf.Clamp01(speed / Mathf.Max(0.01f, speedRef)) * (1f - minFactor);
            float targetSteer = steerInput * maxSteerDeg * factor;

            // Framerate-independent steering with MoveTowards
            float currentSpeed = (Mathf.Abs(steerInput) > 0.01f) ? steerSpeed : returnSpeed;
            _currentSteerDeg = Mathf.MoveTowards(_currentSteerDeg, targetSteer, currentSpeed * Time.deltaTime);

            // Apply to front wheels only
            if (suspension.wheels == null) return;
            for (int i = 0; i < suspension.wheels.Length; i++)
            {
                var w = suspension.wheels[i];
                if (w != null && w.isFront)
                {
                    w.steerDeg = _currentSteerDeg;
                }
            }
        }
    }
}
