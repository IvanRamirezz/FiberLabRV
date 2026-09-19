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

    // ── Clases internas ───────────────────────────────────────────────────────

    [System.Serializable] private class Resultado { public int practica_id; public float calificacion; }
    [System.Serializable] private class ResultadosWrapper { public Resultado[] items; }

    private CalificacionesRepository repository;

    void Awake()
    {
        repository = new CalificacionesRepository(supabaseConfig);
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
            if (code == 0) SetMsg("Error de red. Intenta de nuevo.");
            else if (code == 401 || code == 403) SetMsg("Sin permisos para ver calificaciones (revisa RLS/policies).");
            else SetMsg("Error al consultar calificaciones.");
            return;
        }

        // Parsear JSON
        string wrapped = "{\"items\":" + body + "}";
        ResultadosWrapper data = JsonUtility.FromJson<ResultadosWrapper>(wrapped);

        float suma = 0f;
        int count = 0;

        if (data?.items != null)
        {
            foreach (var r in data.items)
            {
                switch (r.practica_id)
                {
                    case 1: if (practica1Text) practica1Text.text = r.calificacion.ToString("0.0"); break;
                    case 2: if (practica2Text) practica2Text.text = r.calificacion.ToString("0.0"); break;
                    case 3: if (practica3Text) practica3Text.text = r.calificacion.ToString("0.0"); break;
                }

                suma += r.calificacion;
                count++;
            }
        }

        if (promedioText)
            promedioText.text = count > 0 ? (suma / count).ToString("0.0") : "-";

        SetMsg("");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetPlaceholders()
    {
        if (practica1Text) practica1Text.text = "-";
        if (practica2Text) practica2Text.text = "-";
        if (practica3Text) practica3Text.text = "-";
        if (promedioText) promedioText.text = "-";
    }

    private void SetMsg(string msg)
    {
        if (messageText != null) messageText.text = msg;
    }
}
