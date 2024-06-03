using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Reflection;
using Unity.VisualScripting;

public static class EditorTools
{
    [MenuItem("Tools/Test")]
    public static void Test()
    {

    }

    [MenuItem("Tools/TestWindow")]
    public static void TestWindow()
    {
        EditorWindow window = EditorWindow.GetWindow(typeof(TestEditorWindow));
        window.Show();
    }

    public class TestEditorWindow : EditorWindow
    {
        private string str = null;

        private void OnEnable()
        {
            Rect rect = this.position;
            rect.size = new Vector2(256, 64);
            this.position = rect;
        }

        private void OnGUI()
        {
            str = EditorGUILayout.TextField(str);
            if (GUILayout.Button("Do")) { Do(); }
        }

        private void Do()
        {
            
        }
    }
}
