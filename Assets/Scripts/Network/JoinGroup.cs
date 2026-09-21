using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Debug = UnityEngine.Debug;

public class JoinGroup : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField inputCodigo;
    public TMP_Text statusText;

    [Header("Popup - código incorrecto")]
    public GameObject popupCodigoInvalido;

    [Header("Scene")]
    public string sceneDestino = "Bienvenida";

    [Header("Supabase Config")]
    public SupabaseConfig supabaseConfig;

    private JoinGroupRepository repository;
    private JoinGroupLogica logica;

    void Awake()
    {
        repository = new JoinGroupRepository(supabaseConfig);
        logica = new JoinGroupLogica();
    }

    // ── Botón Unirse al grupo ─────────────────────────────────────────────────

    public void OnClickUnirse()
    {
        string codigo = inputCodigo.text.Trim();

        if (string.IsNullOrEmpty(codigo))
        {
            SetStatus("Ingresa el código de acceso.");
            return;
        }

        StartCoroutine(BuscarYUnirse(codigo));
    }

    // ── Flujo principal ───────────────────────────────────────────────────────

    private System.Collections.IEnumerator BuscarYUnirse(string codigo)
    {
        SetStatus("Validando código...");
        HidePopup();

        string accessToken = PlayerPrefs.GetString("sb_access_token", "");
        int alumnoId = PlayerPrefs.GetInt("alumno_id", 0);

        if (!logica.SesionValida(accessToken, alumnoId))
        {
            SetStatus(JoinGroupLogica.MensajeSesionInvalida);
            yield break;
        }

        bool ok = false;
        long code = 0;
        string body = null;

        // 1) Buscar grupo por codigo_acceso
        yield return StartCoroutine(repository.BuscarGrupoPorCodigo(accessToken, codigo,
            (success, responseCode, responseBody) => { ok = success; code = responseCode; body = responseBody; }));

        var busqueda = logica.InterpretarBusqueda(ok, code, body);
        if (!Continuar(busqueda)) yield break;
        int grupoId = busqueda.grupoId;

        // 2) Actualizar alumnos SET grupo_id = grupoId WHERE alumno_id = alumnoId
        yield return StartCoroutine(repository.AsignarAlumnoAGrupo(accessToken, alumnoId, grupoId,
            (success, responseCode, responseBody) => { ok = success; code = responseCode; body = responseBody; }));

        if (!Continuar(logica.InterpretarAsignacion(ok, code))) yield break;

        // 3) Guardar grupo_id en sesión y navegar
        PlayerPrefs.SetInt("grupo_id", grupoId);
        PlayerPrefs.SetInt("tiene_grupo", 1);
        PlayerPrefs.Save();

        SetStatus("");
        SceneManager.LoadScene(sceneDestino);
    }

    // Traduce el resultado de la lógica a UI; devuelve true si el flujo debe continuar.
    private bool Continuar(JoinGroupLogica.Resultado r)
    {
        switch (r.accion)
        {
            case JoinGroupLogica.Accion.MostrarMensaje:
                SetStatus(r.mensaje);
                return false;
            case JoinGroupLogica.Accion.CodigoInvalido:
                // Código incorrecto → mostrar modal
                ShowPopup();
                SetStatus("");
                return false;
            default:
                return true;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }

    private void ShowPopup()
    {
        if (popupCodigoInvalido != null) popupCodigoInvalido.SetActive(true);
    }

    private void HidePopup()
    {
        if (popupCodigoInvalido != null) popupCodigoInvalido.SetActive(false);
    }
}