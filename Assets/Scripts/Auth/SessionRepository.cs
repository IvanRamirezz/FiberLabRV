using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// Capa de datos de SessionManager: un método por request (una llamada, un
// resultado). No parsea el JSON, no toca PlayerPrefs ni decide qué hacer con la
// respuesta — eso se queda en SessionManager. code == 0 en el callback
// significa falla de red. Nunca loguear tokens ni cuerpos con tokens.
public class SessionRepository
{
    [Serializable] private class SessionPatch { public string active_session_uuid; }

    private readonly SupabaseConfig supabaseConfig;

    public SessionRepository(SupabaseConfig supabaseConfig)
    {
        this.supabaseConfig = supabaseConfig;
    }

    // NOTA: si una política RLS ocultara la fila propia de usuarios, la respuesta
    // sería 200 con [] y SessionManager la tomaría como "sin fila": invalidaría la
    // sesión y mandaría a Login a TODOS los usuarios (falla cerrada y visible, no
    // silenciosa). Se usa dos veces en ValidarSesion (consulta inicial y reintento
    // tras renovar el token), con reglas distintas del lado de SessionManager.
    public IEnumerator ObtenerSesionActiva(
        string accessToken, int usuarioId,
        Action<bool, long, string> onComplete)
    {
        string url = $"{supabaseConfig.url}/rest/v1/usuarios" +
                     $"?usuario_id=eq.{usuarioId}" +
                     $"&select=active_session_uuid&limit=1";

        using (var req = UnityWebRequest.Get(url))
        {
            SetupHeaders(req, accessToken);
            yield return req.SendWebRequest();
            Completar(req, onComplete);
        }
    }

    // NOTA: con "Prefer: return=minimal", si una política RLS filtrara la fila
    // del usuario, PostgREST respondería 204 con CERO filas actualizadas y esto
    // se reportaría como éxito: SessionManager guardaría el UUID local pero la BD
    // conservaría otro, y la siguiente validación (SceneGuard) mostraría un falso
    // "sesión duplicada". Comportamiento actual, preservado.
    public IEnumerator GuardarSesionActiva(
        string accessToken, int usuarioId, string nuevoUuid,
        Action<bool, long, string> onComplete)
    {
        string url = $"{supabaseConfig.url}/rest/v1/usuarios?usuario_id=eq.{usuarioId}";
        byte[] bodyBytes = Encoding.UTF8.GetBytes(
            JsonUtility.ToJson(new SessionPatch { active_session_uuid = nuevoUuid })
        );

        yield return EnviarPatch(url, accessToken, bodyBytes, onComplete);
    }

    // El cuerpo es un literal con null real a propósito: JsonUtility serializaría
    // un string null como "" y eso cambiaría lo que queda en la BD. Por eso este
    // método NO se unifica con GuardarSesionActiva.
    public IEnumerator LimpiarSesionActiva(
        string accessToken, int usuarioId,
        Action<bool, long, string> onComplete)
    {
        string url = $"{supabaseConfig.url}/rest/v1/usuarios?usuario_id=eq.{usuarioId}";
        byte[] bodyBytes = Encoding.UTF8.GetBytes("{\"active_session_uuid\":null}");

        yield return EnviarPatch(url, accessToken, bodyBytes, onComplete);
    }

    // NOTA: el cuerpo se arma por concatenación, sin escapar JSON. Es inofensivo
    // con los refresh tokens reales de Supabase (alfanuméricos). Solo lleva
    // apikey, sin Authorization. Comportamiento actual, preservado.
    public IEnumerator RenovarToken(
        string refreshToken,
        Action<bool, long, string> onComplete)
    {
        string url = $"{supabaseConfig.url}/auth/v1/token?grant_type=refresh_token";
        byte[] bodyBytes = Encoding.UTF8.GetBytes(
            "{\"refresh_token\":\"" + refreshToken + "\"}"
        );

        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyBytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.timeout = 5;
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("apikey", supabaseConfig.anonKey);

            yield return req.SendWebRequest();

            Completar(req, onComplete);
        }
    }

    private void SetupHeaders(UnityWebRequest req, string accessToken)
    {
        req.downloadHandler = new DownloadHandlerBuffer();
        req.timeout = 5;
        req.SetRequestHeader("apikey", supabaseConfig.anonKey);
        req.SetRequestHeader("Authorization", "Bearer " + accessToken);
        req.SetRequestHeader("Accept", "application/json");
    }

    private IEnumerator EnviarPatch(
        string url, string accessToken, byte[] bodyBytes,
        Action<bool, long, string> onComplete)
    {
        using (var req = new UnityWebRequest(url, "PATCH"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyBytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.timeout = 5;
            req.SetRequestHeader("apikey", supabaseConfig.anonKey);
            req.SetRequestHeader("Authorization", "Bearer " + accessToken);
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Prefer", "return=minimal");

            yield return req.SendWebRequest();

            Completar(req, onComplete);
        }
    }

    private void Completar(UnityWebRequest req, Action<bool, long, string> onComplete)
    {
        if (IsNetworkFailure(req))
        {
            onComplete(false, 0, null);
            return;
        }

        bool ok = req.responseCode >= 200 && req.responseCode < 300;
        onComplete(ok, req.responseCode, req.downloadHandler.text);
    }

    private bool IsNetworkFailure(UnityWebRequest req) =>
        req.result == UnityWebRequest.Result.ConnectionError ||
        req.result == UnityWebRequest.Result.DataProcessingError;
}
