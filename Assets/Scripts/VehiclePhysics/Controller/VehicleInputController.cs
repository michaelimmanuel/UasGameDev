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

        [Header("Gear Selection")]
        public bool autoGear = true;
        public float upshiftRpm = 6200f;
        public float downshiftRpm = 1800f;

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
            float throttle = Mathf.Clamp01(axis);          // positive part
            float brake = Mathf.Clamp01(-axis);            // negative part

            powertrain.throttle = throttle;
            brakes.brakeInput = brake;

            if (autoGear && powertrain.engineCurve != null && powertrain.gearbox != null)
            {
                // Simple auto shift based on RPM thresholds
                if (powertrain.engineRpm > upshiftRpm && powertrain.currentGear < powertrain.gearbox.gearRatios.Length)
                    powertrain.currentGear++;
                else if (powertrain.engineRpm < downshiftRpm && powertrain.currentGear > 1)
                    powertrain.currentGear--;
            }
        }
    }
}
