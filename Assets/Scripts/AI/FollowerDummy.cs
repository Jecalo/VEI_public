using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public class FollowerDummy : MonoBehaviour
{
    [SerializeField]
    private bool jumpOnce = false;
    [SerializeField]
    private bool showPath = true;
    [SerializeField]
    private float speed = 2.5f;

    public bool Follow = true;
    public Queue<Vector3> points;

    private Rigidbody rb;
    private PlayerController player = null;



    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        player = FindObjectOfType<PlayerController>();
        points = new Queue<Vector3>();
    }

    private void Start()
    {
        
    }

    private void Update()
    {
        Movement();

        if (showPath) { GizmoPath(); }
    }


    private void GizmoPath()
    {
        if (points.Count != 0)
        {
            Gizmo.DrawPolyline(Color.blue, points);
            Gizmo.DrawLine(transform.position, points.Peek(), Color.blue);
        }
    }

    private void Movement()
    {
        if (points.Count == 0) { return; }
        if (Vector3.Distance(transform.position, points.Peek()) < 0.2f) { points.Dequeue(); }
        if (points.Count == 0) { return; }

        Vector3 dir = points.Peek() - transform.position;
        dir.y = 0f;
        dir = dir.normalized * speed;
        dir.y = rb.velocity.y;

        if (jumpOnce) { dir.y += 5f; jumpOnce = false; }

        rb.velocity = dir;
    }
}
