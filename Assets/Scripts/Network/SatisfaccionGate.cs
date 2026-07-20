using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

/// <summary>
/// Colócalo en Feedback.unity. Si el alumno ya tiene una fila en
/// encuestas_satisfaccion, salta directo al flujo post-encuesta en vez
/// de mostrar el cuestionario de nuevo.
/// </summary>
public class SatisfaccionGate : MonoBehaviour
{
    [Header("Supabase Config")]
    public SupabaseConfig supabaseConfig;

    [Header("Escena si ya respondió")]
    public string sceneYaRespondio = "EnvioExitoso";

    [System.Serializable] private class EncuestaRow { public long encuesta_id; }

    private void Start()
    {
        StartCoroutine(CheckAndRedirect());
    }

    private System.Collections.IEnumerator CheckAndRedirect()
    {
        string rol = PlayerPrefs.GetString("rol", "alumno");
        if (rol != "alumno") yield break;

        string accessToken = PlayerPrefs.GetString("sb_access_token", "");
        int alumnoId = PlayerPrefs.GetInt("alumno_id", 0);
        if (string.IsNullOrEmpty(accessToken) || alumnoId == 0) yield break;

        string url = $"{supabaseConfig.url}/rest/v1/encuestas_satisfaccion?alumno_id=eq.{alumnoId}&select=encuesta_id&limit=1";

        using (var req = UnityWebRequest.Get(url))
        {
            req.downloadHandler = new DownloadHandlerBuffer();
            req.timeout = 12;
            req.SetRequestHeader("apikey", supabaseConfig.anonKey);
            req.SetRequestHeader("Authorization", "Bearer " + accessToken);
            req.SetRequestHeader("Accept", "application/json");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.ConnectionError ||
                req.result == UnityWebRequest.Result.DataProcessingError)
            {
                Debug.LogWarning("SatisfaccionGate: error de red al verificar encuesta, se deja pasar.");
                yield break;
            }

            if (req.responseCode < 200 || req.responseCode >= 300)
            {
                Debug.LogWarning($"SatisfaccionGate: error {req.responseCode} al verificar encuesta, se deja pasar.");
                yield break;
            }

            var arr = JsonHelper.FromJson<EncuestaRow>(req.downloadHandler.text);
            if (arr != null && arr.Length > 0)
            {
                Debug.Log("SatisfaccionGate: alumno ya respondió la encuesta, saltando.");
                SceneManager.LoadScene(sceneYaRespondio);
            }
        }
    }
}
