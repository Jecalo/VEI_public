using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class SingletonInstancer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AfterSceneLoaded()
    {
        if (GameObject.FindObjectOfType<EventSystem>() == null)
        {
            GameObject obj = new GameObject("EventSystem");
            obj.AddComponent<EventSystem>();
            obj.AddComponent<InputSystemUIInputModule>();
        }

        if (GameObject.FindObjectOfType<InputDirector>() == null)
        {
            GameObject obj = new GameObject("InputDirector");
            InputDirector comp = obj.AddComponent<InputDirector>();
            comp.InitialControlState = InputDirector.ControlState.Menu;
            comp.Initialize();
        }

        if (GameObject.FindObjectOfType<ConsoleController>() == null)
        {
            GameObject prefab = Resources.Load<GameObject>("CanvasDebug");
            GameObject.Instantiate(prefab);
        }
    }
}
