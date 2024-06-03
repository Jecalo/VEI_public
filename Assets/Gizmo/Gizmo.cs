using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;

public static class Gizmo
{
    public static void DrawCube(Vector3 center, Vector3 extents, Vector3 rotation, Color color)
    {
        GizmoInternal.Cubes.Add(new GizmoInternal.PreProps(center, rotation, extents, color));
    }

    public static void DrawCube(Vector3 center, Vector3 extents, Quaternion rotation, Color color)
    {
        GizmoInternal.Cubes.Add(new GizmoInternal.PreProps(center, rotation.eulerAngles, extents, color));
    }

    public static void DrawCube(Vector3 center, Vector3 extents, Color color)
    {
        GizmoInternal.Cubes.Add(new GizmoInternal.PreProps(center, 0f, extents, color));
    }

    public static void DrawSolidCube(Vector3 center, Vector3 extents, Color color)
    {
        float3 c = center;
        float3 e = extents / 2f;
        float3 v0 = c - e;
        float3 v1 = c + new float3(e.x, -e.y, -e.z);
        float3 v2 = c + new float3(-e.x, e.y, -e.z);
        float3 v3 = c + new float3(-e.x, -e.y, e.z);
        float3 v4 = c + new float3(e.x, e.y, -e.z);
        float3 v5 = c + new float3(-e.x, e.y, e.z);
        float3 v6 = c + new float3(e.x, -e.y, e.z);
        float3 v7 = c + e;

        GizmoInternal.Tris.Add(new GizmoInternal.PropsTri(v0, v1, v2, color));
        GizmoInternal.Tris.Add(new GizmoInternal.PropsTri(v1, v4, v2, color));
        GizmoInternal.Tris.Add(new GizmoInternal.PropsTri(v3, v6, v5, color));
        GizmoInternal.Tris.Add(new GizmoInternal.PropsTri(v6, v7, v5, color));
        GizmoInternal.Tris.Add(new GizmoInternal.PropsTri(v0, v1, v3, color));
        GizmoInternal.Tris.Add(new GizmoInternal.PropsTri(v1, v6, v3, color));
        GizmoInternal.Tris.Add(new GizmoInternal.PropsTri(v2, v4, v5, color));
        GizmoInternal.Tris.Add(new GizmoInternal.PropsTri(v4, v7, v5, color));
        GizmoInternal.Tris.Add(new GizmoInternal.PropsTri(v2, v3, v5, color));
        GizmoInternal.Tris.Add(new GizmoInternal.PropsTri(v2, v0, v3, color));
        GizmoInternal.Tris.Add(new GizmoInternal.PropsTri(v4, v6, v7, color));
        GizmoInternal.Tris.Add(new GizmoInternal.PropsTri(v4, v1, v6, color));
    }

    public static void DrawSolidCube(Vector3 center, Vector3 extents, Vector3 rotation, Color color)
    {
        DrawSolidCube(center, extents, Quaternion.Euler(rotation), color);
    }

    public static void DrawSolidCube(Vector3 center, Vector3 extents, Quaternion rotation, Color color)
    {
        float3 c = center;
        float3 e = extents / 2f;
        float3 v0 = rotation * (c - e);
        float3 v1 = rotation * (c + new float3(e.x, -e.y, -e.z));
        float3 v2 = rotation * (c + new float3(-e.x, e.y, -e.z));
        float3 v3 = rotation * (c + new float3(-e.x, -e.y, e.z));
        float3 v4 = rotation * (c + new float3(e.x, e.y, -e.z));
        float3 v5 = rotation * (c + new float3(-e.x, e.y, e.z));
        float3 v6 = rotation * (c + new float3(e.x, -e.y, e.z));
        float3 v7 = rotation * (c + e);

        GizmoInternal.Tris.Add(new GizmoInternal.PropsTri(v0, v1, v2, color));
        GizmoInternal.Tris.Add(new GizmoInternal.PropsTri(v1, v4, v2, color));
        GizmoInternal.Tris.Add(new GizmoInternal.PropsTri(v3, v6, v5, color));
        GizmoInternal.Tris.Add(new GizmoInternal.PropsTri(v6, v7, v5, color));
        GizmoInternal.Tris.Add(new GizmoInternal.PropsTri(v0, v1, v3, color));
        GizmoInternal.Tris.Add(new GizmoInternal.PropsTri(v1, v6, v3, color));
        GizmoInternal.Tris.Add(new GizmoInternal.PropsTri(v2, v4, v5, color));
        GizmoInternal.Tris.Add(new GizmoInternal.PropsTri(v4, v7, v5, color));
        GizmoInternal.Tris.Add(new GizmoInternal.PropsTri(v2, v3, v5, color));
        GizmoInternal.Tris.Add(new GizmoInternal.PropsTri(v2, v0, v3, color));
        GizmoInternal.Tris.Add(new GizmoInternal.PropsTri(v4, v6, v7, color));
        GizmoInternal.Tris.Add(new GizmoInternal.PropsTri(v4, v1, v6, color));
    }

    public static void DrawBounds(Vector3 min, Vector3 max, Color color)
    {
        GizmoInternal.Cubes.Add(new GizmoInternal.PreProps((min + max) / 2f, 0f, max - min, color));
    }

    public static void DrawBounds(Bounds bounds, Color color)
    {
        GizmoInternal.Cubes.Add(new GizmoInternal.PreProps(bounds.center, 0f, bounds.extents, color));
    }

    public static void DrawSphere(Vector3 center, float radius, Color color)
    {
        GizmoInternal.Spheres.Add(new GizmoInternal.PreProps(center, 0f, radius, color));
    }

    public static void DrawSphere(Vector3 center, float radius, Quaternion rotation, Vector3 scale, Color color)
    {
        GizmoInternal.Spheres.Add(new GizmoInternal.PreProps(center, rotation.eulerAngles, radius * scale, color));
    }

    public static void DrawLine(Vector3 a, Vector3 b, Color color)
    {
        GizmoInternal.Lines.Add(new GizmoInternal.PropsLine(a, b, color));
    }

    public static void DrawPolyline(Color color, params Vector3[] list)
    {
        int c = list.Length - 1;
        for(int i = 0; i < c; i++)
        {
            GizmoInternal.Lines.Add(new GizmoInternal.PropsLine(list[i], list[i + 1], color));
        }
    }

    public static void DrawPolyline(Color color, IEnumerable<Vector3> list)
    {
        IEnumerator<Vector3> e = list.GetEnumerator();
        if (!e.MoveNext()) { return; }
        Vector3 p = e.Current;

        while (e.MoveNext())
        {
            GizmoInternal.Lines.Add(new GizmoInternal.PropsLine(p, e.Current, color));
            p = e.Current;
        }
    }

    public static void DrawLines(Color color, params Vector3[] list)
    {
        int c = list.Length;
        for (int i = 1; i < c; i += 2)
        {
            GizmoInternal.Lines.Add(new GizmoInternal.PropsLine(list[i - 1], list[i], color));
        }
    }

    public static void DrawLines(Color color, IEnumerable<Vector3> list)
    {
        IEnumerator<Vector3> e = list.GetEnumerator();

        while (true)
        {
            if (!e.MoveNext()) { break; }
            Vector3 p = e.Current;
            if (!e.MoveNext()) { break; }
            GizmoInternal.Lines.Add(new GizmoInternal.PropsLine(p, e.Current, color));
        }
    }

    public static void DrawSolidTriangle(Vector3 a, Vector3 b, Vector3 c, Color color)
    {
        GizmoInternal.Tris.Add(new GizmoInternal.PropsTri(a, b, c, color));
    }

    public static void DrawSolidQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color)
    {
        GizmoInternal.Tris.Add(new GizmoInternal.PropsTri(a, b, c, color));
        GizmoInternal.Tris.Add(new GizmoInternal.PropsTri(a, c, d, color));
    }

    public static void DrawFrustrum(Camera camera, Color color)
    {
        if (camera == null) { return; }
        DrawFrustrum(camera.transform.position, camera.transform.rotation, camera.fieldOfView, camera.nearClipPlane, camera.farClipPlane, camera.aspect, color);
    }

    public static void DrawFrustrum(Vector3 position, Quaternion rotation, float vertical_fov, float nearClipPlane, float farClipPlane, float ratio, Color color)
    {
        float4x4 m = float4x4.TRS(position, rotation, 1f);
        float nearClip = nearClipPlane;
        float farClip = farClipPlane;
        float ar = ratio;
        float fov = math.tan(math.radians(vertical_fov) / 2.0f);
        float fov_h = fov * ar;
        float width_near = nearClip * fov_h;
        float width_far = farClip * fov_h;
        float height_near = nearClip * fov;
        float height_far = farClip * fov;

        float3 v0 = math.transform(m, new Vector3(-width_far, -height_far, farClip));
        float3 v1 = math.transform(m, new Vector3(width_far, -height_far, farClip));
        float3 v2 = math.transform(m, new Vector3(-width_far, height_far, farClip));
        float3 v3 = math.transform(m, new Vector3(-width_near, -height_near, nearClip));
        float3 v4 = math.transform(m, new Vector3(width_far, height_far, farClip));
        float3 v5 = math.transform(m, new Vector3(-width_near, height_near, nearClip));
        float3 v6 = math.transform(m, new Vector3(width_near, -height_near, nearClip));
        float3 v7 = math.transform(m, new Vector3(width_near, height_near, nearClip));


        GizmoInternal.Lines.Add(new GizmoInternal.PropsLine(v0, v1, color));
        GizmoInternal.Lines.Add(new GizmoInternal.PropsLine(v0, v2, color));
        GizmoInternal.Lines.Add(new GizmoInternal.PropsLine(v0, v3, color));
        GizmoInternal.Lines.Add(new GizmoInternal.PropsLine(v7, v4, color));
        GizmoInternal.Lines.Add(new GizmoInternal.PropsLine(v7, v5, color));
        GizmoInternal.Lines.Add(new GizmoInternal.PropsLine(v7, v6, color));
        GizmoInternal.Lines.Add(new GizmoInternal.PropsLine(v1, v4, color));
        GizmoInternal.Lines.Add(new GizmoInternal.PropsLine(v1, v6, color));
        GizmoInternal.Lines.Add(new GizmoInternal.PropsLine(v2, v4, color));
        GizmoInternal.Lines.Add(new GizmoInternal.PropsLine(v2, v5, color));
        GizmoInternal.Lines.Add(new GizmoInternal.PropsLine(v3, v5, color));
        GizmoInternal.Lines.Add(new GizmoInternal.PropsLine(v3, v6, color));
    }

    public static void DrawCollider(BoxCollider collider, Color color)
    {
        if (collider == null) { return; }
        GizmoInternal.Cubes.Add(new GizmoInternal.PreProps(collider.transform.TransformPoint(collider.center),
            collider.transform.rotation.eulerAngles, collider.size * (float3)collider.transform.lossyScale, color));
    }

    public static void DrawCollider(SphereCollider collider, Color color)
    {
        if (collider == null) { return; }
        float3 s = collider.transform.lossyScale;
        GizmoInternal.Spheres.Add(new GizmoInternal.PreProps(collider.transform.TransformPoint(collider.center),
            collider.transform.rotation.eulerAngles, collider.radius * math.max(math.max(s.x, s.y), s.z), color));
    }

    public static void DrawCollider(CapsuleCollider collider, Color color)
    {
        if (collider == null) { return; }

        float3 s = math.abs(collider.transform.lossyScale);
        float h, r;
        float3 center = collider.transform.TransformPoint(collider.center);
        float3 direction;
        float3 offset1, offset2;

        if (collider.direction == 0)
        {
            h = (collider.height * s.x) / 2f;
            r = math.max(s.y, s.z);
            direction = new float3(1f, 0f, 0f);
            offset1 = new float3(0f, 1f, 0f);
            offset2 = new float3(0f, 0f, 1f);
        }
        else if (collider.direction == 1)
        {
            h = (collider.height * s.y) / 2f;
            r = math.max(s.x, s.z);
            direction = new float3(0f, 1f, 0f);
            offset1 = new float3(1f, 0f, 0f);
            offset2 = new float3(0f, 0f, 1f);
        }
        else
        {
            h = (collider.height * s.z) / 2f;
            r = math.max(s.x, s.y);
            direction = new float3(0f, 0f, 1f);
            offset1 = new float3(1f, 0f, 0f);
            offset2 = new float3(0f, 1f, 0f);
        }

        r *= collider.radius;
        h = math.max(0f, math.abs(h) - r);
        direction = collider.transform.TransformDirection(direction);
        float3 c1 = center + direction * h;
        float3 c2 = center - direction * h;
        offset1 = collider.transform.TransformDirection(offset1) * r;
        offset2 = collider.transform.TransformDirection(offset2) * r;

        GizmoInternal.Spheres.Add(new GizmoInternal.PreProps(c1, collider.transform.rotation.eulerAngles, r, color));
        GizmoInternal.Spheres.Add(new GizmoInternal.PreProps(c2, collider.transform.rotation.eulerAngles, r, color));
        GizmoInternal.Lines.Add(new GizmoInternal.PropsLine(c1 + offset1, c2 + offset1, color));
        GizmoInternal.Lines.Add(new GizmoInternal.PropsLine(c1 - offset1, c2 - offset1, color));
        GizmoInternal.Lines.Add(new GizmoInternal.PropsLine(c1 + offset2, c2 + offset2, color));
        GizmoInternal.Lines.Add(new GizmoInternal.PropsLine(c1 - offset2, c2 - offset2, color));
    }

    public static void DrawColliders(GameObject obj, Color color)
    {
        if (obj == null) { return; }

        Collider[] colliders = obj.GetComponentsInChildren<Collider>(true);

        foreach (Collider collider in colliders)
        {
            var t = collider.GetType();

            if (t == typeof(BoxCollider)) { DrawCollider(collider as BoxCollider, color); }
            else if (t == typeof(SphereCollider)) { DrawCollider(collider as SphereCollider, color); }
            else if (t == typeof(CapsuleCollider)) { DrawCollider(collider as CapsuleCollider, color); }
        }
    }

    public static void DrawColliders(List<GameObject> objs, Color color)
    {
        if (objs == null) { return; }
        List<Collider> colliders = new List<Collider>();

        foreach (GameObject obj in objs)
        {
            if (obj == null) { continue; }
            obj.GetComponentsInChildren(true, colliders);

            foreach (Collider collider in colliders)
            {
                var t = collider.GetType();

                if (t == typeof(BoxCollider)) { DrawCollider(collider as BoxCollider, color); }
                else if (t == typeof(SphereCollider)) { DrawCollider(collider as SphereCollider, color); }
                else if (t == typeof(CapsuleCollider)) { DrawCollider(collider as CapsuleCollider, color); }
            }
        }
    }

    //public static void DrawWiremesh(Matrix4x4 trs, Mesh mesh, Color color)
    //{
    //    throw new System.NotImplementedException();
    //}
}
