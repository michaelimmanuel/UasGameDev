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

        [Header("Inputs (set by controller)")]
        [Range(0f,1f)] public float throttle;
        [Tooltip("Brake torque input (0..1) forwarded to BrakeSystem if present")] [Range(0f,1f)] public float brake;

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
            float motionSign = Mathf.Sign(avgVx);
            if (Mathf.Abs(avgVx) < 0.05f) motionSign = 0f; // near rest: no directional brake torque (will rely on damping elsewhere)

            // Apply torque: driving torque always positive forward, braking torque opposes motion.
            float wheelTorque = (engineTorque - engineBrakeTorque * motionSign) * ratio * gearbox.efficiency;
            float perWheelTorque = wheelTorque / countPowered; // open diff distribution

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
