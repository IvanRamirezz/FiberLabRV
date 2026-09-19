using System;
using System.Collections;
using UnityEngine.Networking;

// Capa de datos de CalificacionesLoader: hace el GET a Supabase y clasifica
// el resultado (red / código HTTP / cuerpo). No decide textos de error ni
// parsea el JSON — eso se queda en CalificacionesLoader.
public class CalificacionesRepository
{
    private readonly SupabaseConfig supabaseConfig;

    public CalificacionesRepository(SupabaseConfig supabaseConfig)
    {
        this.supabaseConfig = supabaseConfig;
    }

    public IEnumerator ObtenerCalificaciones(
        string accessToken, int alumnoId,
        Action<bool, long, string> onComplete)
    {
        string url =
            $"{supabaseConfig.url}/rest/v1/resultados" +
            $"?alumno_id=eq.{alumnoId}" +
            $"&practica_id=in.(1,2,3)" +
            $"&select=practica_id,calificacion";

        using (var request = UnityWebRequest.Get(url))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 12;
            request.SetRequestHeader("apikey", supabaseConfig.anonKey);
            request.SetRequestHeader("Authorization", "Bearer " + accessToken);
            request.SetRequestHeader("Accept", "application/json");

            yield return request.SendWebRequest();

            if (IsNetworkFailure(request))
            {
                onComplete(false, 0, null);
                yield break;
            }

            bool ok = request.responseCode >= 200 && request.responseCode < 300;
            onComplete(ok, request.responseCode, request.downloadHandler.text);
        }
    }

    private bool IsNetworkFailure(UnityWebRequest req) =>
        req.result == UnityWebRequest.Result.ConnectionError ||
        req.result == UnityWebRequest.Result.DataProcessingError;
}
