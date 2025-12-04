using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VehiclePhysics.Debugging
{
    [DisallowMultipleComponent]
    public class VehicleSpeedGizmo : MonoBehaviour
    {
        public Rigidbody rb;
        [Tooltip("Offset above the vehicle where the label appears.")]
        public Vector3 labelOffset = new Vector3(0, 1.5f, 0);
        [Tooltip("Color of the speed label in Scene view.")]
        public Color labelColor = new Color(0.2f, 0.8f, 1f, 1f);
        [Tooltip("Draw an arrow indicating forward direction and magnitude.")]
        public bool drawArrow = true;
        [Tooltip("Scale factor for arrow length per 10 km/h.")]
        public float arrowScalePer10Kmh = 0.5f;

        void Reset()
        {
            rb = GetComponentInParent<Rigidbody>();
            if (rb == null) rb = GetComponent<Rigidbody>();
        }

        void OnValidate()
        {
            if (rb == null)
            {
                rb = GetComponentInParent<Rigidbody>();
                if (rb == null) rb = GetComponent<Rigidbody>();
            }
        }

        void OnDrawGizmos()
        {
            if (rb == null) return;
            float kmh = rb.linearVelocity.magnitude * 3.6f;
            Vector3 pos = transform.position + labelOffset;

#if UNITY_EDITOR
            var prevColor = Handles.color;
            Handles.color = labelColor;
            Handles.Label(pos, $"{kmh:F1} km/h");
            Handles.color = prevColor;
#endif

            if (drawArrow)
            {
                Gizmos.color = labelColor;
                Vector3 dir = rb.transform.forward.normalized;
                float length = (kmh / 10f) * arrowScalePer10Kmh;
                Gizmos.DrawLine(pos, pos + dir * length);
                // arrow head
                Vector3 right = Quaternion.AngleAxis(25f, rb.transform.up) * dir;
                Vector3 left = Quaternion.AngleAxis(-25f, rb.transform.up) * dir;
                float headLen = Mathf.Min(0.5f, length * 0.3f);
                Gizmos.DrawLine(pos + dir * length, pos + dir * length - right * headLen);
                Gizmos.DrawLine(pos + dir * length, pos + dir * length - left * headLen);
            }
        }
    }
}
