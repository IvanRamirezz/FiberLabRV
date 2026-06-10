using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;



public class LoginSupabase : MonoBehaviour
{
    [Header("Campos de entrada")]
    public TMP_InputField correoInput;
    public TMP_InputField passwordInput;

    [Header("Config Supabase")]
    private string urlAuth = "https://fjbadoisrcumdcwpcadt.supabase.co/auth/v1/token?grant_type=password";
    private string apiKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImZqYmFkb2lzcmN1bWRjd3BjYWR0Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDk4MzI3NDIsImV4cCI6MjA2NTQwODc0Mn0.h7BC7ZT_6en4xeJpNtrScLEo28nnDyvcqgmaiIWcEPA";

    public void IniciarSesion()
    {
        string correo = correoInput.text;
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(correo) || string.IsNullOrEmpty(password))
        {
            Debug.LogError("Ingresa correo y contraseña");
            return;
        }

        StartCoroutine(LoginRequest(correo, password));
    }

    IEnumerator LoginRequest(string correo, string password)
    {
        // Crear JSON con credenciales
        string jsonData = $"{{\"email\":\"{correo}\",\"password\":\"{password}\"}}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        UnityWebRequest request = new UnityWebRequest(urlAuth, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("apikey", apiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success || request.responseCode == 200)
        {
            string responseText = request.downloadHandler.text;
            Debug.Log("Login correcto: " + responseText);

            // Extraer el token (muy importante para futuras consultas)
            SupabaseSessionData session = JsonUtility.FromJson<SupabaseSessionData>(responseText);

            SesionUsuario.token = session.access_token;
            SesionUsuario.usuarioEmail = session.user.email;

            Debug.Log("Token: " + SesionUsuario.token);
            Debug.Log("Usuario: " + SesionUsuario.usuarioEmail);

            // Aquí podrías cargar tu escena principal
            SceneManager.LoadScene("Menu");
        }
        else
        {
            Debug.LogError($"Error en login: {request.responseCode}\n{request.downloadHandler.text}");
        }
    }
}

// Clases auxiliares para parsear la respuesta de Supabase
[System.Serializable]
public class SupabaseSessionData
{
    public string access_token;
    public SupabaseUser user;
}

[System.Serializable]
public class SupabaseUser
{
    public string id;
    public string email;
}

// Clase estática para guardar datos de sesión globalmente
public static class SesionUsuario
{
    public static string token;
    public static string usuarioEmail;
}
