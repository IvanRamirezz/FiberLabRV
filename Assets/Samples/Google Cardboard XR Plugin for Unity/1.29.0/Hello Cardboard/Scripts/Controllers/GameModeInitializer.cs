using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

/// <summary>
/// Place on any persistent GameObject in the game scene (e.g. Player or GameManager).
/// Reads the chosen mode on startup and configures XR + camera control accordingly.
/// </summary>
public class GameModeInitializer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The CameraLookController on Player (Camara)")]
    public CameraLookController cameraLookController;

    [Tooltip("Root GameObject holding TouchControlsBootstrap (on-screen joystick + look zone). Active only in Normal mode.")]
    public GameObject touchControlsRoot;

    void Start()
    {
        bool vr = GameModeManager.IsVRMode;

        // Camera look via the connected controller's right stick is only for
        // Normal mode. In VR mode the headset's gyroscope drives head look via
        // Cardboard's XR head tracking — CameraLookController writes directly
        // to transform.eulerAngles every frame, which would fight (and can
        // freeze) that tracking if left enabled here.
        if (cameraLookController != null)
            cameraLookController.enabled = !vr;

        // On-screen touch controls only in Normal mode — VR mode uses a connected
        // controller instead of the touchscreen
        if (touchControlsRoot != null)
            touchControlsRoot.SetActive(!vr);

        if (vr)
            EnableXR();
        else
            DisableXR();
    }

    static void EnableXR()
    {
        var manager = XRGeneralSettings.Instance?.Manager;
        if (manager == null) return;

        if (!manager.isInitializationComplete)
            manager.InitializeLoaderSync();

        manager.StartSubsystems();
    }

    static void DisableXR()
    {
        var manager = XRGeneralSettings.Instance?.Manager;
        if (manager == null) return;

        manager.StopSubsystems();
        manager.DeinitializeLoader();
    }
}
