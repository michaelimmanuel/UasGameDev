using UnityEngine;

namespace VehiclePhysics
{
    public struct WheelState
    {
        public Vector3 position;   // world position of contact (or ray end if not grounded)
        public Vector3 forward;    // wheel forward (world)
        public Vector3 right;      // wheel right (world)
        public float   loadN;      // normal force N
        public float   Vx;         // local forward velocity (m/s)
        public float   Vy;         // local lateral velocity (m/s)
        public float   steerDeg;   // steering angle (deg)
        public bool    isFront;    // front axle flag
        public bool    isPowered;  // driven wheel flag
        public bool    grounded;   // is the wheel contacting ground
    }

    public struct TireForces
    {
        public float Fx; // along wheel forward
        public float Fy; // along wheel right
    }

    public interface ITireModel
    {
        float MuX(float slipRatio, float load, bool isFront);
        float MuY(float slipAngleDeg, float load, bool isFront);
    }
}
