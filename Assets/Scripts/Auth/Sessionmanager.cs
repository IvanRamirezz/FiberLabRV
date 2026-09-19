using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Text;
using System.Collections;
using Debug = UnityEngine.Debug;

public class SessionManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static SessionManager Instance { get; private set; }

    [Header("Supabase Config")]
    public SupabaseConfig supabaseConfig;

    [Header("Escenas")]
    public string sceneLogin = "Login";

    [Header("Popup sesión duplicada (opcional)")]
    public GameObject popupSesionDuplicada;

    // ── Clases internas ───────────────────────────────────────────────────────

    [System.Serializable] private class AuthResponse { public string access_token; public string refresh_token; }
    [System.Serializable] private class UsuarioSesion { public string active_session_uuid; }
    [System.Serializable] private class SessionPatch { public string active_session_uuid; }

    // ── Unity ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── API pública ───────────────────────────────────────────────────────────

    public void SetPopupSesionDuplicada(GameObject popup) => popupSesionDuplicada = popup;

    public void ValidarSesion(System.Action onValida = null, System.Action onInvalida = null)
        => StartCoroutine(ValidarSesionCoroutine(onValida, onInvalida));

    public void RegistrarSesion(System.Action onDone = null)
        => StartCoroutine(RegistrarSesionCoroutine(onDone));

    public void Logout()
        => StartCoroutine(LogoutCoroutine());

    public void IrAlLogin()
    {
        if (popupSesionDuplicada != null) popupSesionDuplicada.SetActive(false);
        SceneManager.LoadScene(sceneLogin);
    }

    // ── Validar sesión ────────────────────────────────────────────────────────
    // Funciona para alumno y profesor porque ambos guardan "usuario_id"

    private IEnumerator ValidarSesionCoroutine(System.Action onValida, System.Action onInvalida)
    {
        string accessToken = PlayerPrefs.GetString("sb_access_token", "");
        string sessionUuid = PlayerPrefs.GetString("session_uuid", "").Trim();
        int usuarioId = PlayerPrefs.GetInt("usuario_id", 0);

        // sin datos básicos → Login
        if (string.IsNullOrEmpty(accessToken) || usuarioId == 0 || string.IsNullOrEmpty(sessionUuid))
        {
            LimpiarSesionLocal();
            onInvalida?.Invoke();
            SceneManager.LoadScene(sceneLogin);
            yield break;
        }

        // consultar active_session_uuid en BD
        string urlUsuario = $"{supabaseConfig.url}/rest/v1/usuarios" +
                            $"?usuario_id=eq.{usuarioId}" +
                            $"&select=active_session_uuid&limit=1";

        var req = UnityWebRequest.Get(urlUsuario);
        SetupHeaders(req, accessToken);
        yield return req.SendWebRequest();

        bool networkFail = IsNetworkFailure(req);
        long code = req.responseCode;
        string body = (!networkFail && code >= 200 && code < 300)
                           ? req.downloadHandler.text : "";

        // sin red → dejar pasar sin bloquear
        if (networkFail)
        {
            Debug.LogWarning("SessionManager: sin red, omitiendo validación.");
            onValida?.Invoke();
            yield break;
        }

        // token expirado → renovar y reintentar
        if (code == 401)
        {
            Debug.Log("SessionManager: token expirado, renovando...");
            bool renovado = false;
            yield return StartCoroutine(RefrescarToken(ok => renovado = ok));

            if (!renovado)
            {
                LimpiarSesionLocal();
                onInvalida?.Invoke();
                SceneManager.LoadScene(sceneLogin);
                yield break;
            }

            accessToken = PlayerPrefs.GetString("sb_access_token", "");
            var req2 = UnityWebRequest.Get(urlUsuario);
            SetupHeaders(req2, accessToken);
            yield return req2.SendWebRequest();

            bool net2 = IsNetworkFailure(req2);
            bool http2 = req2.responseCode < 200 || req2.responseCode >= 300;
            body = (!net2 && !http2) ? req2.downloadHandler.text : "";

            if (net2 || http2)
            {
                LimpiarSesionLocal();
                onInvalida?.Invoke();
                SceneManager.LoadScene(sceneLogin);
                yield break;
            }
        }
        else if (code < 200 || code >= 300)
        {
            LimpiarSesionLocal();
            onInvalida?.Invoke();
            SceneManager.LoadScene(sceneLogin);
            yield break;
        }

        // comparar UUIDs
        var arr = JsonHelper.FromJson<UsuarioSesion>(body);
        if (arr == null || arr.Length == 0)
        {
            LimpiarSesionLocal();
            onInvalida?.Invoke();
            SceneManager.LoadScene(sceneLogin);
            yield break;
        }

        string sessionEnBd = (arr[0].active_session_uuid ?? "").Trim();

        Debug.Log($"UUID local: '{sessionUuid}'");
        Debug.Log($"UUID en BD: '{sessionEnBd}'");

        if (sessionEnBd != sessionUuid)
        {
            Debug.Log("SessionManager: sesión duplicada detectada.");
            LimpiarSesionLocal();
            onInvalida?.Invoke();
            MostrarPopupYRedirigir();
            yield break;
        }

        Debug.Log("SessionManager: sesión válida ✅");
        onValida?.Invoke();
    }

    // ── Registrar sesión nueva ────────────────────────────────────────────────
    // Genera un UUID nuevo, lo escribe en BD y lo guarda en PlayerPrefs

    private IEnumerator RegistrarSesionCoroutine(System.Action onDone)
    {
        string accessToken = PlayerPrefs.GetString("sb_access_token", "");
        int usuarioId = PlayerPrefs.GetInt("usuario_id", 0);

        if (string.IsNullOrEmpty(accessToken) || usuarioId == 0)
        {
            onDone?.Invoke();
            yield break;
        }

        string newUuid = System.Guid.NewGuid().ToString();
        string urlPatch = $"{supabaseConfig.url}/rest/v1/usuarios?usuario_id=eq.{usuarioId}";
        byte[] bodyBytes = Encoding.UTF8.GetBytes(
            JsonUtility.ToJson(new SessionPatch { active_session_uuid = newUuid })
        );

        var req = new UnityWebRequest(urlPatch, "PATCH");
        req.uploadHandler = new UploadHandlerRaw(bodyBytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.timeout = 5;
        req.SetRequestHeader("apikey", supabaseConfig.anonKey);
        req.SetRequestHeader("Authorization", "Bearer " + accessToken);
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Prefer", "return=minimal");

        yield return req.SendWebRequest();

        if (!IsNetworkFailure(req) && req.responseCode >= 200 && req.responseCode < 300)
        {
            PlayerPrefs.SetString("session_uuid", newUuid);
            PlayerPrefs.Save();
            Debug.Log($"SessionManager: sesión registrada → '{newUuid}'");
        }
        else
        {
            Debug.LogWarning($"SessionManager: error al registrar sesión. " +
                             $"Code: {req.responseCode} | {req.downloadHandler.text}");
        }

        onDone?.Invoke();
    }

    // ── Logout ────────────────────────────────────────────────────────────────
    // Escribe null en BD, limpia PlayerPrefs y vuelve al Login

    private IEnumerator LogoutCoroutine()
    {
        string accessToken = PlayerPrefs.GetString("sb_access_token", "");
        int usuarioId = PlayerPrefs.GetInt("usuario_id", 0);

        if (!string.IsNullOrEmpty(accessToken) && usuarioId != 0)
        {
            string urlPatch = $"{supabaseConfig.url}/rest/v1/usuarios?usuario_id=eq.{usuarioId}";
            byte[] bodyBytes = Encoding.UTF8.GetBytes("{\"active_session_uuid\":null}");

            var req = new UnityWebRequest(urlPatch, "PATCH");
            req.uploadHandler = new UploadHandlerRaw(bodyBytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.timeout = 5;
            req.SetRequestHeader("apikey", supabaseConfig.anonKey);
            req.SetRequestHeader("Authorization", "Bearer " + accessToken);
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Prefer", "return=minimal");

            yield return req.SendWebRequest();

            if (IsNetworkFailure(req) || req.responseCode < 200 || req.responseCode >= 300)
            {
                Debug.LogWarning($"SessionManager: error al cerrar sesión en el servidor. " +
                                 $"Code: {req.responseCode} | {req.downloadHandler.text}");
            }
        }

        LimpiarSesionLocal();
        SceneManager.LoadScene(sceneLogin);
    }

    // ── Renovar token JWT ─────────────────────────────────────────────────────

    private IEnumerator RefrescarToken(System.Action<bool> onResult)
    {
        string refreshToken = PlayerPrefs.GetString("sb_refresh_token", "");
        if (string.IsNullOrEmpty(refreshToken)) { onResult(false); yield break; }

        string url = $"{supabaseConfig.url}/auth/v1/token?grant_type=refresh_token";
        byte[] bodyBytes = Encoding.UTF8.GetBytes(
            "{\"refresh_token\":\"" + refreshToken + "\"}"
        );

        var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(bodyBytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.timeout = 5;
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("apikey", supabaseConfig.anonKey);

        yield return req.SendWebRequest();

        if (IsNetworkFailure(req)) { onResult(false); yield break; }

        if (req.responseCode >= 200 && req.responseCode < 300)
        {
            var auth = JsonUtility.FromJson<AuthResponse>(req.downloadHandler.text);
            if (auth != null && !string.IsNullOrEmpty(auth.access_token))
            {
                PlayerPrefs.SetString("sb_access_token", auth.access_token);
                PlayerPrefs.SetString("sb_refresh_token", auth.refresh_token);
                PlayerPrefs.Save();
                Debug.Log("SessionManager: token renovado.");
                onResult(true);
                yield break;
            }
        }

        Debug.Log($"SessionManager: refresh_token inválido. Code: {req.responseCode}");
        onResult(false);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void MostrarPopupYRedirigir()
    {
        if (popupSesionDuplicada != null) popupSesionDuplicada.SetActive(true);
        else SceneManager.LoadScene(sceneLogin);
    }

    private void LimpiarSesionLocal()
    {
        PlayerPrefs.DeleteKey("sb_access_token");
        PlayerPrefs.DeleteKey("sb_refresh_token");
        PlayerPrefs.DeleteKey("auth_uid");
        PlayerPrefs.DeleteKey("usuario_id");   // clave genérica nueva
        PlayerPrefs.DeleteKey("alumno_id");
        PlayerPrefs.DeleteKey("profesor_id");
        PlayerPrefs.DeleteKey("grupo_id");
        PlayerPrefs.DeleteKey("practica_id");
        PlayerPrefs.DeleteKey("practica_seleccionada");
        PlayerPrefs.DeleteKey("tiene_grupo");
        PlayerPrefs.DeleteKey("session_uuid");
        PlayerPrefs.DeleteKey("rol");          // clave genérica nueva
        PlayerPrefs.Save();
    }

    private void SetupHeaders(UnityWebRequest req, string accessToken)
    {
        req.downloadHandler = new DownloadHandlerBuffer();
        req.timeout = 5;
        req.SetRequestHeader("apikey", supabaseConfig.anonKey);
        req.SetRequestHeader("Authorization", "Bearer " + accessToken);
        req.SetRequestHeader("Accept", "application/json");
    }

    private bool IsNetworkFailure(UnityWebRequest req) =>
        req.result == UnityWebRequest.Result.ConnectionError ||
        req.result == UnityWebRequest.Result.DataProcessingError;
}