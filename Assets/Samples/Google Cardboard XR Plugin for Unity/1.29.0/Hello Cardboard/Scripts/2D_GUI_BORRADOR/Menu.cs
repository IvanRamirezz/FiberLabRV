using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;

public class Menu : MonoBehaviour
{
    public GameObject MenuPrincipal;

    // URL y API Key de Supabase
    private string url = "https://fjbadoisrcumdcwpcadt.supabase.co/rest/v1/usuarios";
    private string apiKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImZqYmFkb2lzcmN1bWRjd3BjYWR0Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDk4MzI3NDIsImV4cCI6MjA2NTQwODc0Mn0.h7BC7ZT_6en4xeJpNtrScLEo28nnDyvcqgmaiIWcEPA";

    public void AbrirMenu()
    {
        MenuPrincipal.SetActive(true);
    }



    public void NextScene()
    {
        // Luego cambiamos de escena
        //SceneManager.LoadScene("HelloCardboard");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
    public void EnvioDatos()
    {
        // Antes de iniciar, enviamos datos a Supabase
        StartCoroutine(EnviarDatosASupabase());
    }
    public void FinalizarJuego()
    {
        Console.WriteLine("Se ha finalizado la aplicación");
        Debug.Log("Sayonara");
        Application.Quit();
    }

    // Nuevo método que envía datos a Supabase
    public IEnumerator EnviarDatosASupabase()
    {
        // Genera datos aleatorios
        string nombre = "COCABRONPRUEBACELULAR" + UnityEngine.Random.Range(1000, 9999);
        int puntaje = UnityEngine.Random.Range(10, 100);

        // Crea el JSON
        string jsonData = $"{{\"nombre\":\"{nombre}\",\"puntaje\":{puntaje}}}";
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);

        // Configura la petición HTTP POST
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(jsonToSend);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("apikey", apiKey);
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        // Envía la petición
        yield return request.SendWebRequest();

        // Verifica resultado
        if (request.result == UnityWebRequest.Result.Success || request.responseCode == 201)
        {
            Debug.Log($"Usuario enviado a Supabase: {nombre} ({puntaje})");
        }
        else
        {
            Debug.LogError($"Error: {request.responseCode}\n{request.downloadHandler.text}");
        }
    }

    public IEnumerator StartXRTest()
    {
        if (XRGeneralSettings.Instance.Manager.activeLoader != null)
            yield break;

        yield return XRGeneralSettings.Instance.Manager.InitializeLoader();
        if (XRGeneralSettings.Instance.Manager.activeLoader == null)
        {
            Debug.LogError("Error: No se pudo inicializar el XR Loader.");
            yield break;
        }
        XRGeneralSettings.Instance.Manager.StartSubsystems();
        Debug.Log("Cardboard habilitado.");
    }

    public void StartXR()
    {
        StartCoroutine(StartXRTest());
    }

    public void StopXR()
    {
        if (XRGeneralSettings.Instance.Manager.activeLoader == null)
            return;

        XRGeneralSettings.Instance.Manager.StopSubsystems();
        XRGeneralSettings.Instance.Manager.DeinitializeLoader();
        Debug.Log("Cardboard deshabilitado.");
    }

}
