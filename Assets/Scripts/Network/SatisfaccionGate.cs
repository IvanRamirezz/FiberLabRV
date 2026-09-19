using UnityEngine;
using UnityEngine.SceneManagement;
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

    [Header("UI a ocultar mientras se verifica")]
    [Tooltip("Se desactiva de inmediato y solo se reactiva si el alumno NO ha respondido la encuesta")]
    public GameObject contenidoUI;

    [System.Serializable] private class EncuestaRow { public long encuesta_id; }

    private SatisfaccionRepository repository;

    private void Awake()
    {
        repository = new SatisfaccionRepository(supabaseConfig);
    }

    private void Start()
    {
        if (contenidoUI != null) contenidoUI.SetActive(false);
        StartCoroutine(CheckAndRedirect());
    }

    private System.Collections.IEnumerator CheckAndRedirect()
    {
        string rol = PlayerPrefs.GetString("rol", "alumno");
        if (rol != "alumno") { MostrarUI(); yield break; }

        string accessToken = PlayerPrefs.GetString("sb_access_token", "");
        int alumnoId = PlayerPrefs.GetInt("alumno_id", 0);
        if (string.IsNullOrEmpty(accessToken) || alumnoId == 0) { MostrarUI(); yield break; }

        bool ok = false;
        long code = 0;
        string body = null;
        yield return StartCoroutine(repository.ConsultarEncuestaDeAlumno(accessToken, alumnoId,
            (success, responseCode, responseBody) => { ok = success; code = responseCode; body = responseBody; }));

        ProcesarRespuesta(ok, code, body, alumnoId);
    }

    private void ProcesarRespuesta(bool ok, long code, string body, int alumnoId)
    {
        if (!ok && code == 0)
        {
            Debug.LogWarning("SatisfaccionGate: error de red al verificar encuesta, se deja pasar.");
            MostrarUI();
            return;
        }

        if (!ok)
        {
            Debug.LogWarning($"SatisfaccionGate: error {code} al verificar encuesta, se deja pasar. Body: {body}");
            MostrarUI();
            return;
        }

        Debug.Log($"SatisfaccionGate: respuesta de encuestas_satisfaccion para alumno_id={alumnoId}: {body}");

        var arr = JsonHelper.FromJson<EncuestaRow>(body);
        if (arr != null && arr.Length > 0)
        {
            Debug.Log("SatisfaccionGate: alumno ya respondió la encuesta, saltando.");
            SceneManager.LoadScene(sceneYaRespondio);
        }
        else
        {
            MostrarUI();
        }
    }

    private void MostrarUI()
    {
        if (contenidoUI != null) contenidoUI.SetActive(true);
    }
}
