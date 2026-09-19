using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Debug = UnityEngine.Debug;

public class CheckPractica : MonoBehaviour
{
    [Header("Escenas")]
    public string sceneConPractica = "InicioRV";
    public string sceneSinPractica = "Prac-NoDis";

    [Header("Feedback (opcional)")]
    public TMP_Text statusText;

    [Header("Supabase Config")]
    public SupabaseConfig supabaseConfig;

    [System.Serializable] private class PracticaGrupoRow { public int practica_id; }
    [System.Serializable] private class AlumnoGrupoRow { public int grupo_id; }
    [System.Serializable] private class ResultadoRow { public int resultado_id; }

    private CheckPracticaRepository repository;

    void Awake()
    {
        repository = new CheckPracticaRepository(supabaseConfig);
    }

    public void OnClickIniciar()
    {
        StartCoroutine(VerificarPractica());
    }

    private System.Collections.IEnumerator VerificarPractica()
    {
        SetStatus("Verificando práctica...");

        string accessToken = PlayerPrefs.GetString("sb_access_token", "");
        int alumnoId = PlayerPrefs.GetInt("alumno_id", 0);
        int grupoId = PlayerPrefs.GetInt("grupo_id", 0);

        if (string.IsNullOrEmpty(accessToken) || alumnoId == 0)
        {
            SetStatus("Sesión inválida.");
            yield break;
        }

        bool ok = false;
        long code = 0;
        string body = null;

        // Obtener grupo_id si no está en PlayerPrefs
        if (grupoId == 0)
        {
            yield return StartCoroutine(repository.ObtenerGrupoDeAlumno(accessToken, alumnoId,
                (success, responseCode, responseBody) => { ok = success; code = responseCode; body = responseBody; }));

            grupoId = ProcesarGrupo(ok, code, body);
            if (grupoId != 0)
            {
                PlayerPrefs.SetInt("grupo_id", grupoId);
                PlayerPrefs.Save();
            }
        }

        if (grupoId == 0)
        {
            SetStatus("Sin grupo asignado.");
            yield break;
        }

        // 1) Buscar práctica activa para este grupo
        yield return StartCoroutine(repository.BuscarPracticaActiva(accessToken, grupoId, System.DateTime.UtcNow,
            (success, responseCode, responseBody) => { ok = success; code = responseCode; body = responseBody; }));

        if (!ProcesarPracticaActiva(ok, code, body, out int practicaId)) yield break;

        // 2) Verificar si el alumno ya tiene resultado para esta práctica
        yield return StartCoroutine(repository.BuscarResultado(accessToken, alumnoId, practicaId,
            (success, responseCode, responseBody) => { ok = success; code = responseCode; body = responseBody; }));

        if (!ProcesarResultado(ok, code, body, practicaId)) yield break;

        // 3) Todo ok → guardar practica_id y navegar
        PlayerPrefs.SetInt("practica_id", practicaId);
        PlayerPrefs.Save();

        Debug.Log($">>> Práctica activa: {practicaId} → {sceneConPractica}");

        SetStatus("");
        SceneManager.LoadScene(sceneConPractica);
    }

    // Devuelve el grupo_id, o 0 si no hay (falla de red/HTTP o sin grupo). No
    // distingue entre ambos casos a propósito: comportamiento actual preservado
    // (hallazgo incidental pendiente: una falla real se ve como "Sin grupo asignado.").
    private int ProcesarGrupo(bool ok, long code, string body)
    {
        if (!ok) return 0;

        if (string.IsNullOrEmpty(body) || body == "[]" || body.Contains("\"grupo_id\":null"))
            return 0;

        var arr = JsonHelper.FromJson<AlumnoGrupoRow>(body);
        return (arr != null && arr.Length > 0) ? arr[0].grupo_id : 0;
    }

    // Devuelve true si hay práctica activa y el flujo debe continuar al paso 2.
    private bool ProcesarPracticaActiva(bool ok, long code, string body, out int practicaId)
    {
        practicaId = 0;

        if (!ok)
        {
            ReportarError(code);
            return false;
        }

        var practicas = JsonHelper.FromJson<PracticaGrupoRow>(body);

        if (practicas == null || practicas.Length == 0)
        {
            // No hay práctica activa
            SetStatus("");
            SceneManager.LoadScene(sceneSinPractica);
            return false;
        }

        practicaId = practicas[0].practica_id;
        return true;
    }

    // Devuelve true si NO hay resultado previo y el flujo debe continuar al paso 3.
    private bool ProcesarResultado(bool ok, long code, string body, int practicaId)
    {
        if (!ok)
        {
            ReportarError(code);
            return false;
        }

        var resultados = JsonHelper.FromJson<ResultadoRow>(body);

        if (resultados != null && resultados.Length > 0)
        {
            // Ya realizó esta práctica → sin práctica disponible
            Debug.Log($">>> Alumno ya realizó la práctica {practicaId} → {sceneSinPractica}");
            SetStatus("");
            SceneManager.LoadScene(sceneSinPractica);
            return false;
        }

        return true;
    }

    // code == 0 significa falla de red (contrato de CheckPracticaRepository).
    private void ReportarError(long code)
    {
        SetStatus(code == 0 ? "Error de red." : $"Error {code}");
    }

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }
}