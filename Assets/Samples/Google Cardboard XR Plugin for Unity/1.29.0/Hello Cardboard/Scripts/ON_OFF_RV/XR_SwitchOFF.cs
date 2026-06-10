using UnityEngine;
using UnityEngine.XR.Management;
using System.Collections;

public class XRBootstrap : MonoBehaviour
{
    void Awake()
    {
        // Detenemos XR al inicio (aunque se haya inicializado)
        StartCoroutine(StopXR());
    }

    public IEnumerator StopXR()
    {
        if (XRGeneralSettings.Instance.Manager.activeLoader != null)
        {
            XRGeneralSettings.Instance.Manager.StopSubsystems();
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
            Debug.Log("XR detenido al inicio.");
        }
        yield return null;
    }

    public IEnumerator StartXRTest()
    {
        if (XRGeneralSettings.Instance.Manager.activeLoader != null)
            yield break; // Ya está activo

        Debug.Log("Inicializando XR...");
        yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

        if (XRGeneralSettings.Instance.Manager.activeLoader == null)
        {
            Debug.LogError("No se pudo inicializar XR.");
            yield break;
        }

        XRGeneralSettings.Instance.Manager.StartSubsystems();
        Debug.Log("XR activado.");
    }

    public void StartXR()
    {
        StartCoroutine(StartXRTest());
    }
}
