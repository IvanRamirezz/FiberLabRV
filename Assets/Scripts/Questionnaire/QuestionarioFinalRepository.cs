using System;
using System.Collections;
using System.Text;
using UnityEngine.Networking;

// Capa de datos de QuestionarioFinal: arma y envía los POST a Supabase.
// No decide qué se envía (eso es de QuestionarioFinalLogica/Questionariofinal),
// solo sabe cómo mandarlo por HTTP y reportar (ok, código HTTP, cuerpo);
// código 0 = fallo de red. El mensaje al usuario lo arma MensajesHttp.
public class QuestionarioFinalRepository
{
    private readonly SupabaseConfig supabaseConfig;

    public QuestionarioFinalRepository(SupabaseConfig supabaseConfig)
    {
        this.supabaseConfig = supabaseConfig;
    }

    public IEnumerator EnviarSatisfaccion(
        string accessToken, int alumnoId, string respuestasJson,
        Action<bool, long, string> onComplete)
    {
        string bodyStr = "{" +
            $"\"alumno_id\":{alumnoId}," +
            $"\"respuestas_json\":{respuestasJson}" +
        "}";

        yield return Enviar("encuestas_satisfaccion", accessToken, bodyStr, onComplete);
    }

    public IEnumerator EnviarResultadoPractica(
        string accessToken, int alumnoId, int practicaId, string respuestasJson,
        Action<bool, long, string> onComplete)
    {
        string bodyStr = "{" +
            $"\"alumno_id\":{alumnoId}," +
            $"\"practica_id\":{practicaId}," +
            "\"calificacion\":null," +
            $"\"respuestas_json\":{respuestasJson}" +
        "}";

        yield return Enviar("resultados", accessToken, bodyStr, onComplete);
    }

    private IEnumerator Enviar(
        string tabla, string accessToken, string bodyStr,
        Action<bool, long, string> onComplete)
    {
        byte[] bodyBytes = Encoding.UTF8.GetBytes(bodyStr);
        string url = $"{supabaseConfig.url}/rest/v1/{tabla}";

        var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(bodyBytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.timeout = 10;
        req.SetRequestHeader("apikey", supabaseConfig.anonKey);
        req.SetRequestHeader("Authorization", "Bearer " + accessToken);
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Prefer", "return=minimal");

        yield return req.SendWebRequest();

        if (IsNetworkFailure(req))
        {
            onComplete(false, 0, null);
            yield break;
        }

        bool ok = req.responseCode >= 200 && req.responseCode < 300;
        onComplete(ok, req.responseCode, req.downloadHandler.text);
    }

    private bool IsNetworkFailure(UnityWebRequest req) =>
        req.result == UnityWebRequest.Result.ConnectionError ||
        req.result == UnityWebRequest.Result.DataProcessingError;
}
