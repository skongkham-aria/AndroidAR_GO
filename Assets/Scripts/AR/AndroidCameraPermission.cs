using UnityEngine;
using UnityEngine.Android;

/// <summary>
/// Requests camera permission at runtime for Android
/// Attach this to your ARSession GameObject or Main Camera
/// </summary>
public class AndroidCameraPermission : MonoBehaviour
{
    private bool permissionRequested = false;

    void Start()
    {
        RequestCameraPermission();
    }

    void Update()
    {
        // Keep checking until permission is granted
        if (!permissionRequested)
        {
            RequestCameraPermission();
        }
    }

    private void RequestCameraPermission()
    {
#if PLATFORM_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Debug.Log("[Camera Permission] Requesting camera permission...");
            Permission.RequestUserPermission(Permission.Camera);
            permissionRequested = false;
        }
        else
        {
            Debug.Log("[Camera Permission] ✓ Camera permission granted!");
            permissionRequested = true;
            
            // Disable this script once permission is granted
            this.enabled = false;
        }
#else
        Debug.Log("[Camera Permission] Not on Android platform");
        this.enabled = false;
#endif
    }

    void OnApplicationFocus(bool hasFocus)
    {
        // Check permission again when app regains focus
        if (hasFocus)
        {
            permissionRequested = false;
        }
    }
}
