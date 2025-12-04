using UnityEngine;

namespace VehiclePhysics
{
    /// <summary>
    /// Simple tire model evaluating μx and μy from provided curves and applying load sensitivity.
    /// </summary>
    public class SimpleTireModel : MonoBehaviour, ITireModel
    {
        public TireCurveData curveData;

        [Tooltip("Multiplier applied to evaluated μ before load scaling (for quick global tuning).")]
        public float globalMuScale = 1f;

        public float MuX(float slipRatio, float load, bool isFront)
        {
            if (curveData == null) return 0f;
            var c = isFront ? curveData.muXFront : curveData.muXRear;
            float absSlip = Mathf.Abs(slipRatio);
            float muBase = c.Evaluate(absSlip) * globalMuScale;
            float mu = ApplyLoadSensitivity(muBase, load);
            return mu;
        }

        public float MuY(float slipAngleDeg, float load, bool isFront)
        {
            if (curveData == null) return 0f;
            var c = isFront ? curveData.muYFront : curveData.muYRear;
            float absAng = Mathf.Abs(slipAngleDeg);
            float muBase = c.Evaluate(absAng) * globalMuScale;
            float mu = ApplyLoadSensitivity(muBase, load);
            return mu;
        }

        float ApplyLoadSensitivity(float muBase, float load)
        {
            if (curveData.referenceLoad <= 0.01f) return Mathf.Clamp(muBase, curveData.minMu, curveData.maxMu);
            float rel = (load / curveData.referenceLoad) - 1f; // positive when above ref
            float scaled = muBase * (1f - curveData.loadSensitivity * rel);
            return Mathf.Clamp(scaled, curveData.minMu, curveData.maxMu);
        }
    }
}
