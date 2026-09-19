using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// Capa de datos de JoinGroup: un método por request (una llamada, un
// resultado). No parsea el JSON ni decide el flujo entre pasos — eso se queda
// en JoinGroup. code == 0 en el callback significa falla de red.
public class JoinGroupRepository
{
    [Serializable] private class PatchBody { public int grupo_id; }

    private readonly SupabaseConfig supabaseConfig;

    public JoinGroupRepository(SupabaseConfig supabaseConfig)
    {
        this.supabaseConfig = supabaseConfig;
    }

    public IEnumerator BuscarGrupoPorCodigo(
        string accessToken, string codigo,
        Action<bool, long, string> onComplete)
    {
        string codigoEsc = UnityWebRequest.EscapeURL(codigo);
        string url = $"{supabaseConfig.url}/rest/v1/grupos" +
                     $"?codigo_acceso=eq.{codigoEsc}" +
                     $"&activo=eq.true" +
                     $"&select=grupo_id,codigo_acceso" +
                     $"&limit=1";

        using (var req = UnityWebRequest.Get(url))
        {
            req.downloadHandler = new DownloadHandlerBuffer();
            req.timeout = 5;
            req.SetRequestHeader("apikey", supabaseConfig.anonKey);
            req.SetRequestHeader("Authorization", "Bearer " + accessToken);
            req.SetRequestHeader("Accept", "application/json");

            yield return req.SendWebRequest();

            Completar(req, onComplete);
        }
    }

    // NOTA: con "Prefer: return=minimal", si una política RLS filtrara la fila
    // del alumno, PostgREST respondería 204 con CERO filas actualizadas y esto
    // se reportaría como éxito (JoinGroup guardaría grupo_id localmente sin que
    // quede en la BD). Comportamiento actual, preservado a propósito; si aparece
    // "se unió pero no quedó en el grupo", empezar por aquí.
    public IEnumerator AsignarAlumnoAGrupo(
        string accessToken, int alumnoId, int grupoId,
        Action<bool, long, string> onComplete)
    {
        string url = $"{supabaseConfig.url}/rest/v1/alumnos?alumno_id=eq.{alumnoId}";
        byte[] bodyBytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new PatchBody { grupo_id = grupoId }));

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
