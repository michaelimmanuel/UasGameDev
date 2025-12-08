using UnityEngine;

namespace VehiclePhysics
{
    public class SuspensionSystem : MonoBehaviour
    {
        [System.Serializable]
        public class Wheel
        {
            public string name = "Wheel";
            public Transform attach;          // Suspension attachment point on chassis
            public bool isFront = true;       // Front or rear axle (used for data selection)
            public bool isLeft = true;        // Left/right for anti-roll
            public bool isPowered = false;    // Driven wheel
            [Range(-45f, 45f)] public float steerDeg = 0f; // Steering angle (applied to forward/right basis)

            [Header("Wheel Geometry")]
            public float wheelRadius = 0.34f; // m

            [Header("Overrides (optional)")]
            public bool overrideLengths = false;
            public float restLength = 0.45f;  // m (only if override enabled)
            public float maxTravel = 0.25f;   // m (only if override enabled)

            [HideInInspector] public bool grounded;
            [HideInInspector] public float compression;   // m (positive when compressed)
            [HideInInspector] public float compressionRate; // m/s
            [HideInInspector] public Vector3 contactPoint;
            [HideInInspector] public Vector3 contactNormal;
            [HideInInspector] public WheelState state;

            // Cache previous for damper
            internal float _prevCompression;
        }

        [Header("References")]
        public Rigidbody rb;
        public LayerMask groundMask = ~0;

        [Header("Axle Data")] 
        public SuspensionAxleData frontAxle;
        public SuspensionAxleData rearAxle;

        [Header("Wheels (order doesn’t matter)")]
        public Wheel[] wheels = new Wheel[4];

        [Header("Raycast Settings")]
        [Tooltip("Extra ray length beyond rest+travel to maintain contact over gaps")]
        public float rayExtra = 0.1f;

        [Header("Gizmos")] 
        public bool drawGizmos = true;

        public WheelState[] CurrentWheelStates { get; private set; }

        void Reset()
        {
            rb = GetComponentInParent<Rigidbody>();
        }

        void Awake()
        {
            if (rb == null) rb = GetComponentInParent<Rigidbody>();
            if (wheels == null || wheels.Length == 0)
                wheels = new Wheel[4];
            CurrentWheelStates = new WheelState[wheels.Length];
        }

        void FixedUpdate()
        {
            if (rb == null) return;
            if (wheels == null || wheels.Length == 0) return;

            // First pass: raycast and compute per-wheel spring/damper forces
            float dt = Time.fixedDeltaTime;

            // Compute anti-roll terms per axle
            float frontLeftComp = 0f, frontRightComp = 0f;
            float rearLeftComp = 0f, rearRightComp = 0f;
            bool flSet=false, frSet=false, rlSet=false, rrSet=false;
            int flIndex=-1, frIndex=-1, rlIndex=-1, rrIndex=-1;

            for (int i = 0; i < wheels.Length; i++)
            {
                var w = wheels[i];
                if (w.attach == null) continue;

                var axle = w.isFront ? frontAxle : rearAxle;
                if (axle == null) continue;

                float rest = w.overrideLengths ? w.restLength : axle.restLength;
                float maxT = w.overrideLengths ? w.maxTravel  : axle.maxTravel;

                Vector3 rayOrigin = w.attach.position;
                Vector3 rayDir = -transform.up; // chassis down direction
                float rayLen = rest + maxT + w.wheelRadius + rayExtra;

                if (Physics.Raycast(rayOrigin, rayDir, out RaycastHit hit, rayLen, groundMask, QueryTriggerInteraction.Ignore))
                {
                    w.grounded = true;
                    w.contactPoint = hit.point;
                    w.contactNormal = hit.normal;

                    // Suspension length is distance from attach to ground minus wheel radius
                    float length = Mathf.Max(0f, hit.distance - w.wheelRadius);
                    // Compression from rest (positive when shorter than rest)
                    float compression = Mathf.Clamp(rest - length, 0f, maxT);
                    w.compressionRate = (compression - w._prevCompression) / Mathf.Max(1e-6f, dt);
                    w.compression = compression;
                    w._prevCompression = compression;
                }
                else
                {
                    w.grounded = false;
                    w.contactPoint = rayOrigin + rayDir * (rest + w.wheelRadius);
                    w.contactNormal = transform.up;
                    w.compressionRate = 0f;
                    w.compression = 0f;
                    w._prevCompression = 0f;
                }

                // Track for anti-roll per axle
                if (w.isFront)
                {
                    if (w.isLeft) { frontLeftComp = w.compression; flSet = true; flIndex = i; }
                    else          { frontRightComp = w.compression; frSet = true; frIndex = i; }
                }
                else
                {
                    if (w.isLeft) { rearLeftComp = w.compression; rlSet = true; rlIndex = i; }
                    else          { rearRightComp = w.compression; rrSet = true; rrIndex = i; }
                }

                wheels[i] = w; // write back
            }

            // Second pass: apply forces and populate states
            for (int i = 0; i < wheels.Length; i++)
            {
                var w = wheels[i];
                var axle = w.isFront ? frontAxle : rearAxle;
                if (w.attach == null || axle == null) continue;

                float rest = w.overrideLengths ? w.restLength : axle.restLength;
                float maxT = w.overrideLengths ? w.maxTravel  : axle.maxTravel;

                float springF = axle.springK * w.compression;
                float damperF = axle.damperC * w.compressionRate;

                float normalForce = 0f;
                if (w.grounded)
                {
                    // Apply only spring and damper as wheel normal. Anti-roll is applied as a separate paired force below.
                    normalForce = Mathf.Max(0f, springF + damperF);

                    // Apply suspension force upward at contact point
                    Vector3 up = transform.up;
                    rb.AddForceAtPosition(up * normalForce, w.contactPoint, ForceMode.Force);
                }

                // Build wheel frame (apply steer about chassis up)
                Quaternion steerRot = Quaternion.AngleAxis(w.steerDeg, transform.up);
                Vector3 wheelFwd = steerRot * w.attach.forward;
                // Use cross product to ensure a consistent right axis even if the attach transform is mirrored
                Vector3 wheelRight = Vector3.Cross(transform.up, wheelFwd).normalized;

                // Local velocities at contact/attachment
                Vector3 v = rb.GetPointVelocity(w.contactPoint);
                float Vx = Vector3.Dot(v, wheelFwd);
                float Vy = Vector3.Dot(v, wheelRight);

                // Populate state
                WheelState ws = new WheelState
                {
                    position   = w.contactPoint,
                    forward    = wheelFwd,
                    right      = wheelRight,
                    loadN      = normalForce,
                    Vx         = Vx,
                    Vy         = Vy,
                    omega      = w.state.omega, // carry forward; updated by WheelDynamics
                    steerDeg   = w.steerDeg,
                    isFront    = w.isFront,
                    isPowered  = w.isPowered,
                    grounded   = w.grounded
                };
                w.state = ws;
                wheels[i] = w;
                CurrentWheelStates[i] = ws;
            }

            // Apply anti-roll bars as equal-and-opposite forces per axle
            Vector3 upDir = transform.up;
            // Front axle
            if (flSet && frSet && frontAxle != null)
            {
                float diff = frontLeftComp - frontRightComp; // positive if left more compressed
                var wl = flIndex >= 0 ? wheels[flIndex] : null;
                var wr = frIndex >= 0 ? wheels[frIndex] : null;
                if (wl != null && wr != null && wl.grounded && wr.grounded)
                {
                    float arf = frontAxle.antiRollStiffness * diff;
                    // Resist roll: push UP on the more compressed side, DOWN on the less compressed
                    rb.AddForceAtPosition(upDir * arf, wl.contactPoint, ForceMode.Force);   // left
                    rb.AddForceAtPosition(upDir * -arf, wr.contactPoint, ForceMode.Force);  // right
                }
            }
            // Rear axle
            if (rlSet && rrSet && rearAxle != null)
            {
                float diff = rearLeftComp - rearRightComp;
                var wl = rlIndex >= 0 ? wheels[rlIndex] : null;
                var wr = rrIndex >= 0 ? wheels[rrIndex] : null;
                if (wl != null && wr != null && wl.grounded && wr.grounded)
                {
                    float arf = rearAxle.antiRollStiffness * diff;
                    rb.AddForceAtPosition(upDir * arf, wl.contactPoint, ForceMode.Force);   // left
                    rb.AddForceAtPosition(upDir * -arf, wr.contactPoint, ForceMode.Force);  // right
                }
            }
        }

        void OnDrawGizmosSelected()
        {
            if (!drawGizmos || wheels == null) return;

            Gizmos.matrix = Matrix4x4.identity;
            foreach (var w in wheels)
            {
                if (w == null || w.attach == null) continue;

                // Draw ray
                var axle = w.isFront ? frontAxle : rearAxle;
                float rest = w.overrideLengths ? w.restLength : (axle != null ? axle.restLength : 0.45f);
                float maxT = w.overrideLengths ? w.maxTravel  : (axle != null ? axle.maxTravel : 0.25f);

                Vector3 origin = w.attach.position;
                Vector3 down = -transform.up;
                float total = rest + maxT + w.wheelRadius;

                Gizmos.color = Color.gray;
                Gizmos.DrawLine(origin, origin + down * total);
                Gizmos.DrawWireSphere(origin + down * (rest), 0.02f);

                // Contact point
                Gizmos.color = w.grounded ? Color.green : Color.red;
                Gizmos.DrawSphere(w.contactPoint, 0.03f);

                // Wheel frame axes
                Quaternion steerRot = Quaternion.AngleAxis(w.steerDeg, transform.up);
                Vector3 fwd = steerRot * w.attach.forward;
                Vector3 right = Vector3.Cross(transform.up, fwd).normalized;
                Gizmos.color = new Color(0.2f,0.6f,1f,1f); // forward (blue-ish)
                Gizmos.DrawLine(w.contactPoint, w.contactPoint + fwd * 0.3f);
                Gizmos.color = new Color(0.2f,1f,0.4f,1f); // right (green-ish)
                Gizmos.DrawLine(w.contactPoint, w.contactPoint + right * 0.3f);

                // Normal force label
#if UNITY_EDITOR
                if (w.state.loadN > 0f)
                {
                    UnityEditor.Handles.color = Color.yellow;
                    UnityEditor.Handles.Label(w.contactPoint + Vector3.up * 0.05f, $"N={w.state.loadN:F0}N");
                }
#endif
            }
        }
    }
}
