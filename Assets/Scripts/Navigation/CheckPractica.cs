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

    private CheckPracticaRepository repository;
    private CheckPracticaLogica logica;

    void Awake()
    {
        repository = new CheckPracticaRepository(supabaseConfig);
        logica = new CheckPracticaLogica();
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

        if (!logica.SesionValida(accessToken, alumnoId))
        {
            SetStatus(CheckPracticaLogica.MensajeSesionInvalida);
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

            var grupo = logica.InterpretarGrupo(ok, code, body);
            if (!Continuar(grupo)) yield break;

            grupoId = grupo.grupoId;
            PlayerPrefs.SetInt("grupo_id", grupoId);
            PlayerPrefs.Save();
        }

        // 1) Buscar práctica activa para este grupo
        yield return StartCoroutine(repository.BuscarPracticaActiva(accessToken, grupoId, System.DateTime.UtcNow,
            (success, responseCode, responseBody) => { ok = success; code = responseCode; body = responseBody; }));

        var activa = logica.InterpretarPracticaActiva(ok, code, body);
        if (!Continuar(activa)) yield break;
        int practicaId = activa.practicaId;

        // 2) Verificar si el alumno ya tiene resultado para esta práctica
        yield return StartCoroutine(repository.BuscarResultado(accessToken, alumnoId, practicaId,
            (success, responseCode, responseBody) => { ok = success; code = responseCode; body = responseBody; }));

        var previo = logica.InterpretarResultado(ok, code, body);
        if (previo.accion == CheckPracticaLogica.Accion.IrASinPractica)
            Debug.Log($">>> Alumno ya realizó la práctica {practicaId} → {sceneSinPractica}");
        if (!Continuar(previo)) yield break;

        // 3) Todo ok → guardar practica_id y navegar
        PlayerPrefs.SetInt("practica_id", practicaId);
        PlayerPrefs.Save();

        Debug.Log($">>> Práctica activa: {practicaId} → {sceneConPractica}");

        SetStatus("");
        SceneManager.LoadScene(sceneConPractica);
    }

    // Traduce el resultado de la lógica a UI / escena; devuelve true si el flujo debe continuar.
    private bool Continuar(CheckPracticaLogica.Resultado r)
    {
        switch (r.accion)
        {
            case CheckPracticaLogica.Accion.MostrarError:
                SetStatus(r.mensaje);
                return false;
            case CheckPracticaLogica.Accion.IrASinPractica:
                SetStatus("");
                SceneManager.LoadScene(sceneSinPractica);
                return false;
            default:
                return true;
        }
    }

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }
}