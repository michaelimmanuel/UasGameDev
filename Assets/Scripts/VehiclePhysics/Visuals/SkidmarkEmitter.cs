using UnityEngine;

namespace VehiclePhysics.Visuals
{
    [AddComponentMenu("Vehicle Physics/Skidmark Emitter")]
    public class SkidmarkEmitter : MonoBehaviour
    {
        public VehiclePhysics.SuspensionSystem suspension;
        public VehiclePhysics.SlipCalculator slipCalc;
        public SkidmarkManager manager;
        public VehiclePhysics.PowertrainSystem powertrain; // optional: gate by throttle
        public VehiclePhysics.BrakeSystem brakes;          // optional: gate by braking/handbrake

        [Header("Emission Thresholds")] 
        [Tooltip("Slip ratio magnitude to START emitting marks (accel/brake). 0.3 = 30% wheelspin")]
        public float slipRatioStart = 0.30f;
        [Tooltip("Slip ratio magnitude to STOP emitting marks (hysteresis)")]
        public float slipRatioStop = 0.20f;
        [Tooltip("Slip angle (deg) magnitude to START emitting marks (cornering). 20+ = real sliding")]
        public float slipAngleStartDeg = 22f;
        [Tooltip("Slip angle (deg) to STOP emitting marks (hysteresis)")]
        public float slipAngleStopDeg = 15f;
        [Tooltip("Minimum local wheel speed (m/s) to allow marks")] public float minLocalSpeed = 2.0f;
        [Tooltip("Minimum wheel normal load (N) to allow marks")] public float minNormalLoad = 800f;
        [Tooltip("Minimum continuous time above threshold before starting a mark (s)")] public float minTimeAboveThreshold = 0.12f;

        [Header("Appearance")] 
        public float markWidth = 0.18f;
        public float minSegmentDistance = 0.12f;
        [Tooltip("Maximum distance of a single continuous skid streak (meters) before forcing a break")] public float maxStreakDistance = 10f;
        [Tooltip("How strong marks get as slip increases")] [Range(0.1f, 3f)] public float intensityScale = 0.9f;
        [Tooltip("Slip magnitude at which intensity reaches 1.0 (ratio)")] public float slipRatioFullIntensity = 0.30f;
        [Tooltip("Slip angle at which intensity reaches 1.0 (deg)")] public float slipAngleFullIntensity = 30f;
        [Tooltip("Reduces intensity at very low speed (0..1 multiplier at minLocalSpeed)")] [Range(0f,1f)] public float lowSpeedIntensityFactor = 0.35f;

        private Vector3[] _lastPos;
        private bool[] _emitting;
        private float[] _timeAbove;
        private float[] _streakDistance;

        void Reset()
        {
            if (suspension == null) suspension = GetComponentInParent<VehiclePhysics.SuspensionSystem>();
            if (slipCalc == null) slipCalc = GetComponentInParent<VehiclePhysics.SlipCalculator>();
            if (manager == null) manager = GetComponentInParent<SkidmarkManager>();
            if (powertrain == null) powertrain = GetComponentInParent<VehiclePhysics.PowertrainSystem>();
            if (brakes == null) brakes = GetComponentInParent<VehiclePhysics.BrakeSystem>();
        }

        void Awake()
        {
            if (suspension == null) suspension = GetComponentInParent<VehiclePhysics.SuspensionSystem>();
            if (slipCalc == null) slipCalc = GetComponentInParent<VehiclePhysics.SlipCalculator>();
            if (manager == null)
            {
                // Try to find in scene
                manager = Object.FindFirstObjectByType<SkidmarkManager>();
            }
            if (powertrain == null) powertrain = GetComponentInParent<VehiclePhysics.PowertrainSystem>();
            if (brakes == null) brakes = GetComponentInParent<VehiclePhysics.BrakeSystem>();
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
            if (_lastPos == null || _lastPos.Length != n) _lastPos = new Vector3[n];
            if (_emitting == null || _emitting.Length != n) _emitting = new bool[n];
            if (_timeAbove == null || _timeAbove.Length != n) _timeAbove = new float[n];
            if (_streakDistance == null || _streakDistance.Length != n) _streakDistance = new float[n];
        }

        void FixedUpdate()
        {
            if (suspension == null || slipCalc == null || manager == null) return;
            var states = suspension.CurrentWheelStates;
            if (states == null || suspension.wheels == null) return;

            for (int i = 0; i < suspension.wheels.Length; i++)
            {
                var w = suspension.wheels[i];
                var ws = states[i];
                if (!ws.grounded) { _emitting[i] = false; _timeAbove[i] = 0f; _streakDistance[i] = 0f; continue; }

                // Determine if we should emit based on slip
                float s = (slipCalc.slipRatio != null && slipCalc.slipRatio.Length > i) ? Mathf.Abs(slipCalc.slipRatio[i]) : 0f;
                float a = (slipCalc.slipAngleDeg != null && slipCalc.slipAngleDeg.Length > i) ? Mathf.Abs(slipCalc.slipAngleDeg[i]) : 0f;

                // Gating by speed and load
                float localSpeed = Mathf.Sqrt(ws.Vx * ws.Vx + ws.Vy * ws.Vy);
                bool speedOK = localSpeed >= minLocalSpeed;
                bool loadOK = ws.loadN >= minNormalLoad;

                bool aboveStart = (s >= slipRatioStart) || (a >= slipAngleStartDeg);
                bool aboveStop  = (s >= slipRatioStop)  || (a >= slipAngleStopDeg);

                // Accumulate time above start threshold
                if (aboveStart && speedOK && loadOK) _timeAbove[i] += Time.fixedDeltaTime; else _timeAbove[i] = 0f;

                bool emit;
                if (_emitting[i])
                {
                    // While emitting, require stay-above-stop and gating
                    emit = aboveStop && speedOK && loadOK;
                }
                else
                {
                    // To start, require sustained above-start + gating
                    emit = (_timeAbove[i] >= minTimeAboveThreshold) && speedOK && loadOK;
                }

                // Additional coasting guard: only emit when driver is actively causing slip
                // Require some throttle, brake, or handbrake, OR very high friction utilization.
                bool driverActive = false;
                if (powertrain != null) driverActive |= (powertrain.throttle > 0.08f);
                if (brakes != null)
                {
                    driverActive |= (brakes.brakeInput > 0.08f) || (brakes.handbrakeInput > 0.02f);
                }
                float util = 0f;
                if (slipCalc != null && slipCalc.muX != null && slipCalc.muY != null)
                {
                    // Rough utilization proxy: if slip beyond start on both axes, treat as high
                    util = Mathf.Max(s, a);
                }
                // Gate emission: require driverActive OR strong slip beyond start thresholds
                if (!driverActive && !(s >= slipRatioStart * 1.25f || a >= slipAngleStartDeg * 1.25f))
                {
                    emit = false;
                }

                // Current contact position slightly offset above ground
                Vector3 pos = ws.position + w.contactNormal.normalized * manager.normalOffset;
                if (!emit)
                {
                    _emitting[i] = false;
                    _streakDistance[i] = 0f;
                    _lastPos[i] = pos;
                    continue;
                }

                // Intensity based on slip magnitude
                float sr = Mathf.InverseLerp(slipRatioStart, Mathf.Max(slipRatioStart, slipRatioFullIntensity), s);
                float sa = Mathf.InverseLerp(slipAngleStartDeg, Mathf.Max(slipAngleStartDeg, slipAngleFullIntensity), a);
                float speedScale = Mathf.Lerp(lowSpeedIntensityFactor, 1f, Mathf.InverseLerp(minLocalSpeed, minLocalSpeed * 2f, localSpeed));
                float loadScale = Mathf.Clamp01(Mathf.InverseLerp(minNormalLoad, minNormalLoad * 2f, ws.loadN));
                float intensity = Mathf.Clamp01(Mathf.Max(sr, sa) * intensityScale * speedScale * loadScale);

                // Add segment if we have a previous
                Vector3 prev = _lastPos[i];
                float dist = (pos - prev).magnitude;
                if (_emitting[i] && dist >= minSegmentDistance)
                {
                    // Break long continuous streaks
                    float newStreak = _streakDistance[i] + dist;
                    if (newStreak > maxStreakDistance)
                    {
                        _emitting[i] = false; // force a break; will need to re-satisfy thresholds
                        _streakDistance[i] = 0f;
                        _lastPos[i] = pos;
                    }
                    else
                    {
                        manager.AddSegment(prev, pos, w.contactNormal, markWidth, intensity);
                        _lastPos[i] = pos;
                        _streakDistance[i] = newStreak;
                    }
                }
                else
                {
                    // Start emitting
                    _emitting[i] = true;
                    _lastPos[i] = pos;
                }
            }
        }
    }
}
