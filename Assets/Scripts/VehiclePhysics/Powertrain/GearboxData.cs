using UnityEngine;

namespace VehiclePhysics
{
    [CreateAssetMenu(menuName = "VehiclePhysics/Gearbox Data", fileName = "GearboxData")]
    public class GearboxData : ScriptableObject
    {
        [Tooltip("Ordered gear ratios (exclude neutral). First gear is index 0.")]
        public float[] gearRatios = new float[] { 3.80f, 2.20f, 1.52f, 1.22f, 1.00f };
        [Tooltip("Final drive ratio (diff).")]
        public float finalDrive = 3.73f;
        [Tooltip("Drivetrain efficiency (0..1)")]
        [Range(0.5f,1f)] public float efficiency = 0.92f;

        public float CurrentRatio(int gearIndex)
        {
            if (gearRatios == null || gearRatios.Length == 0) return 0f;
            if (gearIndex < 0 || gearIndex >= gearRatios.Length) gearIndex = 0;
            return gearRatios[gearIndex] * finalDrive;
        }
    }
}
