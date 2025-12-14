using UnityEngine;

namespace VehiclePhysics
{
    /// <summary>
    /// Simple brake system applying brake torque based on input and bias across wheels.
    /// Outputs per-wheel brake force request (converted later to Fx_req reduction).
    /// </summary>
    public class BrakeSystem : MonoBehaviour
    {
        public SuspensionSystem suspension;
        public PowertrainSystem powertrain; // optional reference to reduce wheelFxRequested
        [Header("Audio (optional)")]
        public SimpleRPMBankPlayer rpmBankPlayer;
        public CarAudioController carAudioController;
        public ThreeBandEnginePlayer threeBandPlayer;

        [Header("Brake Input")]
        [Range(0f,1f)] public float brakeInput;    // service brake (0..1)
        [Range(0f,1f)] public float handbrakeInput; // handbrake (0..1) rear-only

        [Header("Params")]
        [Tooltip("Maximum brake torque at full input per powered/braked wheel (N·m)")]
        public float maxBrakeTorque = 3000f;
        [Tooltip("Front brake bias (0..1). Remaining goes to rear.")]
        [Range(0f,1f)] public float frontBias = 0.65f;
        [Tooltip("Maximum handbrake torque applied to rear wheels (N·m)")]
        public float maxHandbrakeTorque = 12000f;
        [Tooltip("When true, handbrake ignores frontBias and applies only to rear wheels.")]
        public bool handbrakeRearOnly = true;

        [Header("Outputs")]
        public float[] wheelBrakeForce; // longitudinal force opposing motion

        void Awake()
        {
            if (suspension == null) suspension = GetComponentInParent<SuspensionSystem>();
            if (powertrain == null) powertrain = GetComponentInParent<PowertrainSystem>();
        }

        void FixedUpdate()
        {
            if (suspension == null) return;
            var states = suspension.CurrentWheelStates;
            if (states == null) return;
            if (wheelBrakeForce == null || wheelBrakeForce.Length != states.Length)
                wheelBrakeForce = new float[states.Length];

            float frontCount = 0f; float rearCount = 0f;
            for (int i = 0; i < suspension.wheels.Length; i++)
            {
                if (suspension.wheels[i].isFront) frontCount++; else rearCount++;
            }
            frontCount = Mathf.Max(1f, frontCount);
            rearCount = Mathf.Max(1f, rearCount);

            float totalBrakeTorque = maxBrakeTorque * brakeInput;
            float frontTorqueTotal = totalBrakeTorque * frontBias;
            float rearTorqueTotal  = totalBrakeTorque * (1f - frontBias);

            for (int i = 0; i < states.Length; i++)
            {
                var w = suspension.wheels[i];
                float radius = Mathf.Max(0.05f, w.wheelRadius);
                float torqueShare = w.isFront ? (frontTorqueTotal / frontCount) : (rearTorqueTotal / rearCount);

                // Add handbrake torque to rear wheels
                if (handbrakeInput > 0f && (!handbrakeRearOnly || !w.isFront))
                {
                    float hbPerWheel = (maxHandbrakeTorque * handbrakeInput) / Mathf.Max(1f, rearCount);
                    if (!w.isFront) torqueShare += hbPerWheel; // rear gets HB; fronts get none if rearOnly
                }
                // Brake force is a positive magnitude (torque / radius)
                // WheelDynamics will apply it to oppose wheel rotation
                float brakeForce = torqueShare / radius;
                wheelBrakeForce[i] = brakeForce;
            }
            // Forward telemetry to optional audio components (powertrain contains engineRpm/throttle)
            if (powertrain != null)
            {
                float rpm = powertrain.engineRpm;
                float thr = powertrain.throttle;
                int g = powertrain.currentGear;

                // approximate vehicle speed from wheel states (average absolute Vx)
                float sumVx = 0f; int cnt = 0;
                for (int i = 0; i < states.Length; i++) { sumVx += Mathf.Abs(states[i].Vx); cnt++; }
                float speed = cnt > 0 ? sumVx / cnt : 0f;

                if (rpmBankPlayer != null) rpmBankPlayer.SetRPM(rpm);
                if (carAudioController != null) carAudioController.SetTelemetry(rpm, thr, 0f, g, speed);
                if (threeBandPlayer != null) threeBandPlayer.SetTelemetry(rpm, thr, 0f, g, speed);
            }
        }
    }
}
