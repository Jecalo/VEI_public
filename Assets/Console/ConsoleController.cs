using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class ConsoleController : MonoBehaviour
{
    [SerializeField]
    private bool showLogs = false;          //Show logs in the console.
    [SerializeField]
    private bool logCommands = true;        //Send all commands submitted as a new log entry
    [SerializeField, Min(1)]
    private int maxLogCount = 64;           //Maximum logs stored and shown in the console
    [SerializeField, Min(32)]
    private int maxMessageSize = 256;       //Maximum command and log size.
    [SerializeField, Min(1)]
    private int maxCachedCommands = 32;     //Maximum stored commands for copying.

    [SerializeField]
    private ScrollRect scrollView = null;
    [SerializeField]
    private TMPro.TextMeshProUGUI textWindow = null;
    [SerializeField]
    private TMPro.TMP_InputField textInput = null;
    [SerializeField]
    private GameObject canvas = null;
    [SerializeField]
    private GameObject console = null;

    [SerializeField]
    private bool autoInitialize = false;

    private System.Text.StringBuilder sb;
    private UConsole.StringCircularBuffer logBuffer;
    private UConsole.StringCircularBuffer commandBuffer;
    private ulong lastLogCounter;
    private int selectedLog;
    private bool refreshConsole;
    private bool setCaret;
    private int newCaretPos;
    private string hint;

    private UConsole.UConsole uconsole;
    private InputAction upArrow, downArrow;
    private InputAction consoleKey;
    private InputAction autocompleteKey;
    private bool consoleEnabled;

    private static ConsoleController Instance = null;
    private bool initialized = false;
    private readonly object logLock = new object();

    private bool timeCommands;
    private System.Diagnostics.Stopwatch sw;



    private void Awake()
    {
        if (Instance != null)
        {
#if !UNITY_EDITOR
            Debug.LogWarning("ConsoleController already instanced.");
#endif
            Destroy(canvas);
        }
        else if (autoInitialize) { Initialize(); }
    }

    private void Start()
    {
        //Keyboard.current.onTextInput += (char c) => { Debug.LogFormat("'{0}' ({1})", c, (int)c); };
        //lock (logLock) { RegenerateLogsShown(); }
    }

    private void OnDestroy()
    {
        if (!initialized) { return; }

        consoleKey.Disable();
        //consoleKey.performed -= SwitchState;
        consoleKey.Dispose();

        upArrow.Disable();
        upArrow.Dispose();

        downArrow.Disable();
        downArrow.Dispose();

        autocompleteKey.Disable();
        autocompleteKey.Dispose();

        if (showLogs) { Application.logMessageReceivedThreaded -= LogCallback; }
    }

    public void Initialize()
    {
        if (initialized) { return; }

        Instance = this;
        DontDestroyOnLoad(canvas);
        //DontDestroyOnLoad(EventSystem.current.gameObject);

#if DEBUG
        uconsole = new UConsole.UConsole(typeof(ConsoleCommandsTest));
#else
        uconsole = new UConsole.UConsole();
#endif

        sb = new();
        logBuffer = new(maxMessageSize, maxLogCount);
        commandBuffer = new(maxMessageSize, maxCachedCommands);
        consoleEnabled = false;
        lastLogCounter = 0;
        selectedLog = -1;
        refreshConsole = false;
        setCaret = false;
        newCaretPos = -1;
        hint = null;

        textInput.onSubmit.AddListener(Submit);
        textInput.characterLimit = maxMessageSize;

        consoleKey = new InputAction("DebugConsoleHotkey", InputActionType.Button, binding: "<Keyboard>/f2");
        consoleKey.performed += SwitchState;
        consoleKey.Enable();

        upArrow = new InputAction("DebugConsoleUp", InputActionType.Button, binding: "<Keyboard>/upArrow");
        upArrow.performed += UpSelect;
        downArrow = new InputAction("DebugConsoleDown", InputActionType.Button, binding: "<Keyboard>/downArrow");
        downArrow.performed += DownSelect;

        autocompleteKey = new InputAction("DebugConsoleHint", InputActionType.Button, binding: "<Keyboard>/tab");
        autocompleteKey.performed += Autocomplete;
        autocompleteKey.Enable();

        if (showLogs) { Application.logMessageReceivedThreaded += LogCallback; }

        if (console.activeSelf) { console.SetActive(false); }

        timeCommands = false;
        sw = new System.Diagnostics.Stopwatch();
    }
    
    private void SwitchState(InputAction.CallbackContext obj)
    {
        consoleEnabled = !consoleEnabled;
        console.SetActive(consoleEnabled);
        InputDirector.SetConsoleState(consoleEnabled);

        if (consoleEnabled)
        {
            textInput.ActivateInputField();
            upArrow.Enable();
            downArrow.Enable();
            autocompleteKey.Enable();
        }
        else
        {
            upArrow.Disable();
            downArrow.Disable();
            autocompleteKey.Disable();
        }
    }

    private void Autocomplete(InputAction.CallbackContext obj)
    {
        if (!textInput.isFocused) { return; }

        //setCaretAtEnd = true;

        if (Keyboard.current.ctrlKey.isPressed)
        {
            if (uconsole.AutocompleteInfix(textInput.text, textInput.caretPosition, out string newInput, out int cursor, out string hint))
            {
                if (newInput != null) { textInput.text = newInput; setCaret = true; newCaretPos = cursor; }
                if (hint != null) { this.hint = hint; }
            }
        }
        else
        {
            if (uconsole.AutocompleteSuffix(textInput.text, textInput.caretPosition, out string newInput, out int cursor, out string hint))
            {
                if (newInput != null) {  textInput.text = newInput; setCaret = true; newCaretPos = cursor; }
                if (hint != null) { this.hint = hint; }
            }
        }
    }

    private void UpSelect(InputAction.CallbackContext ctx)
    {
        selectedLog++;
        int c = (int)commandBuffer.Counter;
        c = Mathf.Min(c, maxCachedCommands) - 1;
        if (selectedLog > c) { selectedLog = c; }
        textInput.text = commandBuffer.Get(selectedLog);
        setCaret = true;
        newCaretPos = -1;
    }

    private void DownSelect(InputAction.CallbackContext ctx)
    {
        if (selectedLog == -1) { return; }
        selectedLog--;
        if (selectedLog < 0) { selectedLog = 0; }
        textInput.text = commandBuffer.Get(selectedLog);
        setCaret = true;
        newCaretPos = -1;
    }

    private void Update()
    {
        if (!console.activeInHierarchy) { return; }
        if (refreshConsole) { RefreshConsole(); refreshConsole = false; }
        if (setCaret) { textInput.caretPosition = newCaretPos == -1 ? int.MaxValue : newCaretPos; setCaret = false; }

        if (!showLogs) { return; }
        lock (logLock)
        {
            if (hint != null) { logBuffer.Add(hint); hint = null; }
            if (lastLogCounter == logBuffer.Counter) { return; }
            RegenerateLogsShown();
            lastLogCounter = logBuffer.Counter;
            hint = null;
        }
    }

    private void RefreshConsole()
    {
        textWindow.rectTransform.sizeDelta = new Vector2(0f, textWindow.preferredHeight);
        scrollView.verticalNormalizedPosition = 0f;
    }

    private void RegenerateLogsShown()
    {
        logBuffer.CombineStrings(sb, "\n");
        textWindow.text = sb.ToString();
        refreshConsole = true;
    }

    private void Submit(string str)
    {
        textInput.ActivateInputField();

        if (str == "") { return; }

        textInput.text = "";

        if (logCommands) { Debug.LogFormat(">{0}", str); }

        commandBuffer.Add(str);
        selectedLog = -1;

        if (timeCommands)
        {
            sw.Restart();
            uconsole.Input(str);
            sw.Stop();
            Debug.LogFormat("Command time: {0}ms ({1})", sw.Elapsed.TotalMilliseconds, str);
        }
        else { uconsole.Input(str); }
    }

    private void LogCallback(string msg, string trace, LogType type)
    {
        lock (logLock)
        {
            logBuffer.Add(msg);
        }
    }

    public static void Clear()
    {
        lock (Instance.logLock)
        {
            Instance.logBuffer.Clear();
        }
    }

    public static void TimeCommands(bool enable)
    {
        Instance.timeCommands = enable;
    }

    public static void TimeCommands()
    {
        Instance.timeCommands = !Instance.timeCommands;
    }

    public static void SetUpdateConsole(bool state)
    {
        if (state == Instance.showLogs) { return; }
        Instance.showLogs = state;

        if (Instance.showLogs) { Application.logMessageReceivedThreaded += Instance.LogCallback; }
        else { Application.logMessageReceivedThreaded -= Instance.LogCallback; }
    }

    public void SetCanvas(GameObject canvas)
    {
        this.canvas = canvas;
    }

    public void SetConsole(GameObject console)
    {
        this.console = console;
    }

    public static UConsole.UConsole GetConsole()
    {
        return Instance.uconsole;
    }
}
