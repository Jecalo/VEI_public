using UnityEngine;



//Must be run before the default time in script execution order
public class Injection_PreUpdate : MonoBehaviour
{
    void Update()
    {
        UpdateInjector.InvokeOnPreUpdate();
    }
}
