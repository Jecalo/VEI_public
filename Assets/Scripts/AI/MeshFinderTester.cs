using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class MeshFinderTester : MonoBehaviour
{
    public int origin, target;
    public float maxSlope = 0f;

    public bool show = true;
    public bool recalculateOnce = false;

    private Mesh mesh;
    private int[] tris;
    private Vector3[] verts;

    private Vector3[] path;

    private void Start()
    {
        mesh = GetComponent<MeshFilter>().sharedMesh;
        tris = mesh.triangles;
        verts = mesh.vertices;

        path = new Vector3[0];
    }

    private void Update()
    {
        if (recalculateOnce)
        {
            path = MeshFinder.FindPath(mesh, float4x4.identity, origin, target, maxSlope);
            recalculateOnce = false;
        }

        if (show)
        {
            if (path.Length != 0) { Gizmo.DrawPolyline(Color.red, path); }

            int o = math.clamp(origin, 0, tris.Length - 1);
            int t = math.clamp(target, 0, tris.Length - 1);

            float3 a = verts[tris[o * 3 + 0]];
            float3 b = verts[tris[o * 3 + 1]];
            float3 c = verts[tris[o * 3 + 2]];
            Gizmo.DrawSolidTriangle(a, b, c, new Color(0f, 1f, 0f, 0.5f));

            a = verts[tris[t * 3 + 0]];
            b = verts[tris[t * 3 + 1]];
            c = verts[tris[t * 3 + 2]];
            Gizmo.DrawSolidTriangle(a, b, c, new Color(1f, 0f, 0f, 0.5f));
        }
    }

    public void SetOrigin(Ray ray)
    {
        var coll = GetComponent<MeshCollider>();

        if (coll.Raycast(ray, out RaycastHit hit, 1000f))
        {
            origin = hit.triangleIndex;
            recalculateOnce = true;
        }
    }
}
