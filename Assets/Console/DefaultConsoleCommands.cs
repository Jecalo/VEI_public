using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using UConsole;
using System.Reflection;

public static class DefaultConsoleCommands
{
    #region Logging
    public static void Log(object obj)
    {
        Debug.Log(obj);
    }

    public static void LogWarning(object obj)
    {
        Debug.LogWarning(obj);
    }

    public static void LogError(object obj)
    {
        Debug.LogError(obj);
    }

    public static void LogFormat(string str, object[] args)
    {
        Debug.LogFormat(str, args);
    }

    public static void LogWarningFormat(string str, object[] args)
    {
        Debug.LogWarningFormat(str, args);
    }

    public static void LogErrorFormat(string str, object[] args)
    {
        Debug.LogErrorFormat(str, args);
    }

    #endregion


    #region Editor
#if UNITY_EDITOR
    public static void EditorPause()
    {
        Debug.Break();
    }
#endif
    #endregion


    #region ObjectManipulation

    public static GameObject Find(string name)
    {
        return GameObject.Find(name);
    }

    public static UnityEngine.Object FindObjectOfType(string type)
    {
        Type t = Type.GetType(type, false, true);
        if (t != null) { return GameObject.FindObjectOfType(t); }
        else { return null; }
    }

    public static Component GetComponent(GameObject obj, string type)
    {
        if (obj == null) { return null; }
        Type t = Type.GetType(type, false, true);
        if (t != null) { return obj.GetComponent(t); }
        else { return null; }
    }

    public static Component GetComponentInChildren(GameObject obj, string type)
    {
        if (obj == null) { return null; }
        Type t = Type.GetType(type, false, true);
        if (t != null) { return obj.GetComponentInChildren(t); }
        else { return null; }
    }

    public static void SetPosition(GameObject obj, Vector3 position)
    {
        if (obj == null) { return; }
        obj.transform.position = position;
    }

    public static void SetRotation(GameObject obj, Vector3 rotation)
    {
        if (obj == null) { return; }
        obj.transform.rotation = Quaternion.Euler(rotation);
    }

    public static void SetScale(GameObject obj, Vector3 scale)
    {
        if (obj == null) { return; }
        obj.transform.localScale = scale;
    }

    public static void Translate(GameObject obj, Vector3 position)
    {
        if (obj == null) { return; }
        obj.transform.position = obj.transform.position + position;
    }

    public static void Rotate(GameObject obj, Vector3 rotation)
    {
        if (obj == null) { return; }
        obj.transform.rotation = Quaternion.Euler(rotation) * obj.transform.rotation;
    }

    public static void Scale(GameObject obj, Vector3 scale)
    {
        if (obj == null) { return; }
        obj.transform.localScale = Vector3.Scale(obj.transform.localScale, scale);
    }

    public static GameObject SpawnPrimitive(PrimitiveType type)
    {
        return GameObject.CreatePrimitive(type);
    }

    #endregion

    #region DataGathering

    public static void Time()
    {
        Debug.Log(DateTime.Now);
    }

    public static void SystemInfo()
    {
        System.Text.StringBuilder sb = new();
        sb.Append(DateTime.Now);
        sb.Append('\n');
        sb.Append(UnityEngine.SystemInfo.operatingSystem);
        sb.Append('\n');
        sb.Append(UnityEngine.SystemInfo.deviceModel);
        sb.Append('\n');
        sb.AppendFormat("Job worker threads: {0}/{1}", JobsUtility.JobWorkerCount, JobsUtility.JobWorkerMaximumCount);
        sb.Append('\n');
        sb.Append("RAM " + UnityEngine.SystemInfo.systemMemorySize + "MB");
        sb.Append('\n');
        sb.Append(UnityEngine.SystemInfo.processorType);
        sb.Append('\n');
        sb.Append(UnityEngine.SystemInfo.graphicsDeviceName);
        Debug.Log(sb);
    }

    #endregion


    #region Reflection

    public static Type TypeOf(object obj)
    {
        return obj?.GetType();
    }

    public static object GetField(object obj, string field)
    {
        Type t = obj.GetType();

        var f = t.GetField(field, BindingFlags.IgnoreCase | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        if (f != null) { return f.GetValue(obj); }

        var p = t.GetProperty(field, BindingFlags.IgnoreCase | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        if (p != null) { return p.GetValue(obj); }

        return null;
    }

    public static void SetField(object obj, string field, object value)
    {
        Type t = obj.GetType();

        var f = t.GetField(field, BindingFlags.IgnoreCase | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        if (f != null) { f.SetValue(obj, value); }

        var p = t.GetProperty(field, BindingFlags.IgnoreCase | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        if (p != null) { p.SetValue(obj, value); }
    }

    public static object InvokeMethod(object obj, string method, object[] parameters)
    {
        Type t = obj.GetType();

        var f = t.GetMethod(method, BindingFlags.IgnoreCase | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        if (f != null) { return f.Invoke(obj, parameters); }

        return null;
    }

    public static object InvokeMethod(object obj, string method)
    {
        Type t = obj.GetType();

        var f = t.GetMethod(method, BindingFlags.IgnoreCase | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        if (f != null) { return f.Invoke(obj, null); }

        return null;
    }

    public static object InvokeMethodStatic(string type, string method)
    {
        Type t = Type.GetType(type, false, true);

        if (t == null) { return null; }

        var f = t.GetMethod(method, BindingFlags.IgnoreCase | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        if (f != null) { return f.Invoke(null, null); }

        return null;
    }

    public static object InvokeMethodStatic(string type, string method, object[] parameters)
    {
        Type t = Type.GetType(type, false, true);

        if (t == null) { return null; }

        var f = t.GetMethod(method, BindingFlags.IgnoreCase | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        if (f != null) { return f.Invoke(null, parameters); }

        return null;
    }

    #endregion

    #region Console

    public static void Clear()
    {
        ConsoleController.Clear();
    }

    public static void TimeCommands(bool enable)
    {
        ConsoleController.TimeCommands(enable);
    }

    public static void TimeCommands()
    {
        ConsoleController.TimeCommands();
    }

    #endregion
}
