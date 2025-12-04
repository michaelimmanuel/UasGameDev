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

        [Header("Brake Input")]
        [Range(0f,1f)] public float brakeInput; // set by controller (0..1)

        [Header("Params")]
        [Tooltip("Maximum brake torque at full input per powered/braked wheel (N·m)")]
        public float maxBrakeTorque = 3000f;
        [Tooltip("Front brake bias (0..1). Remaining goes to rear.")]
        [Range(0f,1f)] public float frontBias = 0.65f;

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
                // Force opposing current forward velocity direction
                float dir = Mathf.Sign(states[i].Vx);
                float brakeForce = (torqueShare / radius) * dir; // positive if Vx positive
                wheelBrakeForce[i] = brakeForce;
            }

            // If linked to powertrain, reduce its FxRequested by brake force
            if (powertrain != null && powertrain.wheelFxRequested != null)
            {
                int m = Mathf.Min(states.Length, powertrain.wheelFxRequested.Length);
                m = Mathf.Min(m, wheelBrakeForce.Length);
                for (int i = 0; i < m; i++)
                {
                    // Subtract brake force: ensure sign opposes current Fx
                    powertrain.wheelFxRequested[i] -= wheelBrakeForce[i];
                }
            }
        }
    }
}
