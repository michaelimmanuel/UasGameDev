using UnityEngine;

namespace VehiclePhysics
{
    /// <summary>
    /// Reads player input (Horizontal / Vertical) and updates steering, powertrain, and brakes.
    /// Vertical > 0 = throttle, Vertical < 0 = brake.
    /// </summary>
    public class VehicleInputController : MonoBehaviour
    {
        public SteeringInput steering;
        public PowertrainSystem powertrain;
        public BrakeSystem brakes;

        [Header("Input Axes")]
        public string steerAxis = "Horizontal";
        public string throttleBrakeAxis = "Vertical";
        [Tooltip("Input axis or button for handbrake (0..1)")]
        public string handbrakeAxis = "Jump"; // default maps to space bar if using Input Manager

        [Header("Throttle Smoothing")] 
        [Tooltip("Enable progressive throttle to reduce sudden wheelspin")] 
        public bool smoothThrottle = true;
        [Tooltip("Non-linear response: 1=linear, >1=progressive, <1=aggressive")] 
        public float throttleCurveExponent = 1.6f;
        [Tooltip("Max increase per second from 0..1")] 
        public float throttleRisePerSecond = 2.0f; // 0->1 in 0.5s
        [Tooltip("Max decrease per second from 0..1")] 
        public float throttleFallPerSecond = 6.0f; // faster release
        [Tooltip("Ignore tiny inputs below this level")] 
        [Range(0f,0.2f)] public float throttleDeadzone = 0.04f;

        [Header("Gear Selection")]
        public bool autoGear = true;
        public float upshiftRpm = 6200f;
        public float downshiftRpm = 1800f;

        private float _smoothedThrottle;
            // Track last fixed time a gear change occurred to avoid multiple shifts per physics step
            private float _lastShiftFixedTime = -1f;

        void Reset()
        {
            if (steering == null) steering = GetComponentInParent<SteeringInput>();
            if (powertrain == null) powertrain = GetComponentInParent<PowertrainSystem>();
            if (brakes == null) brakes = GetComponentInParent<BrakeSystem>();
        }

        void Update()
        {
            if (steering != null)
            {
                // SteeringInput already handles Horizontal in its own Update; optional duplication skipped.
            }
            if (powertrain == null || brakes == null) return;

            float axis = Input.GetAxis(throttleBrakeAxis);
            float rawThrottle = Mathf.Clamp01(axis);          // positive part
            float brake = Mathf.Clamp01(-axis);               // negative part
            float handbrake = 0f;
            // Try button-like axis first; fall back to GetButton
            if (!string.IsNullOrEmpty(handbrakeAxis))
            {
                // For standard Input Manager, button returns 1 when pressed via GetAxisRaw
                handbrake = Mathf.Clamp01(Input.GetAxisRaw(handbrakeAxis));
                if (handbrake <= 0f && Input.GetButton(handbrakeAxis)) handbrake = 1f;
            }

            // Throttle shaping
            float targetThrottle = rawThrottle;
            if (targetThrottle < throttleDeadzone) targetThrottle = 0f;
            if (throttleCurveExponent > 0.001f && Mathf.Abs(targetThrottle) > 0f)
                targetThrottle = Mathf.Pow(targetThrottle, Mathf.Max(0.05f, throttleCurveExponent));

            if (!smoothThrottle)
            {
                _smoothedThrottle = targetThrottle;
            }
            else
            {
                float dt = Time.deltaTime;
                float delta = targetThrottle - _smoothedThrottle;
                float maxStep = (delta >= 0f ? throttleRisePerSecond : throttleFallPerSecond) * dt;
                delta = Mathf.Clamp(delta, -maxStep, maxStep);
                _smoothedThrottle += delta;
            }

            powertrain.throttle = Mathf.Clamp01(_smoothedThrottle);
            brakes.brakeInput = brake;
            brakes.handbrakeInput = handbrake;

            if (autoGear && powertrain.engineCurve != null && powertrain.gearbox != null)
            {
                // Simple auto shift based on RPM thresholds
                // Only allow one gear change per physics step to prevent skipping when Update()
                // runs multiple times between FixedUpdate ticks.
                if (Time.fixedTime > _lastShiftFixedTime)
                {
                    if (powertrain.engineRpm > upshiftRpm && powertrain.currentGear < powertrain.gearbox.gearRatios.Length)
                    {
                        powertrain.currentGear++;
                        _lastShiftFixedTime = Time.fixedTime;
                    }
                    else if (powertrain.engineRpm < downshiftRpm && powertrain.currentGear > 1)
                    {
                        powertrain.currentGear--;
                        _lastShiftFixedTime = Time.fixedTime;
                    }
                }
            }
        }
    }
}
