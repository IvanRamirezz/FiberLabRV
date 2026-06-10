using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using TMPro;
using System.Text;
using Debug = UnityEngine.Debug;

public class LoginSupabaseREST : MonoBehaviour
{
    [Header("UI (TMP)")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_Text messageText;

    [Header("Scene")]
    public string sceneToLoad = "Bienvenida";

    [Header("Supabase")]
    public string supabaseUrl = "https://cxmsimqrtkqsrxzasptq.supabase.co";
    public string supabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImN4bXNpbXFydGtxc3J4emFzcHRxIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjgwODM3MzcsImV4cCI6MjA4MzY1OTczN30.YKtWzPapWVN58EMbs1F44xAiiKYP0U6Z_R2fMQXtTew";

    // ---------- API ----------
    [System.Serializable]
    private class LoginPayload
    {
        public string email;
        public string password;
    }

    [System.Serializable]
    private class AuthResponse
    {
        public string access_token;
        public string refresh_token;
        public User user;
    }

    [System.Serializable]
    private class User
    {
        public string id; // auth_uid (uuid)
        public string email;
    }

    public void OnClickLogin()
    {
        string email = emailInput.text.Trim();
        string pass = passwordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
        {
            SetMsg("Escribe correo y contraseña.");
            return;
        }

        StartCoroutine(SignInCoroutine(email, pass));
    }

    private System.Collections.IEnumerator SignInCoroutine(string email, string pass)
    {
        SetMsg("Iniciando sesión...");

        // Endpoint password grant
        // POST {supabaseUrl}/auth/v1/token?grant_type=password
        string url = $"{supabaseUrl}/auth/v1/token?grant_type=password";

        var payload = new LoginPayload { email = email, password = pass };
        string json = JsonUtility.ToJson(payload);
        byte[] body = Encoding.UTF8.GetBytes(json);

        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("apikey", supabaseAnonKey);

            // Algunos proyectos requieren esto también:
            req.SetRequestHeader("Authorization", "Bearer " + supabaseAnonKey);

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("HTTP Error: " + req.error + " | " + req.downloadHandler.text);
                SetMsg("Error de red. Intenta de nuevo.");
                yield break;
            }

            // Si credenciales son incorrectas, Supabase responde 400 con JSON error
            // pero Unity a veces lo marca como Success. Validamos contenido:
            string resText = req.downloadHandler.text;

            // Hack simple: si no trae access_token, lo consideramos fallo
            if (!resText.Contains("access_token"))
            {
                SetMsg("Credenciales incorrectas.");
                yield break;
            }

            var auth = JsonUtility.FromJson<AuthResponse>(resText);

            if (auth == null || string.IsNullOrEmpty(auth.access_token) || auth.user == null)
            {
                SetMsg("Credenciales incorrectas.");
                yield break;
            }

            // Guarda tokens y auth_uid para usar después (resultados, etc.)
            PlayerPrefs.SetString("sb_access_token", auth.access_token);
            PlayerPrefs.SetString("sb_refresh_token", auth.refresh_token);
            PlayerPrefs.SetString("auth_uid", auth.user.id);
            PlayerPrefs.Save();

            SetMsg("");
            SceneManager.LoadScene(sceneToLoad);
        }
    }

    private void SetMsg(string msg)
    {
        if (messageText != null) messageText.text = msg;
    }
}
