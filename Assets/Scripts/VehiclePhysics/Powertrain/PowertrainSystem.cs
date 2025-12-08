using UnityEngine;

namespace VehiclePhysics
{
    /// <summary>
    /// Computes engine RPM from average powered wheel forward speed, evaluates torque, distributes wheel forces.
    /// Simplified: no clutch/differential slip; wheel angular speed inferred from Vx (no inertia yet).
    /// </summary>
    public class PowertrainSystem : MonoBehaviour
    {
        public SuspensionSystem suspension;
        public EngineTorqueCurve engineCurve;
        public GearboxData gearbox;
        public SlipCalculator slipCalc; // optional: for traction assist

        [Header("Inputs (set by controller)")]
        [Range(0f,1f)] public float throttle;
        [Tooltip("Brake torque input (0..1) forwarded to BrakeSystem if present")] [Range(0f,1f)] public float brake;

        [Header("Engine Braking")]
        [Tooltip("Speed scale (m/s) for smoothing engine braking sign near rest. Larger = softer around 0.")]
        public float engineBrakeSpeedEpsilon = 0.3f;

        [Header("Traction Assist")]
        [Tooltip("Limit drive force to available μx·N to reduce launch wheelspin.")]
        public bool enableTractionAssist = true;
        [Tooltip("Blend factor for assist (0=no assist, 1=full cap to μx·N).")]
        [Range(0f,1f)] public float tractionAssistStrength = 1f;

        [Header("State")]
        public int currentGear = 1; // 1-based user view; internally index = currentGear-1
        public float engineRpm;
        public float engineTorque; // output (positive)
        public float engineBrakeTorque; // (positive, applied opposite wheel rotation)

        [Header("Wheel Requests")]
        public float[] wheelFxRequested; // longitudinal force BEFORE ellipse

        public float wheelRadius = 0.34f; // fallback if wheel data missing

        void Awake()
        {
            if (suspension == null) suspension = GetComponentInParent<SuspensionSystem>();
            if (slipCalc == null) slipCalc = GetComponentInParent<SlipCalculator>();
        }

        void FixedUpdate()
        {
            if (suspension == null || engineCurve == null || gearbox == null) return;
            var states = suspension.CurrentWheelStates;
            if (states == null) return;
            if (wheelFxRequested == null || wheelFxRequested.Length != states.Length)
                wheelFxRequested = new float[states.Length];

            float ratio;
            if (gearbox.gearRatios == null || gearbox.gearRatios.Length == 0)
            {
                // Fallback: use only final drive if no gears defined
                ratio = gearbox.finalDrive;
            }
            else
            {
                int gearIndex = Mathf.Clamp(currentGear - 1, 0, gearbox.gearRatios.Length - 1);
                ratio = gearbox.CurrentRatio(gearIndex);
            }

            // Average powered wheel forward speed
            float sumVx = 0f; int countPowered = 0;
            for (int i = 0; i < states.Length; i++)
            {
                if (suspension.wheels[i].isPowered)
                {
                    sumVx += states[i].Vx;
                    countPowered++;
                }
            }
            if (countPowered == 0)
            {
                // Ensure array exists but zero out requests when no powered wheels
                if (wheelFxRequested == null || wheelFxRequested.Length != states.Length)
                    wheelFxRequested = new float[states.Length];
                for (int i = 0; i < wheelFxRequested.Length; i++) wheelFxRequested[i] = 0f;
                return;
            }
            float avgVx = sumVx / countPowered;
            float r = wheelRadius > 0 ? wheelRadius : suspension.wheels[0].wheelRadius;
            r = Mathf.Max(r, 0.05f);

            // RPM from linear speed (simplified) RPM = (Vx / (2πR)) * ratio * 60
            engineRpm = (avgVx / (2f * Mathf.PI * r)) * ratio * 60f;
            engineRpm = Mathf.Max(engineCurve.idleRpm, engineRpm);

            engineTorque = engineCurve.Torque(engineRpm, throttle);
            engineBrakeTorque = engineCurve.EngineBrakeTorque(engineRpm, throttle);

            // Determine motion direction for engine braking so it always opposes current wheel motion.
            // Smooth directional factor based on average powered-wheel forward speed.
            float eps = Mathf.Max(1e-3f, engineBrakeSpeedEpsilon);
            float motionFactor = Mathf.Clamp(avgVx / eps, -1f, 1f);

            // Apply torque: 
            // - engineTorque is always positive (forward acceleration when throttle > 0)
            // - engineBrakeTorque should oppose motion direction (slow down the car)
            // When moving forward (motionFactor > 0): subtract engine brake to slow down
            // When moving backward (motionFactor < 0): add engine brake to slow down (bring toward zero)
            float wheelTorque = (engineTorque - engineBrakeTorque * motionFactor) * ratio * gearbox.efficiency;
            
            // When no throttle and nearly stationary, zero out to prevent any creep
            if (throttle < 0.01f && Mathf.Abs(avgVx) < 0.5f)
            {
                wheelTorque = 0f;
            }
            
            float perWheelTorque = wheelTorque / countPowered; // open diff distribution

            // Traction assist: cap drive force by μx·N per powered wheel if data available
            if (enableTractionAssist && slipCalc != null && slipCalc.muX != null && slipCalc.muX.Length == states.Length)
            {
                float rEff = r;
                // Compute per-wheel requested force from torque distribution
                float scale = 1f;
                for (int i = 0; i < states.Length; i++)
                {
                    if (!suspension.wheels[i].isPowered) continue;
                    float Fx_req = perWheelTorque / rEff;
                    float N = states[i].loadN;
                    float Fx_lim = Mathf.Max(0f, slipCalc.muX[i] * N);
                    if (Fx_req > 1e-4f && Fx_lim > 0f)
                    {
                        float s = Fx_lim / Fx_req;
                        scale = Mathf.Min(scale, s);
                    }
                }
                // Blend assist
                scale = Mathf.Lerp(1f, scale, tractionAssistStrength);
                wheelTorque *= scale;
                perWheelTorque = wheelTorque / countPowered;
            }

            // Convert to force: F = T / R
            for (int i = 0; i < states.Length; i++)
            {
                if (suspension.wheels[i].isPowered)
                {
                    float Fx_req = perWheelTorque / r;
                    wheelFxRequested[i] = Fx_req; // will be reduced by brakes below
                }
                else
                {
                    wheelFxRequested[i] = 0f;
                }
            }
        }
    }
}
