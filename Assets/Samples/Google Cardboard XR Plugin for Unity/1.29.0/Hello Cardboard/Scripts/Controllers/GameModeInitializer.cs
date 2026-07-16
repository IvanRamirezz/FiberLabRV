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

    void Start()
    {
        bool vr = GameModeManager.IsVRMode;

        // Joystick camera only in Normal mode
        if (cameraLookController != null)
            cameraLookController.enabled = !vr;

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
