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

    // ── Clases mapeadas a tu BD ───────────────────────────────────────────────

    [System.Serializable] private class GrupoRow { public int grupo_id; public string codigo_acceso; }

    private JoinGroupRepository repository;

    void Awake()
    {
        repository = new JoinGroupRepository(supabaseConfig);
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

        if (string.IsNullOrEmpty(accessToken) || alumnoId == 0)
        {
            SetStatus("Sesión inválida. Vuelve a iniciar sesión.");
            yield break;
        }

        bool ok = false;
        long code = 0;
        string body = null;

        // 1) Buscar grupo por codigo_acceso
        yield return StartCoroutine(repository.BuscarGrupoPorCodigo(accessToken, codigo,
            (success, responseCode, responseBody) => { ok = success; code = responseCode; body = responseBody; }));

        if (!ProcesarBusqueda(ok, code, body, out int grupoId)) yield break;

        // 2) Actualizar alumnos SET grupo_id = grupoId WHERE alumno_id = alumnoId
        yield return StartCoroutine(repository.AsignarAlumnoAGrupo(accessToken, alumnoId, grupoId,
            (success, responseCode, responseBody) => { ok = success; code = responseCode; body = responseBody; }));

        if (!ProcesarAsignacion(ok, code)) yield break;

        // 3) Guardar grupo_id en sesión y navegar
        PlayerPrefs.SetInt("grupo_id", grupoId);
        PlayerPrefs.SetInt("tiene_grupo", 1);
        PlayerPrefs.Save();

        SetStatus("");
        SceneManager.LoadScene(sceneDestino);
    }

    // Devuelve true si se obtuvo un grupo y el flujo debe continuar al paso 2.
    private bool ProcesarBusqueda(bool ok, long code, string body, out int grupoId)
    {
        grupoId = 0;

        if (!ok && code == 0) { SetStatus("Error de red. Intenta de nuevo."); return false; }
        if (!HandleReqBasics(code,
                "Sin permisos para validar el código (revisa RLS/policies).",
                "Error al validar el código. Intenta de nuevo."))
            return false;

        var grupos = JsonHelper.FromJson<GrupoRow>(body);
        if (grupos == null || grupos.Length == 0)
        {
            // Código incorrecto → mostrar modal
            ShowPopup();
            SetStatus("");
            return false;
        }

        grupoId = grupos[0].grupo_id;
        return true;
    }

    // Devuelve true si el PATCH fue exitoso y el flujo debe continuar al paso 3.
    private bool ProcesarAsignacion(bool ok, long code)
    {
        if (!ok && code == 0) { SetStatus("Error de red al unirse al grupo."); return false; }
        return HandleReqBasics(code,
            "Sin permisos para unirte al grupo (revisa RLS/policies).",
            "Error al unirse al grupo. Intenta de nuevo.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool HandleReqBasics(long code, string mensajeSinPermisos, string mensajeError)
    {
        if (code == 401 || code == 403)
        {
            SetStatus(mensajeSinPermisos);
            return false;
        }

        if (code < 200 || code >= 300)
        {
            SetStatus(mensajeError);
            return false;
        }

        return true;
    }

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