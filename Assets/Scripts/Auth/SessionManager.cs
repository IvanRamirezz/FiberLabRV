using UnityEngine;
using UnityEngine.SceneManagement;
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

    private SessionRepository repository;

    // ── Unity ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        repository = new SessionRepository(supabaseConfig);
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
        bool ok = false;
        long code = 0;
        string body = null;
        yield return StartCoroutine(repository.ObtenerSesionActiva(accessToken, usuarioId,
            (success, responseCode, responseBody) => { ok = success; code = responseCode; body = responseBody; }));

        ConsultaSesion consulta = ClasificarPrimeraConsulta(ok, code);

        // sin red → dejar pasar sin bloquear
        if (consulta == ConsultaSesion.SinRed)
        {
            Debug.LogWarning("SessionManager: sin red, omitiendo validación.");
            onValida?.Invoke();
            yield break;
        }

        // token expirado → renovar y reintentar
        if (consulta == ConsultaSesion.TokenExpirado)
        {
            Debug.Log("SessionManager: token expirado, renovando...");
            bool renovado = false;
            yield return StartCoroutine(RefrescarToken(r => renovado = r));

            if (!renovado)
            {
                LimpiarSesionLocal();
                onInvalida?.Invoke();
                SceneManager.LoadScene(sceneLogin);
                yield break;
            }

            accessToken = PlayerPrefs.GetString("sb_access_token", "");
            yield return StartCoroutine(repository.ObtenerSesionActiva(accessToken, usuarioId,
                (success, responseCode, responseBody) => { ok = success; code = responseCode; body = responseBody; }));

            // en el reintento CUALQUIER falla (incluida la de red) invalida la sesión
            if (!ok)
            {
                LimpiarSesionLocal();
                onInvalida?.Invoke();
                SceneManager.LoadScene(sceneLogin);
                yield break;
            }
        }
        else if (consulta == ConsultaSesion.Rechazada)
        {
            LimpiarSesionLocal();
            onInvalida?.Invoke();
            SceneManager.LoadScene(sceneLogin);
            yield break;
        }

        // comparar UUIDs
        ResultadoSesion resultado = CompararSesion(body, sessionUuid);

        if (resultado == ResultadoSesion.SinFila)
        {
            LimpiarSesionLocal();
            onInvalida?.Invoke();
            SceneManager.LoadScene(sceneLogin);
            yield break;
        }

        if (resultado == ResultadoSesion.Duplicada)
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

    // ── Seams: clasifican la respuesta (sin PlayerPrefs, callbacks ni escenas) ──

    private enum ConsultaSesion { SinRed, TokenExpirado, Rechazada, Ok }
    private enum ResultadoSesion { SinFila, Duplicada, Valida }

    // Primera consulta de validación: SinRed deja pasar (fail-open); 401 intenta
    // renovar el token; cualquier otro error HTTP invalida la sesión.
    private ConsultaSesion ClasificarPrimeraConsulta(bool ok, long code)
    {
        if (!ok && code == 0) return ConsultaSesion.SinRed;
        if (code == 401) return ConsultaSesion.TokenExpirado;
        if (!ok) return ConsultaSesion.Rechazada;
        return ConsultaSesion.Ok;
    }

    // Compara el UUID de la BD con el local. Los dos logs de UUID solo salen si
    // hay fila, igual que antes.
    private ResultadoSesion CompararSesion(string body, string uuidLocal)
    {
        var arr = JsonHelper.FromJson<UsuarioSesion>(body);
        if (arr == null || arr.Length == 0) return ResultadoSesion.SinFila;

        string sessionEnBd = (arr[0].active_session_uuid ?? "").Trim();

        Debug.Log($"UUID local: '{uuidLocal}'");
        Debug.Log($"UUID en BD: '{sessionEnBd}'");

        return sessionEnBd != uuidLocal ? ResultadoSesion.Duplicada : ResultadoSesion.Valida;
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

        bool ok = false;
        long code = 0;
        string body = null;
        yield return StartCoroutine(repository.GuardarSesionActiva(accessToken, usuarioId, newUuid,
            (success, responseCode, responseBody) => { ok = success; code = responseCode; body = responseBody; }));

        if (ok)
        {
            PlayerPrefs.SetString("session_uuid", newUuid);
            PlayerPrefs.Save();
            Debug.Log($"SessionManager: sesión registrada → '{newUuid}'");
        }
        else
        {
            Debug.LogWarning($"SessionManager: error al registrar sesión. " +
                             $"Code: {code} | {body}");
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
            bool ok = false;
            long code = 0;
            string body = null;
            yield return StartCoroutine(repository.LimpiarSesionActiva(accessToken, usuarioId,
                (success, responseCode, responseBody) => { ok = success; code = responseCode; body = responseBody; }));

            if (!ok)
            {
                Debug.LogWarning($"SessionManager: error al cerrar sesión en el servidor. " +
                                 $"Code: {code} | {body}");
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

        bool ok = false;
        long code = 0;
        string body = null;
        yield return StartCoroutine(repository.RenovarToken(refreshToken,
            (success, responseCode, responseBody) => { ok = success; code = responseCode; body = responseBody; }));

        if (!ProcesarRenovacion(ok, code, body, out AuthResponse auth)) { onResult(false); yield break; }

        PlayerPrefs.SetString("sb_access_token", auth.access_token);
        PlayerPrefs.SetString("sb_refresh_token", auth.refresh_token);
        PlayerPrefs.Save();
        Debug.Log("SessionManager: token renovado.");
        onResult(true);
    }

    // Devuelve true si la respuesta trae un access_token válido. Sin efectos sobre
    // PlayerPrefs. Una falla de red no se loguea (igual que antes); cualquier otra
    // respuesta inválida sí.
    private bool ProcesarRenovacion(bool ok, long code, string body, out AuthResponse auth)
    {
        auth = null;

        if (!ok && code == 0) return false;

        if (ok)
        {
            auth = JsonUtility.FromJson<AuthResponse>(body);
            if (auth != null && !string.IsNullOrEmpty(auth.access_token)) return true;
        }

        Debug.Log($"SessionManager: refresh_token inválido. Code: {code}");
        return false;
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

}