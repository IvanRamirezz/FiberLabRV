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

    private SatisfaccionRepository repository;
    private SatisfaccionLogica logica;

    private void Awake()
    {
        repository = new SatisfaccionRepository(supabaseConfig);
        logica = new SatisfaccionLogica();
    }

    private void Start()
    {
        if (contenidoUI != null) contenidoUI.SetActive(false);
        StartCoroutine(CheckAndRedirect());
    }

    private System.Collections.IEnumerator CheckAndRedirect()
    {
        string rol = PlayerPrefs.GetString("rol", "alumno");
        string accessToken = PlayerPrefs.GetString("sb_access_token", "");
        int alumnoId = PlayerPrefs.GetInt("alumno_id", 0);
        if (!logica.DebeConsultar(rol, accessToken, alumnoId)) { MostrarUI(); yield break; }

        bool ok = false;
        long code = 0;
        string body = null;
        yield return StartCoroutine(repository.ConsultarEncuestaDeAlumno(accessToken, alumnoId,
            (success, responseCode, responseBody) => { ok = success; code = responseCode; body = responseBody; }));

        ProcesarRespuesta(ok, code, body, alumnoId);
    }

    private void ProcesarRespuesta(bool ok, long code, string body, int alumnoId)
    {
        var decision = logica.InterpretarRespuesta(ok, code, body);

        switch (decision)
        {
            case SatisfaccionLogica.Decision.MostrarPorErrorDeRed:
                Debug.LogWarning("SatisfaccionGate: error de red al verificar encuesta, se deja pasar.");
                MostrarUI();
                return;

            case SatisfaccionLogica.Decision.MostrarPorErrorHttp:
                Debug.LogWarning($"SatisfaccionGate: error {code} al verificar encuesta, se deja pasar. Body: {body}");
                MostrarUI();
                return;
        }

        Debug.Log($"SatisfaccionGate: respuesta de encuestas_satisfaccion para alumno_id={alumnoId}: {body}");

        if (decision == SatisfaccionLogica.Decision.YaRespondio)
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
