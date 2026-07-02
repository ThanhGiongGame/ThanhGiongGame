using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class WeaponTrail : MonoBehaviour
{
    [Header("References")]
    public Transform trailStart;
    public Transform trailEnd;

    [Header("Settings")]
    public float lifeTime = 0.15f;
    public Gradient colorGradient;
    struct TrailPoint
    {
        public Vector3 start;
        public Vector3 end;
        public float time;
    }

    private readonly List<TrailPoint> points = new();

    private Mesh mesh;
    private MeshFilter meshFilter;

    private bool emitting;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();

        mesh = new Mesh();
        mesh.name = "WeaponTrailMesh";

        meshFilter.mesh = mesh;
    }
    public void BeginTrail()
    {
        emitting = true;
    }

    public void EndTrail()
    {
        emitting = false;
    }

    void LateUpdate()
    {
        if (emitting)
        {
            points.Add(new TrailPoint
            {
                start = trailStart.position,
                end = trailEnd.position,
                time = Time.time
            });
        }

        points.RemoveAll(p => Time.time - p.time > lifeTime);

        BuildMesh();
    }

    void BuildMesh()
    {
        mesh.Clear();

        if (points.Count < 2)
            return;

        int pointCount = points.Count;

        Vector3[] vertices = new Vector3[pointCount * 2];
        Color[] colors = new Color[pointCount * 2];

        Vector2[] uvs = new Vector2[pointCount * 2];

        int[] triangles = new int[(pointCount - 1) * 6];

        for (int i = 0; i < pointCount; i++)
        {

            TrailPoint p = points[i];

            float age = Time.time - p.time;
            float t = 1f - Mathf.Clamp01(age / lifeTime);

            Color color = colorGradient.Evaluate(t);
            color.a *= t;

            colors[i * 2] = color;
            colors[i * 2 + 1] = color;  
            vertices[i * 2] =
                transform.InverseTransformPoint(p.start);

            vertices[i * 2 + 1] =
                transform.InverseTransformPoint(p.end);

            float u = (float)i / (pointCount - 1);

            uvs[i * 2] = new Vector2(u, 0);
            uvs[i * 2 + 1] = new Vector2(u, 1);
        }

        int tri = 0;

        for (int i = 0; i < pointCount - 1; i++)
        {
            int v = i * 2;

            triangles[tri++] = v;
            triangles[tri++] = v + 2;
            triangles[tri++] = v + 1;

            triangles[tri++] = v + 1;
            triangles[tri++] = v + 2;
            triangles[tri++] = v + 3;
        }

        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        mesh.RecalculateBounds();
    }
}