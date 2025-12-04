using UnityEngine;

namespace VehiclePhysics
{
    /// <summary>
    /// Temporary helper to nudge the car forward for telemetry testing.
    /// Hold a key to apply a small forward force at the rigidbody center.
    /// Remove this from production builds.
    /// </summary>
    public class DebugNudge : MonoBehaviour
    {
        public Rigidbody rb;

        [Header("Nudge Settings")]
        [Tooltip("Key to apply nudge force while held.")]
        public KeyCode nudgeKey = KeyCode.LeftShift;

        [Tooltip("Force magnitude in Newtons applied along car forward.")]
        public float forceN = 1500f;

        [Tooltip("If true, applies force at each grounded wheel contact (more realistic push).")]
        public bool useWheelPositions = true;

        public SuspensionSystem suspension;

        [Tooltip("Optional transform used to define forward direction. If null, uses suspension.transform.forward.")]
        public Transform forwardReference;

        [Tooltip("Show small HUD indicator when nudge is active.")]
        public bool showHud = true;

        void Reset()
        {
            if (rb == null) rb = GetComponentInParent<Rigidbody>();
            if (suspension == null) suspension = GetComponentInParent<SuspensionSystem>();
        }

        void Awake()
        {
            if (rb == null) rb = GetComponentInParent<Rigidbody>();
            if (suspension == null) suspension = GetComponentInParent<SuspensionSystem>();
        }

        void FixedUpdate()
        {
            if (rb == null) return;
            if (!Input.GetKey(nudgeKey)) return;

            Vector3 fwd = forwardReference != null ? forwardReference.forward : (suspension != null ? suspension.transform.forward : transform.forward);

            if (useWheelPositions && suspension != null && suspension.wheels != null)
            {
                // Distribute nudge across grounded wheels
                int groundedCount = 0;
                foreach (var w in suspension.wheels)
                    if (w != null && w.grounded) groundedCount++;

                if (groundedCount == 0) groundedCount = suspension.wheels.Length;

                float perWheel = forceN / Mathf.Max(1, groundedCount);
                for (int i = 0; i < suspension.wheels.Length; i++)
                {
                    var w = suspension.wheels[i];
                    if (w == null) continue;
                    Vector3 pos = w.grounded ? w.contactPoint : w.attach.position;
                    rb.AddForceAtPosition(fwd * perWheel, pos, ForceMode.Force);
                }
            }
            else
            {
                // Simple center force
                rb.AddForce(fwd * forceN, ForceMode.Force);
            }
        }

        void OnGUI()
        {
            if (!showHud) return;
            if (rb == null) return;
            string status = Input.GetKey(nudgeKey) ? "NUDGE: ON" : "NUDGE: off";
            GUILayout.BeginArea(new Rect(10, 420, 180, 40), GUI.skin.box);
            GUILayout.Label(status + $"  v={rb.linearVelocity.magnitude:F1} m/s");
            GUILayout.EndArea();
        }
    }
}
