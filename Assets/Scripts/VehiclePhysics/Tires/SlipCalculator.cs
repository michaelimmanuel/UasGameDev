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
        public float sEpsilon = 0.5f; // m/s
        [Tooltip("Min forward speed added for slip angle to keep steering responsive.")]
        public float aEpsilon = 0.5f; // m/s

        [Header("Outputs (debug)")] 
        public float[] slipRatio;     // per wheel
        public float[] slipAngleDeg;  // per wheel
        public float[] muX;           // longitudinal μ
        public float[] muY;           // lateral μ

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
        }

        void FixedUpdate()
        {
            if (suspension == null || suspension.wheels == null) return;
            var states = suspension.CurrentWheelStates;
            if (states == null) return;
            int n = states.Length;
            if (slipRatio == null || slipRatio.Length != n) AllocateArrays();
            if (tireModel == null) return; // wait until model assigned

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
                    continue;
                }

                // Approximate wheel angular speed from linear forward velocity (no torque yet)
                float omegaApprox = ws.Vx / Mathf.Max(radius, 1e-4f); // rad/s
                float denom = Mathf.Max(Mathf.Abs(ws.Vx), sEpsilon);
                float s = ((radius * omegaApprox) - ws.Vx) / denom; // currently ~0 until torque model added
                slipRatio[i] = s;

                // Slip angle
                float alphaRad = Mathf.Atan2(ws.Vy, Mathf.Abs(ws.Vx) + aEpsilon);
                float alphaDeg = alphaRad * Mathf.Rad2Deg;
                slipAngleDeg[i] = alphaDeg;

                // μ values from tire model
                muX[i] = tireModel.MuX(s, ws.loadN, ws.isFront);
                muY[i] = tireModel.MuY(alphaDeg, ws.loadN, ws.isFront);
            }
        }

        void OnGUI()
        {
            // Simple telemetry overlay (top-left)
            if (slipRatio == null) return;
            const int width = 260;
            GUILayout.BeginArea(new Rect(10, 10, width, 400), GUI.skin.box);
            GUILayout.Label("Wheel Telemetry (Task 2)");
            if (suspension != null && suspension.wheels != null)
            {
                for (int i = 0; i < suspension.wheels.Length; i++)
                {
                    var ws = suspension.CurrentWheelStates[i];
                    GUILayout.Label($"[{i}] N={ws.loadN:F0} s={slipRatio[i]:F3} α={slipAngleDeg[i]:F1}° μx={muX[i]:F2} μy={muY[i]:F2}");
                }
            }
            GUILayout.EndArea();
        }
    }
}
