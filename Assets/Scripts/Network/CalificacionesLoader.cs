using System.Collections;
using UnityEngine;
using TMPro;

public class CalificacionesLoader : MonoBehaviour
{
    [Header("Supabase Config")]
    // Arrastra aquí el mismo asset SupabaseConfig que usas en Login
    public SupabaseConfig supabaseConfig;

    [Header("TextMeshPros - Calificaciones")]
    public TextMeshProUGUI practica1Text;
    public TextMeshProUGUI practica2Text;
    public TextMeshProUGUI practica3Text;
    public TextMeshProUGUI promedioText;

    [Header("Feedback")]
    public TMP_Text messageText;

    private CalificacionesRepository repository;
    private CalificacionesLogica logica;

    void Awake()
    {
        repository = new CalificacionesRepository(supabaseConfig);
        logica = new CalificacionesLogica();
    }

    // ── Botón Mostrar Calificaciones ──────────────────────────────────────────

    public void OnClickMostrarCalificaciones()
    {
        StopAllCoroutines();
        StartCoroutine(CargarCalificaciones());
    }

    // ── Carga de datos ────────────────────────────────────────────────────────

    private IEnumerator CargarCalificaciones()
    {
        SetMsg("Cargando calificaciones...");

        string accessToken = PlayerPrefs.GetString("sb_access_token", "");
        int alumnoId = PlayerPrefs.GetInt("alumno_id", 0);

        if (string.IsNullOrEmpty(accessToken) || alumnoId == 0)
        {
            SetMsg("Sesión inválida. Inicia sesión de nuevo.");
            yield break;
        }

        // Limpiar UI mientras carga
        SetPlaceholders();

        bool ok = false;
        long code = 0;
        string body = null;
        yield return StartCoroutine(repository.ObtenerCalificaciones(accessToken, alumnoId,
            (success, responseCode, responseBody) => { ok = success; code = responseCode; body = responseBody; }));

        ProcesarRespuesta(ok, code, body);
    }

    private void ProcesarRespuesta(bool ok, long code, string body)
    {
        if (!ok)
        {
            SetMsg(logica.MensajeError(code));
            return;
        }

        var resumen = logica.Interpretar(body);

        if (practica1Text) practica1Text.text = resumen.practica1;
        if (practica2Text) practica2Text.text = resumen.practica2;
        if (practica3Text) practica3Text.text = resumen.practica3;
        if (promedioText) promedioText.text = resumen.promedio;

        SetMsg("");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetPlaceholders()
    {
        if (practica1Text) practica1Text.text = CalificacionesLogica.Pendiente;
        if (practica2Text) practica2Text.text = CalificacionesLogica.Pendiente;
        if (practica3Text) practica3Text.text = CalificacionesLogica.Pendiente;
        if (promedioText) promedioText.text = CalificacionesLogica.Pendiente;
    }

    private void SetMsg(string msg)
    {
        if (messageText != null) messageText.text = msg;
    }
}
