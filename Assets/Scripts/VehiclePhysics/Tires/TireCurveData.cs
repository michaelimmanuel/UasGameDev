using UnityEngine;

namespace VehiclePhysics
{
    [CreateAssetMenu(menuName = "VehiclePhysics/Tire Curve Data", fileName = "TireCurveData")]
    public class TireCurveData : ScriptableObject
    {
        [Header("Longitudinal μx Curves (vs slip ratio)")] 
        public AnimationCurve muXFront = AnimationCurve.Linear(0, 0, 0.15f, 1.2f); // Placeholder
        public AnimationCurve muXRear  = AnimationCurve.Linear(0, 0, 0.15f, 1.25f); // Slightly higher rear for drift bias

        [Header("Lateral μy Curves (vs slip angle deg)")] 
        public AnimationCurve muYFront = AnimationCurve.Linear(0, 0, 12f, 1.1f);
        public AnimationCurve muYRear  = AnimationCurve.Linear(0, 0, 12f, 1.15f);

        [Header("Load Sensitivity")] 
        [Tooltip("Coefficient determining how μ decreases with higher load.")] 
        [Range(0f, 0.3f)] public float loadSensitivity = 0.1f;
        [Tooltip("Reference load per wheel (N) used for scaling μ.")] 
        public float referenceLoad = 2500f;
        [Tooltip("Minimum μ after load scaling.")] 
        public float minMu = 0.2f;
        [Tooltip("Maximum μ clamp after scaling.")] 
        public float maxMu = 3.0f;
    }
}
