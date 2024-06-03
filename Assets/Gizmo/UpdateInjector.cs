using System.Collections;
using System.Collections.Generic;
using UnityEngine;



//This static class instantiates a gameobject at the start of the game (or when entering play mode in the editor)
//This gameobject has the preupdate, update and postupdate injector components.
//All events registered on this class are called at appropiate times.
public static class UpdateInjector
{
    public delegate void UpdateCallback();

    public static event UpdateCallback OnPreUpdate;
    public static event UpdateCallback OnUpdate;
    public static event UpdateCallback OnPostUpdate;

    private static GameObject Instance = null;

    static UpdateInjector()
    {
#if UNITY_EDITOR
        if (!UnityEditor.EditorApplication.isPlaying) { Debug.LogWarning("Injector is being initialized while not playing."); return; }
#endif
        if (Instance != null) { Debug.LogError("Injector instance already present"); return; }

        Instance = new GameObject("UpdateInjector");
        Object.DontDestroyOnLoad(Instance);
        Instance.AddComponent<Injection_PreUpdate>();
        Instance.AddComponent<Injection_Update>();
        Instance.AddComponent<Injection_PostUpdate>();
    }


    public static void InvokeOnPreUpdate()
    {
        if (OnPreUpdate != null) { OnPreUpdate(); }
    }

    public static void InvokeOnUpdate()
    {
        if (OnUpdate != null) { OnUpdate(); }
    }

    public static void InvokeOnPostUpdate()
    {
        if (OnPostUpdate != null) { OnPostUpdate(); }
    }
}
