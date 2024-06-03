using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraController : MonoBehaviour
{
    [SerializeField, Tooltip("Maximum angle when looking up in the X axis.")]
    private float maxRotX = 70.0f;
    [SerializeField, Tooltip("Minimum angle when looking down in the X axis.")]
    private float minRotX = -50.0f;
    [SerializeField]
    private float cameraRotationSpeed = 6.0f;
    [SerializeField]
    private float movementSpeed = 3.0f;
    [SerializeField]
    private float sprintSpeed = 6.0f;
    [SerializeField]
    private bool activeMovement = true;

    [SerializeField]
    private Director director;

    private DefaultControl action;  //Input system wrapper

    private bool lastRotX = false;  //Stores the last valid camera position (true=looking up, false=looking down). Used to determine where to put the camera if it goes out of its limits.
    private float minRotXAdjusted;  //Adjusted minimum euler degrees in the X axis.

    

    //Probably should not be here.
    private void Awake()
    {
        action = new DefaultControl();
        action.Default.Enable();

        action.Default.Restart.performed += _ => Restart();
        action.Default.f2.performed += _ => F2();
        action.Default.f3.performed += _ => F3();

        if (activeMovement)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Start()
    {
        minRotXAdjusted = 360.0f + minRotX;
    }

    private void Update()
    {
        if(activeMovement)
        {
            //Interact();
            MovementUpdate();
        }

        if (action.Default.f1.WasPerformedThisFrame()) {
            activeMovement = !activeMovement;
            if (activeMovement) { Cursor.lockState = CursorLockMode.Locked; }
            else { Cursor.lockState = CursorLockMode.None; }
            Cursor.visible = !activeMovement;
        }
    }

    private void LateUpdate()
    {
        if(activeMovement) { CameraUpdate(); }
        
    }


    private void Interact()
    {
        if (action.Default.Interact.ReadValue<float>() != 0.0f)
        {
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit))
            {

            }
        }
    }

    private void MovementUpdate()
    {
        Vector3 movement = action.Default.Movement.ReadValue<Vector3>();

        if (movement == Vector3.zero) { return; }

        if (action.Default.Run.ReadValue<float>() > 0.0f) { movement *= sprintSpeed; }
        else { movement *= movementSpeed; }

        movement *= Time.deltaTime;

        Vector3 eulerRot = transform.rotation.eulerAngles;
        //eulerRot.x = 0.0f;
        //eulerRot.z = 0.0f;

        //Quaternion rot = Quaternion.identity;
        //rot.eulerAngles = eulerRot;
        movement = transform.rotation * movement;

        transform.Translate(movement, Space.World);
    }

    private void CameraUpdate()
    {
        Vector2 aim = action.Default.Aim.ReadValue<Vector2>();
        Vector3 eulerRot = new Vector3(aim.y * -cameraRotationSpeed, aim.x * cameraRotationSpeed, 0.0f);

        eulerRot *= Time.deltaTime;

        eulerRot += transform.rotation.eulerAngles;
        eulerRot.z = 0.0f;

        if ((eulerRot.x > maxRotX) && (eulerRot.x < minRotXAdjusted))
        {
            if (lastRotX) { eulerRot.x = maxRotX; }
            else { eulerRot.x = minRotXAdjusted; }
        }

        Quaternion rot = Quaternion.identity;
        rot.eulerAngles = eulerRot;
        transform.rotation = rot;

        if ((eulerRot.x > 0.0f) && (eulerRot.x < 180.0f)) { lastRotX = true; }
        else { lastRotX = false; }
    }


    private void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void F2()
    {
        
    }

    private void F3()
    {
        
    }
}
