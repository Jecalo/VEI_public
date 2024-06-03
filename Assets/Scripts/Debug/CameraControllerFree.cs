using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;


[Serializable]
public class RayEvent : UnityEvent<Ray> { }


public class CameraControllerFree : MonoBehaviour
{
    private const float RotationSpeedModifier = 0.01f;

    public bool LockCursor = false;
    //public bool AllowKeyMovement = false;

    public float RotationSpeed = 10.0f;
    public float PanSpeed = 1.0f;
    public float ZoomSpeed = 1.0f;

    //public float MoveSpeed = 10.0f;
    //public float MoveSpeedIncrement = 2.5f;
    //public float SprintMultiplier = 10.0f;

    public UnityEvent<Ray, string> OnClickEvents;

    //private float inputChangeSpeed;
    //private float inputVertical;
    //private Vector2 inputMove;
    private Vector2 mouseDelta;
    private float scroll;
    private bool leftShift, alt, leftClick, rightClick, middleClick;
    private bool leftClicked;

    private Ray mouseRay;

    private InputActionMap map;
    private InputAction lookAction;
    //private InputAction speedAction;
    //private InputAction moveAction;
    //private InputAction yMoveAction;

    private Camera cam;


    private void Awake()
    {
        cam = GetComponent<Camera>();
        RegisterInputs();

        if (LockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Start()
    {
        map?.Enable();
    }

    private void OnEnable()
    {
        map?.Enable();
    }

    private void OnDisable()
    {
        map?.Disable();
    }

    private void OnDestroy()
    {
        map?.Disable();
        map?.Dispose();
    }

    private void RegisterInputs()
    {
        map = new InputActionMap("Free Camera");

        lookAction = map.AddAction("look", binding: "<Mouse>/delta");
        //moveAction = map.AddAction("move");
        //speedAction = map.AddAction("speed");
        //yMoveAction = map.AddAction("yMove");

        lookAction.AddBinding("<Gamepad>/rightStick").WithProcessor("scaleVector2(x=15, y=15)");
        //moveAction.AddCompositeBinding("Dpad")
        //    .With("Up", "<Keyboard>/w")
        //    .With("Up", "<Keyboard>/upArrow")
        //    .With("Down", "<Keyboard>/s")
        //    .With("Down", "<Keyboard>/downArrow")
        //    .With("Left", "<Keyboard>/a")
        //    .With("Left", "<Keyboard>/leftArrow")
        //    .With("Right", "<Keyboard>/d")
        //    .With("Right", "<Keyboard>/rightArrow");
        //speedAction.AddCompositeBinding("Dpad")
        //    .With("Up", "<Keyboard>/home")
        //    .With("Down", "<Keyboard>/end");
        //yMoveAction.AddCompositeBinding("Dpad")
        //    .With("Up", "<Keyboard>/pageUp")
        //    .With("Down", "<Keyboard>/pageDown")
        //    .With("Up", "<Keyboard>/space")
        //    .With("Down", "<Keyboard>/ctrl");
    }

    

    void UpdateInputs()
    {
        mouseDelta = lookAction.ReadValue<Vector2>();
        //inputMove = moveAction.ReadValue<Vector2>();
        //inputVertical = yMoveAction.ReadValue<Vector2>().y;
        //inputChangeSpeed = speedAction.ReadValue<Vector2>().y;

        leftShift = Keyboard.current.leftShiftKey.isPressed;
        alt = Keyboard.current.altKey.isPressed;

        leftClick = Mouse.current.leftButton.isPressed;
        leftClicked = Mouse.current.leftButton.wasPressedThisFrame;
        rightClick = Mouse.current.rightButton.isPressed;
        middleClick = Mouse.current.middleButton.isPressed;
        scroll = Mouse.current.scroll.ReadValue().y;

        mouseRay = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
    }

    void Update()
    {
        if (DebugManager.instance.displayRuntimeUI) { return; }

        UpdateInputs();

        //if (inputChangeSpeed != 0.0f)
        //{
        //    MoveSpeed += inputChangeSpeed * MoveSpeedIncrement;
        //    if (MoveSpeed < MoveSpeedIncrement) MoveSpeed = MoveSpeedIncrement;
        //}

        if (leftClicked) { OnClickEvents?.Invoke(mouseRay, "click"); }
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            if (Physics.Raycast(mouseRay, out RaycastHit hit))
            {
                VoxelManager.ChangeTerrain(hit.point, true, Constants.DefaultTerrainChangeSize, Constants.DefaultTerrainChangeMaterial);
            }
        }
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            if (Physics.Raycast(mouseRay, out RaycastHit hit))
            {
                VoxelManager.ChangeTerrain(hit.point, false, Constants.DefaultTerrainChangeSize, Constants.DefaultTerrainChangeMaterial);
            }
        }

        if (rightClick)
        {
            if (mouseDelta != Vector2.zero)
            {
                float rotationX = transform.localEulerAngles.x;
                float newRotationY = transform.localEulerAngles.y + mouseDelta.x * RotationSpeed * RotationSpeedModifier;

                // Weird clamping code due to weird Euler angle mapping...
                float newRotationX = (rotationX - mouseDelta.y * RotationSpeed * RotationSpeedModifier);
                if (rotationX <= 90.0f && newRotationX >= 0.0f)
                    newRotationX = Mathf.Clamp(newRotationX, 0.0f, 90.0f);
                if (rotationX >= 270.0f)
                    newRotationX = Mathf.Clamp(newRotationX, 270.0f, 360.0f);

                transform.localRotation = Quaternion.Euler(newRotationX, newRotationY, 0f);
            }
        }
        else if (middleClick && alt)
        {
            if (Physics.Raycast(mouseRay, out RaycastHit hit))
            {
                float d = Vector3.Dot(transform.position - hit.point, transform.forward) / Vector3.Dot(transform.forward, transform.forward);
                transform.position = hit.point + d * transform.forward;
            }
        }
        else if (middleClick)
        {
            transform.position += transform.up * PanSpeed * -mouseDelta.y * RotationSpeedModifier;
            transform.position += transform.right * PanSpeed * -mouseDelta.x * RotationSpeedModifier;
        }
        
        if (scroll != 0f)
        {
            transform.position += transform.forward * ZoomSpeed * scroll * RotationSpeedModifier;
        }

        //if (AllowKeyMovement)
        //{
        //    float moveSpeed = Time.deltaTime * MoveSpeed;
        //    if (leftShift) { moveSpeed *= SprintMultiplier; }
        //    transform.position += transform.forward * moveSpeed * inputMove.y;
        //    transform.position += transform.right * moveSpeed * inputMove.x;
        //    transform.position += Vector3.up * moveSpeed * inputVertical;
        //}
    }
}
