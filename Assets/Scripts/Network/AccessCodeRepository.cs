using System;
using System.Collections;
using UnityEngine.Networking;

// Capa de datos de AccessCodeGate: un método por request (una llamada, un
// resultado). No parsea el JSON ni decide el flujo entre pasos — eso se queda
// en AccessCodeGate. code == 0 en el callback significa falla de red.
public class AccessCodeRepository
{
    private readonly SupabaseConfig supabaseConfig;

    public AccessCodeRepository(SupabaseConfig supabaseConfig)
    {
        this.supabaseConfig = supabaseConfig;
    }

    // NOTA: si una política RLS filtrara la tabla practicas, esto respondería
    // 200 con [] y AccessCodeGate mostraría "Código incorrecto." — indistinguible
    // de un código realmente mal escrito. Comportamiento actual, preservado.
    public IEnumerator BuscarPracticaPorCodigo(
        string accessToken, string codigo,
        Action<bool, long, string> onComplete)
    {
        string codigoEsc = UnityWebRequest.EscapeURL(codigo);
        string url = $"{supabaseConfig.url}/rest/v1/practicas?codigo=eq.{codigoEsc}&select=id,titulo,codigo&limit=1";

        using (var req = UnityWebRequest.Get(url))
        {
            SetupHeaders(req, accessToken);
            yield return req.SendWebRequest();
            Completar(req, onComplete);
        }
    }

    // NOTA: este es el guard contra repetir una práctica ya hecha. Si una
    // política RLS ocultara las filas propias de resultados, la respuesta sería
    // 200 con [] y AccessCodeGate lo tomaría como "no existe resultado", dejando
    // al alumno repetir la práctica. El guard depende de que el SELECT sobre los
    // resultados propios esté permitido. Comportamiento actual, preservado.
    public IEnumerator BuscarResultado(
        string accessToken, long alumnoId, int practicaId,
        Action<bool, long, string> onComplete)
    {
        string url = $"{supabaseConfig.url}/rest/v1/resultados?alumno_id=eq.{alumnoId}&practica_id=eq.{practicaId}&select=id&limit=1";

        using (var req = UnityWebRequest.Get(url))
        {
            SetupHeaders(req, accessToken);
            yield return req.SendWebRequest();
            Completar(req, onComplete);
        }
    }

    private void SetupHeaders(UnityWebRequest req, string accessToken)
    {
        req.downloadHandler = new DownloadHandlerBuffer();
        req.timeout = 12;
        req.SetRequestHeader("apikey", supabaseConfig.anonKey);
        req.SetRequestHeader("Authorization", "Bearer " + accessToken);
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
