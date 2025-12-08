using System.Collections.Generic;
using UnityEngine;

namespace VehiclePhysics.Visuals
{
    [AddComponentMenu("Vehicle Physics/Skidmark Manager")]
    public class SkidmarkManager : MonoBehaviour
    {
        [Header("Rendering")]
        public Material skidMaterial;
        [Tooltip("Width of the skid mark quads (meters)")] public float defaultWidth = 0.18f;
        [Tooltip("Offset above ground to avoid z-fighting (meters)")] public float normalOffset = 0.01f;
        [Tooltip("Maximum segments before clearing the mesh to keep it light")] public int maxSegments = 4000;

        private Mesh _mesh;
        private MeshFilter _mf;
        private MeshRenderer _mr;

        private readonly List<Vector3> _vertices = new List<Vector3>(8192);
        private readonly List<Vector3> _normals  = new List<Vector3>(8192);
        private readonly List<Color>   _colors   = new List<Color>(8192);
        private readonly List<Vector2> _uvs      = new List<Vector2>(8192);
        private readonly List<int>     _indices  = new List<int>(12288);

        private int _segmentCount;

        void Awake()
        {
            _mf = gameObject.GetComponent<MeshFilter>();
            if (_mf == null) _mf = gameObject.AddComponent<MeshFilter>();
            _mr = gameObject.GetComponent<MeshRenderer>();
            if (_mr == null) _mr = gameObject.AddComponent<MeshRenderer>();
            if (_mesh == null)
            {
                _mesh = new Mesh { name = "Skidmarks" };
                _mesh.MarkDynamic();
                _mf.sharedMesh = _mesh;
            }
            if (skidMaterial == null)
            {
                var sh = Shader.Find("Sprites/Default");
                skidMaterial = new Material(sh) { color = new Color(0.05f, 0.05f, 0.05f, 0.9f) };
            }
            _mr.sharedMaterial = skidMaterial;
        }

        public void Clear()
        {
            _vertices.Clear();
            _normals.Clear();
            _colors.Clear();
            _uvs.Clear();
            _indices.Clear();
            _segmentCount = 0;
            ApplyMesh();
        }

        /// <summary>
        /// Add a skid mark segment between two ground-contact positions.
        /// </summary>
        /// <param name="p0">Previous contact position</param>
        /// <param name="p1">Current contact position</param>
        /// <param name="normal">Ground normal</param>
        /// <param name="width">Mark width (meters)</param>
        /// <param name="intensity">0..1, used as alpha</param>
        public void AddSegment(Vector3 p0, Vector3 p1, Vector3 normal, float width, float intensity)
        {
            if (width <= 1e-3f) width = defaultWidth;
            Vector3 d = p1 - p0;
            float len = d.magnitude;
            if (len < 0.02f) return; // too short
            Vector3 n = normal.sqrMagnitude < 1e-6f ? Vector3.up : normal.normalized;
            Vector3 t = Vector3.Cross(n, d).normalized; // left/right
            float half = width * 0.5f;

            // Slightly lift to avoid z-fight
            p0 += n * normalOffset;
            p1 += n * normalOffset;

            // Quad vertices
            Vector3 v0 = p0 - t * half;
            Vector3 v1 = p0 + t * half;
            Vector3 v2 = p1 - t * half;
            Vector3 v3 = p1 + t * half;

            Color col = new Color(0f, 0f, 0f, Mathf.Clamp01(intensity));

            int baseIndex = _vertices.Count;
            _vertices.Add(v0); _vertices.Add(v1); _vertices.Add(v2); _vertices.Add(v3);
            _normals.Add(n);   _normals.Add(n);   _normals.Add(n);   _normals.Add(n);
            _colors.Add(col);  _colors.Add(col);  _colors.Add(col);  _colors.Add(col);
            _uvs.Add(new Vector2(0f, 0f));
            _uvs.Add(new Vector2(1f, 0f));
            _uvs.Add(new Vector2(0f, 1f));
            _uvs.Add(new Vector2(1f, 1f));

            // Two triangles
            _indices.Add(baseIndex + 0);
            _indices.Add(baseIndex + 2);
            _indices.Add(baseIndex + 1);

            _indices.Add(baseIndex + 2);
            _indices.Add(baseIndex + 3);
            _indices.Add(baseIndex + 1);

            _segmentCount++;
            if (_segmentCount > maxSegments)
            {
                // Simple strategy: clear when exceeding cap to avoid complex reindexing
                Clear();
                _segmentCount = 0;
            }

            ApplyMesh();
        }

        void ApplyMesh()
        {
            _mesh.Clear();
            _mesh.SetVertices(_vertices);
            _mesh.SetNormals(_normals);
            _mesh.SetColors(_colors);
            _mesh.SetUVs(0, _uvs);
            _mesh.SetTriangles(_indices, 0, true);
            _mesh.RecalculateBounds();
        }
    }
}
