using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using VoxelTerrain;


public class PlayerController : MonoBehaviour
{
    [SerializeField, Tooltip("Maximum angle when looking up in the X axis.")]
    private float maxRotX = 89f;
    [SerializeField, Tooltip("Minimum angle when looking down in the X axis.")]
    private float minRotX = -89f;
    [SerializeField]
    private float cameraRotationSpeed = 6.0f;
    [SerializeField]
    private float movementSpeed = 3.0f;
    [SerializeField]
    private float sprintSpeed = 6.0f;
    [SerializeField]
    private float jumpSpeed = 5.0f;

    [SerializeField, Tooltip("Player rigidbody reference.")]
    private Rigidbody rb = null;
    [SerializeField, Tooltip("Camera transform reference.")]
    private Transform cameraTr = null;

    public bool AimActive = true;

    private bool lastRotX = false;  //Stores the last valid camera position (true=looking up, false=looking down). Used to determine where to put the camera if it goes out of its limits.
    private float minRotXAdjusted;  //Adjusted minimum euler degrees in the X axis.



    private void Awake()
    {

    }

    private void OnDestroy()
    {
        if (testMesh != null) { meshSDF.Dispose(); }
        if (testMesh2 != null) { meshSDF2.Dispose(); }
        if (treeMesh != null) { treeSDF.Dispose(); }
    }

    private void Start()
    {
        minRotXAdjusted = 360.0f + minRotX;
        if (testMesh != null)
        {
            meshSDF = MeshSDF.BakeMeshSDFData(testMesh, Unity.Collections.Allocator.Persistent);
        }
        if (testMesh2 != null)
        {
            meshSDF2 = MeshSDF.BakeMeshSDFData(testMesh2, Unity.Collections.Allocator.Persistent);
        }
        if (treeMesh != null)
        {
            treeSDF = MeshSDF.BakeMeshSDFData(treeMesh, Unity.Collections.Allocator.Persistent);
        }
    }

    private void Update()
    {
        Interact();
        JumpUpdate();
        MovementUpdate();
    }

    private void LateUpdate()
    {
        if (AimActive) { CameraUpdate(); }
    }


    public Mesh testMesh;
    public Mesh testMesh2;
    public Mesh treeMesh;
    private MeshSDF.MeshSDFData meshSDF;
    private MeshSDF.MeshSDFData meshSDF2;
    private MeshSDF.MeshSDFData treeSDF;

    private void Interact()
    {
        var action = InputDirector.GetGameMap();

        if (action.f1.WasPerformedThisFrame())
        {
            if (Physics.Raycast(cameraTr.position, cameraTr.forward, out RaycastHit hit, 512.0f))
            {
                VoxelManager.ChangeTerrain(hit.point, true, Constants.DefaultTerrainChangeSize, Constants.DefaultTerrainChangeMaterial);
            }
        }
        else if (action.f2.WasPerformedThisFrame())
        {
            if (Physics.Raycast(cameraTr.position, cameraTr.forward, out RaycastHit hit, 512.0f))
            {
                VoxelManager.ChangeTerrain(hit.point, false, Constants.DefaultTerrainChangeSize, Constants.DefaultTerrainChangeMaterial);
            }
        }
        else if (action.f3.WasPerformedThisFrame())
        {
            if (treeMesh == null) { return; }

            if (Physics.Raycast(cameraTr.position, cameraTr.forward, out RaycastHit hit, 512.0f))
            {
                var grid = VoxelManager.GetFirstGrid();
                Kernel kernel = KernelBuilder.MeshKernel(grid, treeSDF, hit.point, 0f, 3f, new KernelConfig(KernelMode.Add), 5);
                KernelHelper.ApplyKernel(grid, kernel);
                kernel.Dispose();
            }
        }
        else if (action.f4.WasPerformedThisFrame())
        {
            if (testMesh == null) { return; }

            if (Physics.Raycast(cameraTr.position, cameraTr.forward, out RaycastHit hit, 512.0f))
            {
                var grid = VoxelManager.GetFirstGrid();
                Kernel kernel = KernelBuilder.MeshKernel(grid, meshSDF, hit.point, new Unity.Mathematics.float3(0,180,0), 2f, new KernelConfig(KernelMode.Add), 4);
                KernelHelper.ApplyKernel(grid, kernel);
                kernel.Dispose();
            }
        }
        else if (action.f5.WasPerformedThisFrame())
        {
            VoxelManager.RegenerateAllGrids();
        }
        else if (action.f6.WasPerformedThisFrame())
        {
            if (Physics.Raycast(cameraTr.position, cameraTr.forward, out RaycastHit hit, 512.0f))
            {
                VoxelManager.ChangeTerrain(hit.point, false, Constants.DefaultTerrainChangeSize, 6);
            }
        }
        else if (action.f7.WasPerformedThisFrame())
        {
            if (testMesh2 == null) { return; }

            if (Physics.Raycast(cameraTr.position, cameraTr.forward, out RaycastHit hit, 512.0f))
            {
                var grid = VoxelManager.GetFirstGrid();
                Kernel kernel = KernelBuilder.MeshKernel(grid, meshSDF2, hit.point, 0f, 2f, new KernelConfig(KernelMode.Add), 4);
                KernelHelper.ApplyKernel(grid, kernel);
                kernel.Dispose();
            }
        }
        else if (action.f8.WasPerformedThisFrame())
        {

        }
    }

    private void MovementUpdate()
    {
        var action = InputDirector.GetGameMap();

        //Get the input, check if there needs to be movement.
        Vector3 axisInput = action.Movement.ReadValue<Vector2>();

        if (axisInput.magnitude < 0.001f)
        {
            float vertical = rb.velocity.y;
            rb.velocity = new Vector3(0.0f, vertical, 0.0f);
            return;
        }

        //Calculate the movement for this frame.
        Vector3 movement = new Vector3(axisInput.x, 0.0f, axisInput.y);

        if (action.Run.ReadValue<float>() > 0.0f) { movement *= sprintSpeed; }
        else { movement *= movementSpeed; }

        //movement *= Time.deltaTime;

        //Rotate the movement to the direction of the camera.
        Vector3 eulerRot = cameraTr.rotation.eulerAngles;
        eulerRot.x = 0.0f;
        eulerRot.z = 0.0f;

        Quaternion rot = Quaternion.identity;
        rot.eulerAngles = eulerRot;
        movement = rot * movement;

        //Move the position.
        //rb.MovePosition(rb.position + movement);
        float y = rb.velocity.y;
        rb.velocity = new Vector3(movement.x, y, movement.z);
    }

    private void JumpUpdate()
    {
        var action = InputDirector.GetGameMap();

        if (action.Jump.WasPerformedThisFrame())
        {
            rb.AddForce(new Vector3(0.0f, jumpSpeed, 0.0f), ForceMode.VelocityChange);
        }
    }

    private void CameraUpdate()
    {
        var action = InputDirector.GetGameMap();

        //Get the input and add it to the current rotation.
        Vector2 aim = action.Aim.ReadValue<Vector2>();
        Vector3 eulerRot = new Vector3(aim.y * -cameraRotationSpeed, aim.x * cameraRotationSpeed, 0.0f);

        eulerRot *= 0.2f;
        eulerRot += cameraTr.rotation.eulerAngles;
        eulerRot.z = 0.0f;

        //If the rotation is out of limits, look at lastRotX and cap the rotation at the correct limit.
        if ((eulerRot.x > maxRotX) && (eulerRot.x < minRotXAdjusted))
        {
            if (lastRotX) { eulerRot.x = maxRotX; }
            else { eulerRot.x = minRotXAdjusted; }
        }

        //Apply the rotation.
        Quaternion rot = Quaternion.identity;
        rot.eulerAngles = eulerRot;
        cameraTr.rotation = rot;

        //Set the last correct position.
        if ((eulerRot.x > 0.0f) && (eulerRot.x < 180.0f)) { lastRotX = true; }
        else { lastRotX = false; }
    }
}
