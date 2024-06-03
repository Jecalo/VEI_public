using UnityEngine;

public class Injection_Update : MonoBehaviour
{
    void Update()
    {
        UpdateInjector.InvokeOnUpdate();
    }
}
