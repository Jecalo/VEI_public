using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static InputDirector;

public class InputDirector : MonoBehaviour
{
    private static InputDirector Instance = null;

    public ControlState InitialControlState = ControlState.Menu;
    public bool AutoInitialize = false;

    private bool initialized = false;
    private DefaultControl actionAsset = null;

    private bool debugConsole;
    private bool isCursorLocked;
    private ControlState controlState;

    private void Awake()
    {
        if (Instance != null)
        {
#if !UNITY_EDITOR
            Debug.LogWarning("Memory manager already instanced.");
#endif
            Destroy(this);
        }
        else if (AutoInitialize) { Initialize(); }
    }

    private void OnDestroy()
    {
        if (!initialized) { return; }
        //Disable maps?
    }

    public void Initialize()
    {
        if (initialized) { return; }

        if (Instance != null) { Debug.LogError("Attempting to initialize a second InputDirector."); return; }

        Instance = this;
        initialized = true;
        DontDestroyOnLoad(gameObject);

        actionAsset = new DefaultControl();
        isCursorLocked = false;
        debugConsole = false;

        controlState = InitialControlState;

        actionAsset.Default.Restart.performed += _ => Director.Reload();
        actionAsset.Default.f0.performed += _ => RenderFeatureToggle.ToggleLast();
        actionAsset.Default.f9.performed += _ => RenderFeatureToggle.ToggleSencondLast();

        EnableControl(controlState);
        SetControlCursor(controlState);
    }

    private void SetCursorLock()
    {
        SetCursorLock(!isCursorLocked);
    }

    private void SetCursorLock(bool locked)
    {
        if (locked == isCursorLocked) { return; }
        isCursorLocked = locked;

        if (locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void EnableControl(ControlState state)
    {
        if (state == ControlState.Game)
        {
            actionAsset.Default.Enable();
        }
        else if (state == ControlState.Menu)
        {
            //Debug.LogWarning("Menu controls enabled");
        }
    }

    private void DisableControl(ControlState state)
    {
        if (state == ControlState.Game)
        {
            actionAsset.Default.Disable();
        }
        else if (state == ControlState.Menu)
        {
            //Debug.LogWarning("Menu controls disabled");
        }
    }

    private void SetControlCursor(ControlState state)
    {
        if (state == ControlState.Game)
        {
            SetCursorLock(true);
        }
        else if (state == ControlState.Menu)
        {
            SetCursorLock(false);
        }
    }

    public static void SetControlState(ControlState state)
    {
        if (Instance.controlState == state) { return; }

        if (Instance.debugConsole) { Instance.controlState = state; return; }

        Instance.DisableControl(Instance.controlState);
        Instance.EnableControl(state);
        Instance.SetControlCursor(state);
        Instance.controlState = state;
    }

    public static void SetConsoleState(bool enabled)
    {
        if (enabled == Instance.debugConsole) { return; }

        Instance.debugConsole = enabled;
        if (Instance.debugConsole)
        {
            Instance.DisableControl(Instance.controlState);
            Instance.SetCursorLock(false);
        }
        else
        {
            Instance.EnableControl(Instance.controlState);
            Instance.SetControlCursor(Instance.controlState);
        }
    }

    public static DefaultControl.DefaultActions GetGameMap() 
    {
        return Instance.actionAsset.Default;
    }

    public enum ControlState { Game, Menu }
}
