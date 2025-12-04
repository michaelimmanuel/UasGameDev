using UnityEngine;

namespace VehiclePhysics
{
    /// <summary>
    /// Task 3: Friction Ellipse and per-wheel tire force application.
    /// Uses SlipCalculator μ values and SuspensionSystem wheel states.
    /// Requests Fx/Fy, clamps to ellipse, and applies AddForceAtPosition per wheel.
    ///
    /// For now:
    /// - Fx_req: derived from DebugNudge (approx) by projecting car forward onto wheel forward.
    /// - Fy_req: passive stabilizer proportional to -Vy for immediate lateral resistance.
    /// Replace Fx_req with powertrain and Fy_req with controller in later tasks.
    /// </summary>
    public class TireForcesApplier : MonoBehaviour
    {
        public Rigidbody rb;
        public SuspensionSystem suspension;
        public SlipCalculator slipCalc;
        public PowertrainSystem powertrain; // optional for Fx_req
        public BrakeSystem brakeSystem;     // optional (already reduces powertrain Fx)

        [Header("Requests")]
        [Tooltip("Longitudinal velocity damping fallback when no powertrain: Fx_req = -k * Vx.")]
        public float longitudinalDampingK = 200f; // N per (m/s)

        [Header("Lateral Force from Slip Angle")]
        [Tooltip("Slip angle at which lateral force reaches μy*N (deg). Typical street tire 6–10°.")]
        public float alphaPeakDeg = 8f;
        [Tooltip("Blend low-speed lateral damping to avoid jitter when |V| < holdSpeed.")]
        public float lowSpeedLateralK = 600f;
        [Tooltip("Hard cap: if local speed at contact is below this, suppress lateral force (Fy=0) to prevent big arrows at standstill.")]
        public float lateralZeroSpeed = 0.08f; // m/s
        [Tooltip("When local speed < this, cap |Fy| to maxPerSpeed * speed to avoid long residual arrows while stopping.")]
        public float lateralCapSpeed = 1.0f; // m/s
        [Tooltip("Max |Fy| at low speed = maxPerSpeed * speed.")]
        public float lateralMaxPerSpeed = 1500f; // N per (m/s)

        [Header("Rest Hold (optional)")]
        [Tooltip("Boost damping near zero speed to reduce creeping on slight slopes.")]
        public bool holdAtRest = true;
        [Tooltip("Speed below which hold boost is applied (m/s).")]
        public float holdSpeed = 0.05f;
        [Tooltip("Multiplier applied to K when |V| < holdSpeed.")]
        public float holdBoost = 4f;

        [Header("Debug/Telemetry")]
        public bool showUtilizationHud = true;
        private float[] _utilization;
        [Tooltip("Draw per-wheel force arrows in Scene view")] public bool drawForceGizmos = true;
        private Vector3[] _lastForces; // world-space
        private Vector3[] _lastPositions;

        void Reset()
        {
            if (rb == null) rb = GetComponentInParent<Rigidbody>();
            if (suspension == null) suspension = GetComponentInParent<SuspensionSystem>();
            if (slipCalc == null) slipCalc = GetComponentInParent<SlipCalculator>();
            if (powertrain == null) powertrain = GetComponentInParent<PowertrainSystem>();
            if (brakeSystem == null) brakeSystem = GetComponentInParent<BrakeSystem>();
        }

        void Awake()
        {
            if (rb == null) rb = GetComponentInParent<Rigidbody>();
            if (suspension == null) suspension = GetComponentInParent<SuspensionSystem>();
            if (slipCalc == null) slipCalc = GetComponentInParent<SlipCalculator>();
            if (powertrain == null) powertrain = GetComponentInParent<PowertrainSystem>();
            if (brakeSystem == null) brakeSystem = GetComponentInParent<BrakeSystem>();
            Allocate();
        }

        void OnValidate()
        {
            if (!Application.isPlaying) Allocate();
        }

        void Allocate()
        {
            int n = (suspension != null && suspension.wheels != null) ? suspension.wheels.Length : 0;
            if (n <= 0) return;
            _utilization = new float[n];
            _lastForces = new Vector3[n];
            _lastPositions = new Vector3[n];
        }

        void FixedUpdate()
        {
            if (rb == null || suspension == null || slipCalc == null) return;
            var states = suspension.CurrentWheelStates;
            if (states == null) return;

            // Estimate any external forward request (e.g., DebugNudge) as a body force magnitude along car forward
            // We use rb.velocity change indirectly; simpler: distribute a placeholder request based on Vx sign.
            for (int i = 0; i < states.Length; i++)
            {
                var w = suspension.wheels[i];
                var ws = states[i];

                // Wheel frame
                Quaternion steerRot = Quaternion.AngleAxis(w.steerDeg, suspension.transform.up);
                Vector3 wheelFwd = steerRot * w.attach.forward;
                Vector3 wheelRight = Vector3.Cross(suspension.transform.up, wheelFwd).normalized;

                float N = ws.loadN;
                if (!ws.grounded || N <= 1e-3f)
                {
                    _utilization[i] = 0f;
                    continue;
                }

                // Limits from μ
                float Fx_lim = slipCalc.muX != null && slipCalc.muX.Length > i ? slipCalc.muX[i] * N : 0f;
                float Fy_lim = slipCalc.muY != null && slipCalc.muY.Length > i ? slipCalc.muY[i] * N : 0f;

                // Longitudinal request: prefer powertrain output if available
                float Fx_req;
                if (powertrain != null && powertrain.wheelFxRequested != null && powertrain.wheelFxRequested.Length == states.Length)
                {
                    Fx_req = Mathf.Clamp(powertrain.wheelFxRequested[i], -Fx_lim, Fx_lim);
                }
                else
                {
                    // Fallback damping model
                    float kx = longitudinalDampingK;
                    if (holdAtRest && Mathf.Abs(ws.Vx) < holdSpeed && Mathf.Abs(ws.Vy) < holdSpeed)
                        kx *= holdBoost;
                    Fx_req = Mathf.Clamp(-kx * ws.Vx, -Fx_lim, Fx_lim);
                }

                // Lateral request from slip angle: scale toward limit based on α/α_peak
                float Fy_req;
                float alphaDeg = (slipCalc.slipAngleDeg != null && slipCalc.slipAngleDeg.Length > i) ? slipCalc.slipAngleDeg[i] : 0f;
                // Local speed magnitude at contact
                float localSpeed = Mathf.Sqrt(ws.Vx * ws.Vx + ws.Vy * ws.Vy);
                if (localSpeed < lateralZeroSpeed)
                {
                    // At near standstill, suppress lateral force entirely to avoid spurious torque couples
                    Fy_req = 0f;
                }
                else if (Mathf.Abs(ws.Vx) < holdSpeed && Mathf.Abs(ws.Vy) < holdSpeed)
                {
                    // At near rest, rely on damping to avoid twitching
                    float ky = lowSpeedLateralK * (holdAtRest ? holdBoost : 1f);
                    Fy_req = Mathf.Clamp(-ky * ws.Vy, -Fy_lim, Fy_lim);
                }
                else
                {
                    // Lateral force should oppose slip angle direction
                    float alphaNorm = Mathf.Clamp(alphaDeg / Mathf.Max(1e-3f, alphaPeakDeg), -1f, 1f);
                    Fy_req = Mathf.Clamp(-alphaNorm * Fy_lim, -Fy_lim, Fy_lim);
                    // If speed is low but not zero, cap magnitude to avoid long arrows during deceleration
                    if (localSpeed < lateralCapSpeed)
                    {
                        float cap = Mathf.Clamp(lateralMaxPerSpeed * localSpeed, 0f, Fy_lim);
                        Fy_req = Mathf.Clamp(Fy_req, -cap, cap);
                    }
                }

                // Clamp via ellipse
                Vector2 F = ClampEllipse(new Vector2(Fx_req, Fy_req), new Vector2(Fx_lim, Fy_lim));
                float u = Utilization(F, Fx_lim, Fy_lim);
                _utilization[i] = u;

                // Apply forces at contact
                Vector3 force = wheelFwd * F.x + wheelRight * F.y;
                rb.AddForceAtPosition(force, ws.position, ForceMode.Force);
                _lastForces[i] = force;
                _lastPositions[i] = ws.position;
            }
        }

        static Vector2 ClampEllipse(Vector2 Freq, Vector2 Flim)
        {
            float fxLim = Mathf.Max(1e-4f, Flim.x);
            float fyLim = Mathf.Max(1e-4f, Flim.y);
            float nx = Freq.x / fxLim;
            float ny = Freq.y / fyLim;
            float u = Mathf.Sqrt(nx * nx + ny * ny);
            if (u <= 1f) return Freq;
            float scale = 1f / u;
            return new Vector2(Freq.x * scale, Freq.y * scale);
        }

        static float Utilization(Vector2 F, float Fx_lim, float Fy_lim)
        {
            float fxLim = Mathf.Max(1e-4f, Fx_lim);
            float fyLim = Mathf.Max(1e-4f, Fy_lim);
            float nx = F.x / fxLim;
            float ny = F.y / fyLim;
            return Mathf.Sqrt(nx * nx + ny * ny);
        }

        void OnGUI()
        {
            if (!showUtilizationHud || _utilization == null) return;
            GUILayout.BeginArea(new Rect(280, 10, 200, 200), GUI.skin.box);
            GUILayout.Label("Friction Ellipse Utilization");
            for (int i = 0; i < _utilization.Length; i++)
            {
                GUILayout.Label($"[{i}] u={_utilization[i]:F2}");
            }
            GUILayout.EndArea();
        }

        void OnDrawGizmos()
        {
            if (!drawForceGizmos || _lastForces == null || _lastPositions == null) return;
            for (int i = 0; i < _lastForces.Length; i++)
            {
                Vector3 pos = _lastPositions[i];
                Vector3 F = _lastForces[i];
                if (F == Vector3.zero || pos == Vector3.zero) continue;
                Gizmos.color = Color.yellow;
                // scale length down for readability: 1 meter per 2000 N
                float scale = 1f / 2000f;
                Vector3 end = pos + F * scale;
                Gizmos.DrawLine(pos, end);
                // arrow head
                Vector3 dir = (end - pos).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
                float headLen = 0.2f;
                Gizmos.DrawLine(end, end - (dir + right) * headLen * 0.5f);
                Gizmos.DrawLine(end, end - (dir - right) * headLen * 0.5f);
            }
        }
    }
}
