using System;
using System.Collections;
using UnityEngine.Networking;

// Capa de datos de SatisfaccionGate: hace el GET a Supabase y clasifica el
// resultado (red / código HTTP / cuerpo). No decide qué hacer con la
// respuesta ni parsea el JSON — eso se queda en SatisfaccionGate.
public class SatisfaccionRepository
{
    private readonly SupabaseConfig supabaseConfig;

    public SatisfaccionRepository(SupabaseConfig supabaseConfig)
    {
        this.supabaseConfig = supabaseConfig;
    }

    public IEnumerator ConsultarEncuestaDeAlumno(
        string accessToken, int alumnoId,
        Action<bool, long, string> onComplete)
    {
        string url = $"{supabaseConfig.url}/rest/v1/encuestas_satisfaccion?alumno_id=eq.{alumnoId}&select=encuesta_id&limit=1";

        using (var req = UnityWebRequest.Get(url))
        {
            req.downloadHandler = new DownloadHandlerBuffer();
            req.timeout = 12;
            req.SetRequestHeader("apikey", supabaseConfig.anonKey);
            req.SetRequestHeader("Authorization", "Bearer " + accessToken);
            req.SetRequestHeader("Accept", "application/json");

            yield return req.SendWebRequest();

            if (IsNetworkFailure(req))
            {
                onComplete(false, 0, null);
                yield break;
            }

            bool ok = req.responseCode >= 200 && req.responseCode < 300;
            onComplete(ok, req.responseCode, req.downloadHandler.text);
        }
    }

    private bool IsNetworkFailure(UnityWebRequest req) =>
        req.result == UnityWebRequest.Result.ConnectionError ||
        req.result == UnityWebRequest.Result.DataProcessingError;
}
