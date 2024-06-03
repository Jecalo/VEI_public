using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Director : MonoBehaviour
{

    private static Director Instance = null;

    private void Awake()
    {
        if (Instance != null)
        {
#if !UNITY_EDITOR
            Debug.LogWarning("Director already instanced.");
#endif
            Destroy(this);
        }
        else { Initialize(); }
    }

    private void OnDestroy()
    {
        
    }

    private void Initialize()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void Reload()
    {
        if (Instance == null) { Debug.LogError("No director instanced."); return; }

        Debug.Log("Reloading current scene.");

        string scene = SceneManager.GetActiveScene().name;
        
        if (SceneManager.sceneCount > 1)
        {
            SceneManager.UnloadSceneAsync(scene).completed += (AsyncOperation op) =>
            {
                VoxelManager.SceneChanged();
                SceneManager.LoadScene(scene);
                DebugHelper.SceneChanged();
            };
        }
        else
        {
            VoxelManager.SceneChanged();
            SceneManager.LoadScene(scene);
            DebugHelper.SceneChanged();
        }
    }
}
