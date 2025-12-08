using UnityEngine;

namespace VehiclePhysics
{
    [CreateAssetMenu(menuName = "VehiclePhysics/Tire Curve Data", fileName = "TireCurveData")]
    public class TireCurveData : ScriptableObject
    {
        [Header("Longitudinal μx Curves (vs slip ratio)")] 
        [Tooltip("Front longitudinal friction vs slip ratio (U-shaped/Pacejka-like: rise → peak → plateau/decay)")] 
        public AnimationCurve muXFront = CreateDefaultMuXFront();
        [Tooltip("Rear longitudinal friction vs slip ratio (usually a bit higher for drift bias)")] 
        public AnimationCurve muXRear  = CreateDefaultMuXRear();

        [Header("Lateral μy Curves (vs slip angle deg)")] 
        [Tooltip("Front lateral friction vs slip angle (nonlinear: near-linear → peak ~10° → drift plateau")]
        public AnimationCurve muYFront = CreateDefaultMuYFront();
        [Tooltip("Rear lateral friction vs slip angle (bias for drift plateau)")] 
        public AnimationCurve muYRear  = CreateDefaultMuYRear();

        [Header("Load Sensitivity")] 
        [Tooltip("Coefficient determining how μ decreases with higher load.")] 
        [Range(0f, 0.3f)] public float loadSensitivity = 0.1f;
        [Tooltip("Reference load per wheel (N) used for scaling μ.")] 
        public float referenceLoad = 2500f;
        [Tooltip("Minimum μ after load scaling.")]
        public float minMu = 0.2f;
        [Tooltip("Maximum μ clamp after scaling (realistic tires: 0.8-1.3).")]
        public float maxMu = 1.3f;        // Factory defaults (U-shaped / nonlinear curves)
        public static AnimationCurve CreateDefaultMuXFront()
        {
            // s: 0 → 0.35, μ: 0 → ~1.2, peak ~0.12–0.15, gentle plateau
            return new AnimationCurve(
                new Keyframe(0.00f, 0.00f, 8f, 8f),
                new Keyframe(0.03f, 0.35f, 3f, 3f),
                new Keyframe(0.08f, 0.85f, 2f, 2f),
                new Keyframe(0.12f, 1.10f, 1f, 0.5f), // near peak
                new Keyframe(0.18f, 1.05f, 0f, 0f),
                new Keyframe(0.30f, 0.95f, -0.5f, -0.5f),
                new Keyframe(0.50f, 0.90f, 0f, 0f)
            );
        }

        public static AnimationCurve CreateDefaultMuXRear()
        {
            // Slightly higher peak and plateau for rear
            return new AnimationCurve(
                new Keyframe(0.00f, 0.00f, 8f, 8f),
                new Keyframe(0.03f, 0.40f, 3f, 3f),
                new Keyframe(0.08f, 0.95f, 2f, 2f),
                new Keyframe(0.13f, 1.25f, 1f, 0.5f),
                new Keyframe(0.20f, 1.18f, 0f, 0f),
                new Keyframe(0.35f, 1.05f, -0.5f, -0.5f),
                new Keyframe(0.55f, 1.00f, 0f, 0f)
            );
        }

        public static AnimationCurve CreateDefaultMuYFront()
        {
            // α: 0° → 35°, μ: 0 → ~1.1, peak around 10°, drift plateau thereafter
            return new AnimationCurve(
                new Keyframe(0f,   0.00f, 0.8f, 0.8f),
                new Keyframe(4f,   0.55f, 0.6f, 0.6f),
                new Keyframe(8f,   0.95f, 0.3f, 0.3f),
                new Keyframe(12f,  1.10f, 0.0f, 0.0f), // peak
                new Keyframe(18f,  1.02f, -0.2f, -0.2f),
                new Keyframe(26f,  0.95f, 0.0f, 0.0f),
                new Keyframe(35f,  0.92f, 0.0f, 0.0f)
            );
        }

        public static AnimationCurve CreateDefaultMuYRear()
        {
            // Slightly higher plateau for rear to sustain drift
            return new AnimationCurve(
                new Keyframe(0f,   0.00f, 0.8f, 0.8f),
                new Keyframe(4f,   0.60f, 0.6f, 0.6f),
                new Keyframe(9f,   1.00f, 0.3f, 0.3f),
                new Keyframe(12f,  1.15f, 0.0f, 0.0f),
                new Keyframe(20f,  1.08f, -0.2f, -0.2f),
                new Keyframe(28f,  1.02f, 0.0f, 0.0f),
                new Keyframe(36f,  1.00f, 0.0f, 0.0f)
            );
        }
    }
}
