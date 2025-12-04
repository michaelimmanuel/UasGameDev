using UnityEngine;

namespace VehiclePhysics
{
    [CreateAssetMenu(menuName = "VehiclePhysics/Suspension Axle Data", fileName = "SuspensionAxleData")]
    public class SuspensionAxleData : ScriptableObject
    {
        [Header("Spring-Damper")]
        [Tooltip("Spring rate (N/m)")]
        public float springK = 30000f;

        [Tooltip("Damping (N·s/m)")]
        public float damperC = 4500f;

        [Header("Geometry")]
        [Tooltip("Rest (uncompressed) suspension length (m)")]
        public float restLength = 0.45f;

        [Tooltip("Maximum compression travel (m) from rest")]
        public float maxTravel = 0.25f;

        [Header("Anti-Roll")]
        [Tooltip("Stiffness coupling left/right wheels on this axle (N/m)")]
        public float antiRollStiffness = 15000f;
    }
}
