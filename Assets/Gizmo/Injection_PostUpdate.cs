using UnityEngine;



//Must be run after the default time in script execution order
public class Injection_PostUpdate : MonoBehaviour
{
    void Update()
    {
        UpdateInjector.InvokeOnPostUpdate();
    }
}
