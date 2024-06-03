using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PreloadScene : EditorWindow
{
    [MenuItem("Tools/PreloadScene")]
    private static void Open()
    {
        GetWindow<PreloadScene>();
    }

    private void OnGUI()
    {
        EditorSceneManager.playModeStartScene = (SceneAsset)EditorGUILayout.ObjectField("Start Scene", EditorSceneManager.playModeStartScene, typeof(SceneAsset), false);
    }
}

[InitializeOnLoad]
public static class PlayModeStateChangedExample
{
    static PlayModeStateChangedExample()
    {
        EditorApplication.playModeStateChanged += Change;
    }

    private static void Change(PlayModeStateChange state)
    {
        if (EditorSceneManager.playModeStartScene != null && state == PlayModeStateChange.ExitingEditMode)
        {
            TextAsset txt = new TextAsset(EditorSceneManager.GetActiveScene().path);
            AssetDatabase.DeleteAsset("Assets/preload_tmp.asset");
            AssetDatabase.CreateAsset(txt, "Assets/preload_tmp.asset");
        }
        
    }
}