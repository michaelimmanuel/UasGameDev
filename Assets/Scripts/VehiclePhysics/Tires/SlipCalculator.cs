using UnityEngine;

namespace VehiclePhysics
{
    /// <summary>
    /// Computes slip ratio and slip angle per wheel using SuspensionSystem data and a tire model.
    /// For now angular speed is approximated from linear speed (no wheel inertia).
    /// </summary>
    public class SlipCalculator : MonoBehaviour
    {
        public SuspensionSystem suspension;
        public SimpleTireModel tireModel;

        [Header("Epsilons")]
        [Tooltip("Min forward speed used as denominator to keep slip ratio stable at low speed.")]
        public float sEpsilon = 0.15f; // m/s - lowered for more responsive slip ratio
        [Tooltip("Min forward speed added for slip angle to keep steering responsive. Higher = less sensitive at low speed.")]
        public float aEpsilon = 2.0f; // m/s - increased to prevent false slip angles at low speed

        [Header("Outputs (debug)" )] 
        public float[] slipRatio;     // per wheel
        public float[] slipAngleDeg;  // per wheel
        public float[] muX;           // longitudinal μ
        public float[] muY;           // lateral μ
        public float[] appliedFx;     // final applied longitudinal force (N) per wheel - written by TireForcesApplier

        void Awake()
        {
            if (suspension == null) suspension = GetComponentInParent<SuspensionSystem>();
            AllocateArrays();
        }

        void OnValidate()
        {
            if (!Application.isPlaying) AllocateArrays();
        }

        void AllocateArrays()
        {
            if (suspension == null || suspension.wheels == null) return;
            int n = suspension.wheels.Length;
            slipRatio = new float[n];
            slipAngleDeg = new float[n];
            muX = new float[n];
            muY = new float[n];
            appliedFx = new float[n];
        }

        void FixedUpdate()
        {
            if (suspension == null || suspension.wheels == null) return;
            var states = suspension.CurrentWheelStates;
            if (states == null) return;
            int n = states.Length;
            if (slipRatio == null || slipRatio.Length != n) AllocateArrays();
            if (tireModel == null) return; // wait until model assigned

            // Reset combined slip tracking for this frame
            var simpleTireModel = tireModel as SimpleTireModel;
            if (simpleTireModel != null)
            {
                simpleTireModel.ResetSlipTracking();
            }

            for (int i = 0; i < n; i++)
            {
                var w = suspension.wheels[i];
                var ws = states[i];
                float radius = w.wheelRadius;

                if (!ws.grounded || radius <= 1e-4f)
                {
                    slipRatio[i] = 0f;
                    slipAngleDeg[i] = 0f;
                    muX[i] = 0f;
                    muY[i] = 0f;
                    appliedFx[i] = 0f;
                    continue;
                }

                // Prefer integrated per-wheel angular speed if available
                float omegaUse = ws.omega;
                if (Mathf.Abs(omegaUse) < 1e-4f)
                {
                    // Fallback approximation from kinematics
                    omegaUse = ws.Vx / Mathf.Max(radius, 1e-4f);
                }
                
                // Use higher epsilon at low speed to prevent false slip detection
                float speedFactor = Mathf.Clamp01(Mathf.Abs(ws.Vx) / 5f); // ramp from 0-5 m/s
                float effectiveEpsilon = Mathf.Lerp(sEpsilon * 3f, sEpsilon, speedFactor);
                float denom = Mathf.Max(Mathf.Abs(ws.Vx), effectiveEpsilon);
                
                float wheelSpeed = radius * omegaUse;
                float groundSpeed = ws.Vx;
                float s = (wheelSpeed - groundSpeed) / denom;
                
                // Apply small deadband to filter numerical noise during normal driving
                const float slipDeadband = 0.02f;
                if (Mathf.Abs(s) < slipDeadband)
                    s = 0f;
                
                slipRatio[i] = s;

                // Slip angle - use adaptive epsilon to avoid suppressing angle at low speed
                // effectiveAEpsilon scales from (aEpsilon*2) at very low speed down to (aEpsilon*0.5)
                // at higher speeds to keep steering responsive while retaining stability at near-zero speed.
                float speedFactorA = Mathf.Clamp01(Mathf.Abs(ws.Vx) / 5f); // ramp from 0..5 m/s
                float effectiveAEpsilon = Mathf.Lerp(aEpsilon * 2f, aEpsilon * 0.5f, speedFactorA);
                float alphaRad = Mathf.Atan2(ws.Vy, Mathf.Abs(ws.Vx) + effectiveAEpsilon);
                float alphaDeg = alphaRad * Mathf.Rad2Deg;
                slipAngleDeg[i] = alphaDeg;

                // μ values from tire model
                muX[i] = tireModel.MuX(s, ws.loadN, ws.isFront);
                muY[i] = tireModel.MuY(alphaDeg, ws.loadN, ws.isFront);
            }
        }
    }
}
