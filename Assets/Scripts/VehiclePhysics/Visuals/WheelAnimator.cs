using UnityEngine;

namespace VehiclePhysics.Visuals
{
    [DefaultExecutionOrder(100)] // run after physics scripts with default order
    [AddComponentMenu("Vehicle Physics/Wheel Animator")]
    public class WheelAnimator : MonoBehaviour
    {
        public VehiclePhysics.SuspensionSystem suspension;
        [Tooltip("Wheel visual transforms (center). Size must match number of wheels.")]
        public Transform[] wheelVisuals;

        [Header("Options")]
        [Tooltip("Use ground normal to place wheel center when grounded. If false, uses chassis up.")]
        public bool useGroundNormalForCenter = true;
        [Tooltip("Clamp spin speed at very low velocity to avoid jitter.")]
        public float minSpinSpeedKmh = 0.5f;
        [Tooltip("Apply a fixed local rotation offset to the visual wheel (e.g., 90° around X if mesh axle isn't local right).")]
        public Vector3 visualRotationOffsetEuler = Vector3.zero;
        [Tooltip("Invert the rotation offset for right-side wheels (use negative offset on right).")]
        public bool invertRightWheelOffset = true;

        private float[] _spinAngleRad;

        void Reset()
        {
            if (suspension == null) suspension = GetComponentInParent<VehiclePhysics.SuspensionSystem>();
            AutoAssignVisuals();
        }

        void Awake()
        {
            if (suspension == null) suspension = GetComponentInParent<VehiclePhysics.SuspensionSystem>();
            Allocate();
            if (wheelVisuals == null || wheelVisuals.Length == 0) AutoAssignVisuals();
        }

        void OnValidate()
        {
            if (!Application.isPlaying) Allocate();
        }

        void Allocate()
        {
            if (suspension == null || suspension.wheels == null) return;
            int n = suspension.wheels.Length;
            if (_spinAngleRad == null || _spinAngleRad.Length != n)
                _spinAngleRad = new float[n];
            if (wheelVisuals == null || wheelVisuals.Length != n)
            {
                var arr = new Transform[n];
                if (wheelVisuals != null)
                {
                    for (int i = 0; i < Mathf.Min(n, wheelVisuals.Length); i++) arr[i] = wheelVisuals[i];
                }
                wheelVisuals = arr;
            }
        }

        void AutoAssignVisuals()
        {
            if (suspension == null || suspension.wheels == null) return;
            int n = suspension.wheels.Length;
            wheelVisuals = new Transform[n];
            for (int i = 0; i < n; i++)
            {
                var w = suspension.wheels[i];
                if (w != null && w.attach != null && w.attach.childCount > 0)
                {
                    wheelVisuals[i] = w.attach.GetChild(0);
                }
            }
        }

        // Integrate wheel spin from forward speed in FixedUpdate to match physics
        void FixedUpdate()
        {
            if (suspension == null || suspension.wheels == null || suspension.CurrentWheelStates == null) return;
            var states = suspension.CurrentWheelStates;
            int n = states.Length;
            float dt = Time.fixedDeltaTime;
            for (int i = 0; i < n; i++)
            {
                var w = suspension.wheels[i];
                var ws = states[i];
                float radius = Mathf.Max(0.01f, w.wheelRadius);
                // Spin from longitudinal contact velocity along wheel forward
                float omega = ws.Vx / radius; // rad/s, signed
                // Small deadzone to stop jitter
                if (Mathf.Abs(ws.Vx) < (minSpinSpeedKmh / 3.6f)) omega = 0f;
                _spinAngleRad[i] += omega * dt;
            }
        }

        void LateUpdate()
        {
            if (suspension == null || wheelVisuals == null) return;
            var states = suspension.CurrentWheelStates;
            if (states == null) return;
            Vector3 up = suspension.transform.up;
            for (int i = 0; i < suspension.wheels.Length; i++)
            {
                var w = suspension.wheels[i];
                var ws = states[i];
                var vis = wheelVisuals[i];
                if (vis == null || w.attach == null) continue;

                // Determine wheel center
                Vector3 center;
                if (ws.grounded && useGroundNormalForCenter)
                {
                    // Use wheel's stored contact normal from SuspensionSystem
                    center = ws.position + w.contactNormal.normalized * w.wheelRadius;
                }
                else
                {
                    // center = attach - up * (rest - compression)
                    float rest = w.overrideLengths ? w.restLength : (w.isFront ? (suspension.frontAxle != null ? suspension.frontAxle.restLength : 0.45f)
                                                                              : (suspension.rearAxle  != null ? suspension.rearAxle.restLength  : 0.45f));
                    center = w.attach.position - up * (rest - w.compression);
                }

                // Build steered wheel frame
                Quaternion steerRot = Quaternion.AngleAxis(w.steerDeg, up);
                Vector3 wheelFwd = steerRot * w.attach.forward;
                // Right axis after steer
                Vector3 wheelRight = steerRot * w.attach.right;

                // Base yaw rotation looks along wheel forward with chassis up as reference
                Quaternion yawRot = Quaternion.LookRotation(wheelFwd, up);
                // Apply spin around the steered wheel's local right axis (not world right)
                Quaternion spinRot = Quaternion.AngleAxis(_spinAngleRad[i] * Mathf.Rad2Deg, wheelRight);
                Quaternion finalRot = spinRot * yawRot;
                // Apply user-defined local visual offset to accommodate mesh orientation.
                // If requested, use negative offset for right-side wheels.
                Vector3 offset = visualRotationOffsetEuler;
                if (invertRightWheelOffset && !w.isLeft) offset = -offset;
                finalRot *= Quaternion.Euler(offset);

                vis.SetPositionAndRotation(center, finalRot);
            }
        }
    }
}
