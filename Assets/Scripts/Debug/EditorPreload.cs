using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class EditorPreload : MonoBehaviour
{
    private int frame = 0;

#if !UNITY_EDITOR
    private void Awake()
    {
        Debug.LogError("EditorPrelad was instanced in a build.");
    }
#endif

#if UNITY_EDITOR
    private void Update()
    {
        if (frame == 2)
        {
            TextAsset txt = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/preload_tmp.asset");
            if (txt != null) { EditorSceneManager.LoadSceneInPlayMode(txt.text, new LoadSceneParameters(LoadSceneMode.Additive)); }
            Destroy(gameObject);
        }
        else { frame++; }
    }
#endif
}
