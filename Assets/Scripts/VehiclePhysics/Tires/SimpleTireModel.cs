using UnityEngine;

namespace VehiclePhysics
{
    /// <summary>
    /// Simple tire model evaluating μx and μy from provided curves and applying load sensitivity.
    /// Supports combined slip reduction for realistic drift behavior.
    /// </summary>
    public class SimpleTireModel : MonoBehaviour, ITireModel
    {
        public TireCurveData curveData;

        [Tooltip("Multiplier applied to evaluated μ before load scaling (for quick global tuning).")]
        public float globalMuScale = 1f;

        [Header("Combined Slip (for drifting)")]
        [Tooltip("Enable combined slip: high longitudinal slip reduces lateral grip (essential for handbrake drifts).")]
        public bool enableCombinedSlip = true;
        [Tooltip("How much longitudinal slip affects lateral grip. 1.0 = full friction circle, 0.5 = half effect.")]
        [Range(0f, 1f)] public float combinedSlipStrength = 0.85f;
        [Tooltip("Slip ratio below this threshold has NO effect on lateral grip (normal driving).")]
        public float combinedSlipThreshold = 0.35f;
        [Tooltip("Slip ratio magnitude at which lateral grip is reduced to minimum (full lockup).")]
        public float lockupSlipRatio = 0.9f;
        [Tooltip("Minimum lateral grip multiplier when wheels are fully locked.")]
        [Range(0f, 0.5f)] public float lockedWheelLateralGrip = 0.25f;

        // Cache last slip ratio per wheel for combined slip calculation
        private float[] _lastSlipRatio = new float[8];
        private int _slipIndex = 0;

        public float MuX(float slipRatio, float load, bool isFront)
        {
            if (curveData == null) return 0f;
            
            // Cache slip ratio for combined slip calculation in MuY
            if (_slipIndex < _lastSlipRatio.Length)
            {
                _lastSlipRatio[_slipIndex] = slipRatio;
            }
            
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
            
            // Combined slip: reduce lateral grip when there's high longitudinal slip
            // This is what makes handbrake drifts work - locked rear wheels lose lateral grip
            // Only applies when slip is above threshold (normal driving unaffected)
            if (enableCombinedSlip && _slipIndex < _lastSlipRatio.Length)
            {
                float slipRatioMag = Mathf.Abs(_lastSlipRatio[_slipIndex]);
                
                // Only reduce lateral grip when slip is significant (above threshold)
                // This prevents grip loss during normal acceleration/braking
                if (slipRatioMag > combinedSlipThreshold)
                {
                    // Calculate how far into the lockup range we are
                    // 0 = at threshold, 1 = at full lockup
                    float slipAboveThreshold = slipRatioMag - combinedSlipThreshold;
                    float lockupRange = lockupSlipRatio - combinedSlipThreshold;
                    float lockupFactor = Mathf.Clamp01(slipAboveThreshold / Mathf.Max(0.01f, lockupRange));
                    
                    // Apply lateral grip reduction
                    float lateralReduction = Mathf.Lerp(1f, lockedWheelLateralGrip, lockupFactor * combinedSlipStrength);
                    mu *= lateralReduction;
                }
                _slipIndex++;
            }
            
            return mu;
        }

        /// <summary>
        /// Call this at the start of each physics frame to reset slip ratio tracking.
        /// </summary>
        public void ResetSlipTracking()
        {
            _slipIndex = 0;
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
