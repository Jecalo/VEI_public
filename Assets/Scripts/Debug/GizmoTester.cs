using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GizmoTester : MonoBehaviour
{
    public enum GizmoSource { Unity, Custom }
    public enum GizmoType { Line, Cube }

    public GizmoSource source;
    public int size = 5;
    public bool randomColorEach = false;
    public bool cache = false;



    private void Start()
    {
        
    }

    private void Update()
    {
        if (source != GizmoSource.Custom) { return; }
        if (cache) { }
        else
        {
            Color color = randomColorEach ? Random.ColorHSV() : Color.black;
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        Vector3 p = new Vector3(x, y, z);
                        Gizmo.DrawLine(p, p + Vector3.up, color);
                    }
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (!EditorApplication.isPlaying) { return; }
#endif
        if (source != GizmoSource.Unity) { return; }
        if (cache) { }
        else
        {
            Gizmos.color = randomColorEach ? Random.ColorHSV() : Color.black;
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        Vector3 p = new Vector3(x, y, z);
                        Gizmos.DrawLine(p, p + Vector3.up);
                    }
                }
            }
        }
    }
}
