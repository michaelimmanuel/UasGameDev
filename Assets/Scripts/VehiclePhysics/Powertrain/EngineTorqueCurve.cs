using UnityEngine;

namespace VehiclePhysics
{
    [CreateAssetMenu(menuName = "VehiclePhysics/Engine Torque Curve", fileName = "EngineTorqueCurve")]
    public class EngineTorqueCurve : ScriptableObject
    {
        [Header("Torque Curve")]
        [Tooltip("Torque vs RPM curve evaluated at full throttle.")]
        public AnimationCurve fullThrottleTorque = AnimationCurve.Linear(1000f, 180f, 7000f, 0f);

        [Header("Limits")]
        public float idleRpm = 900f;
        public float redlineRpm = 7000f;

        [Header("Scaling")]
        [Tooltip("Global scale for torque output.")]
        public float torqueScale = 1f;

        [Header("Engine Braking")]
        [Tooltip("Fraction of max full-throttle torque applied opposite wheels at zero throttle.")]
        [Range(0f,0.5f)] public float engineBrakeFraction = 0.08f;
        [Tooltip("Minimum engine braking RPM clamp.")]
        public float engineBrakeMinRpm = 1000f;

        public float Torque(float rpm, float throttle01)
        {
            rpm = Mathf.Clamp(rpm, idleRpm, redlineRpm);
            float baseTorque = fullThrottleTorque.Evaluate(rpm) * torqueScale;
            float t = Mathf.Clamp01(throttle01);
            return baseTorque * t;
        }

        public float EngineBrakeTorque(float rpm, float throttle01)
        {
            if (throttle01 > 0.05f) return 0f;
            rpm = Mathf.Max(rpm, engineBrakeMinRpm);
            float maxTorque = fullThrottleTorque.Evaluate(rpm) * torqueScale;
            return maxTorque * engineBrakeFraction; // acts opposite wheel rotation
        }

        // Factory presets for common engines. Values in Newton-meters.
        // BMW E30 M3 (S14B23) stock: 192 hp @ 6750 rpm, 230 Nm @ 4750 rpm.
        // Curve derived from period dyno averages (smoothed). Use torqueScale=1.
        public static AnimationCurve CreateE30M3S14B23StockCurve()
        {
            return new AnimationCurve(
                new Keyframe(900f, 110f),
                new Keyframe(1500f, 130f),
                new Keyframe(2000f, 145f),
                new Keyframe(2500f, 160f),
                new Keyframe(3000f, 175f),
                new Keyframe(3500f, 190f),
                new Keyframe(4000f, 205f),
                new Keyframe(4500f, 222f),
                new Keyframe(4750f, 230f), // peak torque
                new Keyframe(5000f, 228f),
                new Keyframe(5500f, 222f),
                new Keyframe(6000f, 215f),
                new Keyframe(6500f, 208f),
                new Keyframe(6750f, 202f), // power peak ~192 hp
                new Keyframe(7000f, 195f)
            );
        }

        // BMW E30 M3 Evo2 (S14B23 upgraded): ~220 hp @ 7000 rpm, 240 Nm @ 4750 rpm (approx).
        public static AnimationCurve CreateE30M3S14B23Evo2Curve()
        {
            return new AnimationCurve(
                new Keyframe(900f, 115f),
                new Keyframe(1500f, 135f),
                new Keyframe(2000f, 150f),
                new Keyframe(2500f, 168f),
                new Keyframe(3000f, 182f),
                new Keyframe(3500f, 198f),
                new Keyframe(4000f, 212f),
                new Keyframe(4500f, 232f),
                new Keyframe(4750f, 240f), // peak torque
                new Keyframe(5000f, 238f),
                new Keyframe(5500f, 232f),
                new Keyframe(6000f, 225f),
                new Keyframe(6500f, 218f),
                new Keyframe(6750f, 212f),
                new Keyframe(7000f, 208f) // power peak ~220 hp
            );
        }
    }
}
