using System;
using System.Collections;
using UnityEngine.Networking;

// Capa de datos de CheckPractica: un método por request (una llamada, un
// resultado). No parsea el JSON ni decide el flujo entre pasos — eso se queda
// en CheckPractica. code == 0 en el callback significa falla de red.
public class CheckPracticaRepository
{
    private readonly SupabaseConfig supabaseConfig;

    public CheckPracticaRepository(SupabaseConfig supabaseConfig)
    {
        this.supabaseConfig = supabaseConfig;
    }

    // NOTA: si una política RLS filtrara la tabla alumnos, esto respondería 200
    // con [] y CheckPractica mostraría "Sin grupo asignado." aunque el alumno sí
    // tenga grupo. Comportamiento actual, preservado.
    public IEnumerator ObtenerGrupoDeAlumno(
        string accessToken, int alumnoId,
        Action<bool, long, string> onComplete)
    {
        string url = $"{supabaseConfig.url}/rest/v1/alumnos" +
                     $"?alumno_id=eq.{alumnoId}" +
                     $"&select=grupo_id" +
                     $"&limit=1";

        using (var req = UnityWebRequest.Get(url))
        {
            SetupHeaders(req, accessToken);
            yield return req.SendWebRequest();
            Completar(req, onComplete);
        }
    }

    // NOTA: si una política RLS filtrara practicas_grupo, la respuesta sería 200
    // con [] y CheckPractica lo trataría como "no hay práctica activa" y mandaría
    // al alumno a la escena de sin práctica. Comportamiento actual, preservado.
    public IEnumerator BuscarPracticaActiva(
        string accessToken, int grupoId, DateTime ahoraUtc,
        Action<bool, long, string> onComplete)
    {
        string ahora = ahoraUtc.ToString("yyyy-MM-ddTHH:mm:ssZ");
        string url = $"{supabaseConfig.url}/rest/v1/practicas_grupo" +
                     $"?grupo_id=eq.{grupoId}" +
                     $"&fecha_inicio=lte.{ahora}" +
                     $"&fecha_fin=gte.{ahora}" +
                     $"&select=practica_id" +
                     $"&limit=1";

        using (var req = UnityWebRequest.Get(url))
        {
            SetupHeaders(req, accessToken);
            yield return req.SendWebRequest();
            Completar(req, onComplete);
        }
    }

    // NOTA: este es el guard contra repetir una práctica ya hecha. Si una
    // política RLS ocultara las filas propias de resultados, la respuesta sería
    // 200 con [] y CheckPractica lo tomaría como "no existe resultado", dejando
    // al alumno repetir la práctica. El guard depende de que el SELECT sobre los
    // resultados propios esté permitido. Comportamiento actual, preservado.
    public IEnumerator BuscarResultado(
        string accessToken, int alumnoId, int practicaId,
        Action<bool, long, string> onComplete)
    {
        string url = $"{supabaseConfig.url}/rest/v1/resultados" +
                     $"?alumno_id=eq.{alumnoId}" +
                     $"&practica_id=eq.{practicaId}" +
                     $"&select=resultado_id" +
                     $"&limit=1";

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
        req.timeout = 5;
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
