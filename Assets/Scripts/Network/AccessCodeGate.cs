using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Debug = UnityEngine.Debug;

public class AccessCodeGate : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField inputCodigo;
    public TMP_Text messageText;

    [Header("Popup - ya realizado")]
    public GameObject popup;

    [Header("Supabase Config")]
    // Arrastra aquí el mismo asset SupabaseConfig que usas en Login
    public SupabaseConfig supabaseConfig;

    [Header("Scenes por práctica")]
    public string scenePractica1 = "CuestionarioPrac1";
    public string scenePractica2 = "CuestionarioPrac2";
    public string scenePractica3 = "CuestionarioPrac3";

    // ── Clases internas ───────────────────────────────────────────────────────

    [System.Serializable] private class PracticaRow { public int id; public string titulo; public string codigo; }
    [System.Serializable] private class ResultadoRow { public long id; }

    private AccessCodeRepository repository;

    void Awake()
    {
        repository = new AccessCodeRepository(supabaseConfig);
    }

    // ── Botón Iniciar ─────────────────────────────────────────────────────────

    public void OnClickIniciarPractica()
    {
        string codigo = inputCodigo.text.Trim();

        if (string.IsNullOrEmpty(codigo))
        {
            SetMsg("Ingresa el código.");
            return;
        }

        StartCoroutine(CheckCodigoAndGo(codigo));
    }

    // ── Flujo principal ───────────────────────────────────────────────────────

    private System.Collections.IEnumerator CheckCodigoAndGo(string codigo)
    {
        SetMsg("Validando código...");

        string accessToken = PlayerPrefs.GetString("sb_access_token", "");
        if (string.IsNullOrEmpty(accessToken))
        {
            SetMsg("Sesión no encontrada. Vuelve a iniciar sesión.");
            yield break;
        }

        long alumnoId = PlayerPrefs.GetInt("alumno_id", 0);
        if (alumnoId == 0)
        {
            SetMsg("No se encontró alumno_id. Inicia sesión de nuevo.");
            yield break;
        }

        bool ok = false;
        long code = 0;
        string body = null;

        // 1) Buscar práctica por código
        yield return StartCoroutine(repository.BuscarPracticaPorCodigo(accessToken, codigo,
            (success, responseCode, responseBody) => { ok = success; code = responseCode; body = responseBody; }));

        if (!ProcesarBusquedaPractica(ok, code, body, out PracticaRow practica)) yield break;

        // 2) Verificar si ya existe resultado para ese alumno y práctica
        yield return StartCoroutine(repository.BuscarResultado(accessToken, alumnoId, practica.id,
            (success, responseCode, responseBody) => { ok = success; code = responseCode; body = responseBody; }));

        if (!ProcesarVerificacionResultado(ok, code, body)) yield break;

        // 3) Ir a la escena correspondiente
        string targetScene = GetSceneByPracticaId(practica.id);
        if (string.IsNullOrEmpty(targetScene))
        {
            SetMsg($"Práctica {practica.id} no tiene escena asignada.");
            yield break;
        }

        PlayerPrefs.SetInt("practica_id", practica.id);
        PlayerPrefs.SetString("practica_titulo", practica.titulo ?? "");
        PlayerPrefs.Save();

        SetMsg("");
        SceneManager.LoadScene(targetScene);
    }

    // Devuelve true si se obtuvo una práctica y el flujo debe continuar al paso 2.
    private bool ProcesarBusquedaPractica(bool ok, long code, string body, out PracticaRow practica)
    {
        practica = null;

        if (!HandleReqBasics(code, "validar código")) return false;

        var arr = JsonHelper.FromJson<PracticaRow>(body);
        if (arr == null || arr.Length == 0)
        {
            SetMsg("Código incorrecto.");
            return false;
        }

        practica = arr[0];
        return true;
    }

    // Devuelve true si NO hay resultado previo y el flujo debe continuar al paso 3.
    private bool ProcesarVerificacionResultado(bool ok, long code, string body)
    {
        if (!HandleReqBasics(code, "verificar resultados")) return false;

        var arr = JsonHelper.FromJson<ResultadoRow>(body);
        bool yaExiste = (arr != null && arr.Length > 0);

        if (yaExiste)
        {
            if (popup != null) popup.SetActive(true);
            inputCodigo.text = "";
            inputCodigo.ActivateInputField();
            return false;
        }

        return true;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // code == 0 significa falla de red (contrato de AccessCodeRepository).
    private bool HandleReqBasics(long code, string accion)
    {
        if (code == 0)
        {
            SetMsg("Error de red. Intenta de nuevo.");
            return false;
        }

        if (code == 401 || code == 403)
        {
            SetMsg($"Sin permisos para {accion} (revisa RLS/policies).");
            return false;
        }

        if (code < 200 || code >= 300)
        {
            SetMsg($"Error al {accion}. Intenta de nuevo.");
            return false;
        }

        return true;
    }

    private string GetSceneByPracticaId(int practicaId)
    {
        if (practicaId == 1) return scenePractica1;
        if (practicaId == 2) return scenePractica2;
        if (practicaId == 3) return scenePractica3;
        return null;
    }

    private void SetMsg(string msg)
    {
        if (messageText != null) messageText.text = msg;
    }

    // ══════════════════════════════════════════════════════════════════
    // TEST TEMPORAL — BORRAR estos 3 métodos y EstadoActual() al terminar
    // de probar (ProcesarBusquedaPractica/ProcesarVerificacionResultado se
    // quedan, son código final). Prueban los seams sueltos: no navegan ni
    // tocan PlayerPrefs.
    // ══════════════════════════════════════════════════════════════════

    private string EstadoActual() => messageText != null ? messageText.text : "(messageText sin asignar)";

    [ContextMenu("TEST: Simular código inexistente")]
    void TestCodigoInexistente()
    {
        bool sigue = ProcesarBusquedaPractica(true, 200, "[]", out PracticaRow practica);
        Debug.Log($"TEST AccessCodeGate código inexistente: continua={sigue} practicaNull={practica == null} msg='{EstadoActual()}'");
    }

    [ContextMenu("TEST: Simular práctica ya realizada")]
    void TestPracticaYaRealizada()
    {
        if (popup != null) popup.SetActive(false);
        inputCodigo.text = "CODIGO-PRUEBA";

        bool sigue = ProcesarVerificacionResultado(true, 200, "[{\"id\":1}]");

        bool popupActivo = popup != null && popup.activeSelf;
        Debug.Log($"TEST AccessCodeGate ya realizada: continua={sigue} popupActivo={popupActivo} input='{inputCodigo.text}'");
    }

    [ContextMenu("TEST: Simular errores en ambos pasos")]
    void TestErroresAmbosPasos()
    {
        (string etiqueta, long code)[] casos = { ("red", 0), ("401", 401), ("403", 403), ("500", 500) };

        foreach (var (etiqueta, code) in casos)
        {
            bool sigue = ProcesarBusquedaPractica(false, code, null, out _);
            Debug.Log($"TEST AccessCodeGate paso 1 [{etiqueta}]: continua={sigue} msg='{EstadoActual()}'");
        }

        foreach (var (etiqueta, code) in casos)
        {
            bool sigue = ProcesarVerificacionResultado(false, code, null);
            Debug.Log($"TEST AccessCodeGate paso 2 [{etiqueta}]: continua={sigue} msg='{EstadoActual()}'");
        }
    }
}
