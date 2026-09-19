using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// Capa de datos de LoginSupabaseREST: un método por request (una llamada, un
// resultado). No parsea el JSON ni decide el flujo entre pasos — eso se queda
// en LoginSupabaseREST. code == 0 en el callback significa falla de red.
public class LoginRepository
{
    [Serializable] private class LoginPayload { public string email; public string password; }

    private readonly SupabaseConfig supabaseConfig;

    public LoginRepository(SupabaseConfig supabaseConfig)
    {
        this.supabaseConfig = supabaseConfig;
    }

    // Nunca loguear el body de este request: lleva la contraseña.
    public IEnumerator IniciarSesion(
        string email, string password,
        Action<bool, long, string> onComplete)
    {
        byte[] body = Encoding.UTF8.GetBytes(
            JsonUtility.ToJson(new LoginPayload { email = email, password = password })
        );

        using (var req = new UnityWebRequest($"{supabaseConfig.url}/auth/v1/token?grant_type=password", "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.timeout = 5;
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("apikey", supabaseConfig.anonKey);
            req.SetRequestHeader("Authorization", "Bearer " + supabaseConfig.anonKey);

            yield return req.SendWebRequest();

            Completar(req, onComplete);
        }
    }

    // NOTA: si una política RLS filtrara la fila propia de usuarios, esto
    // respondería 200 con [] y LoginSupabaseREST mostraría "Credenciales
    // incorrectas" aunque el login sí fue válido. Comportamiento actual,
    // preservado.
    public IEnumerator ObtenerUsuarioId(
        string token, string authUid,
        Action<bool, long, string> onComplete)
    {
        string url = $"{supabaseConfig.url}/rest/v1/usuarios" +
                     $"?auth_uid=eq.{UnityWebRequest.EscapeURL(authUid)}" +
                     $"&select=usuario_id&limit=1";

        using (var req = UnityWebRequest.Get(url))
        {
            SetupDbHeaders(req, token);
            yield return req.SendWebRequest();
            Completar(req, onComplete);
        }
    }

    // NOTA (importante, con efecto en cadena): el ROL del usuario se decide
    // solo por esta consulta — cuerpo distinto de "[]" = alumno, "[]" = "otro".
    // Si una política RLS ocultara la fila propia de alumnos, la respuesta sería
    // 200 con [] y un ALUMNO real quedaría con rol = "otro" y sería enviado a la
    // escena de no-alumno (sceneNoAlumno, "Demo"), sin ningún error visible. De
    // ahí en adelante todo lo que trata distinto a los no-"alumno" lo afectaría:
    //   - QuestionarioFinal lo toma como MODO REVISIÓN: no guarda ni envía sus
    //     respuestas (se descartan en silencio) y lo manda a sceneModoRevision.
    //   - SatisfaccionGate lo deja pasar sin verificar si ya respondió.
    //   - Nunca se guarda alumno_id, así que CheckPractica/JoinGroup/etc. lo
    //     tratan como "Sesión inválida.".
    // Hoy funciona porque la política permite el SELECT sobre la fila propia;
    // es una dependencia fuerte. Comportamiento actual, preservado.
    public IEnumerator ObtenerAlumno(
        string token, long usuarioId,
        Action<bool, long, string> onComplete)
    {
        string url = $"{supabaseConfig.url}/rest/v1/alumnos" +
                     $"?alumno_id=eq.{usuarioId}" +
                     $"&select=alumno_id,grupo_id&limit=1";

        using (var req = UnityWebRequest.Get(url))
        {
            SetupDbHeaders(req, token);
            yield return req.SendWebRequest();
            Completar(req, onComplete);
        }
    }

    private void SetupDbHeaders(UnityWebRequest req, string token)
    {
        req.downloadHandler = new DownloadHandlerBuffer();
        req.timeout = 5;
        req.SetRequestHeader("apikey", supabaseConfig.anonKey);
        req.SetRequestHeader("Authorization", "Bearer " + token);
        req.SetRequestHeader("Accept", "application/json");
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
