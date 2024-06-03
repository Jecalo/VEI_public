using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using VoxelTerrain;
using Unity.Mathematics;


public class SampleTaker : MonoBehaviour
{
    [SerializeField]
    private Mesh meshGround = null;
    [SerializeField]
    private Mesh meshAir= null;

    public bool pulsingNoise = false;

    private InputAction action0 = null;
    private InputAction action1 = null;
    private InputAction action2 = null;
    private InputAction action3 = null;
    private InputAction action4 = null;
    private InputAction action5 = null;
    private InputAction action6 = null;

    private MeshSDF.MeshSDFData sdfGround;
    private MeshSDF.MeshSDFData sdfAir;


    void Start()
    {
        sdfGround = MeshSDF.BakeMeshSDFData(meshGround, Unity.Collections.Allocator.Persistent);
        sdfAir = MeshSDF.BakeMeshSDFData(meshAir, Unity.Collections.Allocator.Persistent);

        action0 = new InputAction("Sampler", InputActionType.Button, binding: "<Keyboard>/1");
        action0.performed += Action0_performed;
        action0.Enable();

        action1 = new InputAction("Sampler", InputActionType.Button, binding: "<Keyboard>/2");
        action1.performed += Action1_performed;
        action1.Enable();

        action2 = new InputAction("Sampler", InputActionType.Button, binding: "<Keyboard>/3");
        action2.performed += Action2_performed;
        action2.Enable();

        action3 = new InputAction("Sampler", InputActionType.Button, binding: "<Keyboard>/4");
        action3.performed += Action3_performed;
        action3.Enable();

        action4 = new InputAction("Sampler", InputActionType.Button, binding: "<Keyboard>/5");
        action4.performed += Action4_performed;
        action4.Enable();

        action5 = new InputAction("Sampler", InputActionType.Button, binding: "<Keyboard>/6");
        action5.performed += Action5_performed;
        action5.Enable();

        action6 = new InputAction("Sampler", InputActionType.Button, binding: "<Keyboard>/7");
        action6.performed += Action6_performed;
        action6.Enable();
    }

    private void OnDestroy()
    {
        sdfGround.Dispose();
        sdfAir.Dispose();
    }

    private void Update()
    {
        if (pulsingNoise)
        {
            VoxelGrid grid = FindObjectOfType<VoxelTerrainTest>().grid;
            TerrainGeneration.MovingNoise(grid, 0.5f, Time.time);
        }
    }

    private void Action0_performed(InputAction.CallbackContext obj)
    {
        VoxelManager.ChangeTerrain(new Vector3(15.5f, 15.5f, 15.5f), false, 2.0f, 2);
    }
    private void Action1_performed(InputAction.CallbackContext obj)
    {
        //var grid = VoxelManager.GetFirstGrid();
    }
    private void Action2_performed(InputAction.CallbackContext obj)
    {
        var grid = VoxelManager.GetFirstGrid();
        Kernel kernel = KernelBuilder.MeshKernel(grid, sdfGround, new float3(15.5f,10.0f,15.5f), 0f, 3f, new KernelConfig(KernelMode.Add), 2);
        KernelHelper.ApplyKernel(grid, kernel);
        kernel.Dispose();
    }
    private void Action3_performed(InputAction.CallbackContext obj)
    {
        var grid = VoxelManager.GetFirstGrid();
        Kernel kernel = KernelBuilder.MeshKernel(grid, sdfAir, new float3(15.5f, 15.0f, 15.5f), new float3(0,180f,0), 3f, new KernelConfig(KernelMode.Add), 2);
        KernelHelper.ApplyKernel(grid, kernel);
        kernel.Dispose();
    }
    private void Action4_performed(InputAction.CallbackContext obj)
    {

    }
    private void Action5_performed(InputAction.CallbackContext obj)
    {

    }
    private void Action6_performed(InputAction.CallbackContext obj)
    {

    }
}
