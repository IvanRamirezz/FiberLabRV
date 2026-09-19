using System;
using System.Collections;
using System.Globalization;
using System.Text;
using UnityEngine.Networking;

// Capa de datos de P1_InstructionManager: arma y envía el POST a Supabase con
// la calificación calculada en el entorno RV. No decide el puntaje (eso se
// queda en P1_InstructionManager), solo sabe cómo mandarlo por HTTP.
public class P1_DatosRepository
{
    private readonly SupabaseConfig supabaseConfig;

    public P1_DatosRepository(SupabaseConfig supabaseConfig)
    {
        this.supabaseConfig = supabaseConfig;
    }

    public IEnumerator EnviarResultado(
        string accessToken, int alumnoId, int practicaId, float calificacion,
        int scoreStep2, int scoreStep5, int scoreStep6,
        Action<bool, string> onComplete)
    {
        string respuestasJson = "{" +
            $"\"identificacion_partes\":\"{scoreStep2}/4\"," +
            $"\"identificacion_fibras\":\"{scoreStep5}/4\"," +
            $"\"calculo_posicion_global\":\"{scoreStep6}/6\"," +
            $"\"puntuacion_total\":\"{scoreStep2 + scoreStep5 + scoreStep6}/14\"" +
        "}";

        string calificacionStr = calificacion.ToString("F2", CultureInfo.InvariantCulture);
        string bodyStr = "{" +
            $"\"alumno_id\":{alumnoId}," +
            $"\"practica_id\":{practicaId}," +
            $"\"calificacion\":{calificacionStr}," +
            $"\"respuestas_json\":{respuestasJson}" +
        "}";

        yield return Enviar("resultados", accessToken, bodyStr, onComplete);
    }

    private IEnumerator Enviar(
        string tabla, string accessToken, string bodyStr,
        Action<bool, string> onComplete)
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
            onComplete(false, "Error de red. Intenta de nuevo.");
            yield break;
        }

        if (req.responseCode < 200 || req.responseCode >= 300)
        {
            onComplete(false, $"Error {req.responseCode}: {req.downloadHandler.text}");
            yield break;
        }

        onComplete(true, null);
    }

    private bool IsNetworkFailure(UnityWebRequest req) =>
        req.result == UnityWebRequest.Result.ConnectionError ||
        req.result == UnityWebRequest.Result.DataProcessingError;
}
