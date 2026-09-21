using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Debug = UnityEngine.Debug;

public class QuestionarioFinal : MonoBehaviour
{
    [Header("Pregunta 3")]
    public int numeroPregunta3 = 3;
    [TextArea] public string textoPregunta3 = "Pregunta 3...";
    public QuestionarioPage.TipoPregunta tipoPregunta3 = QuestionarioPage.TipoPregunta.TextoLibre;
    public TMP_InputField inputPregunta3;
    public TMP_Dropdown dropdownPregunta3;
    public ButtonChoiceSelector botonesPregunta3;

    [Header("Pregunta 4")]
    public int numeroPregunta4 = 4;
    [TextArea] public string textoPregunta4 = "Pregunta 4...";
    public QuestionarioPage.TipoPregunta tipoPregunta4 = QuestionarioPage.TipoPregunta.TextoLibre;
    public TMP_InputField inputPregunta4;
    public TMP_Dropdown dropdownPregunta4;
    public ButtonChoiceSelector botonesPregunta4;

    [Header("Pregunta 5 (ultima)")]
    public int numeroPregunta5 = 5;
    [TextArea] public string textoPregunta5 = "Pregunta 5...";
    public QuestionarioPage.TipoPregunta tipoPregunta5 = QuestionarioPage.TipoPregunta.TextoLibre;
    public TMP_InputField inputPregunta5;
    public TMP_Dropdown dropdownPregunta5;
    public ButtonChoiceSelector botonesPregunta5;

    [Header("Navegacion")]
    public string sceneExito = "EnvioExitoso";    // alumno
    public string sceneModoRevision = "EnvioExitoso_Demo"; // profesor/admin revisando: no guarda ni envía nada

    [Header("Modo de envío")]
    [Tooltip("false = encuesta de satisfacción (encuestas_satisfaccion). true = cuestionario de práctica (resultados), usado en CuestionarioPracX-3.")]
    public bool esResultadoPractica = false;

    [Header("Feedback")]
    public TMP_Text mensajeText;

    [Header("Supabase Config")]
    public SupabaseConfig supabaseConfig;

    private QuestionarioFinalRepository repository;
    private QuestionarioFinalLogica logica;

    void Awake()
    {
        repository = new QuestionarioFinalRepository(supabaseConfig);
        logica = new QuestionarioFinalLogica();
    }

    // ── Botón Enviar ──────────────────────────────────────────────────────────

    public void OnClickEnviar()
    {
        Debug.Log("QuestionarioFinal: OnClickEnviar() invocado.");

        // Una pregunta sin ningún campo conectado (Input/Dropdown/Botones) no se
        // usa en esta escena — su respuesta ya se guardó en una escena anterior
        // (ej. CuestionarioPracX-2). No debe pedirse ni sobrescribirse aquí.
        bool p3Conectada = PreguntaConectada(inputPregunta3, dropdownPregunta3, botonesPregunta3);
        bool p4Conectada = PreguntaConectada(inputPregunta4, dropdownPregunta4, botonesPregunta4);
        bool p5Conectada = PreguntaConectada(inputPregunta5, dropdownPregunta5, botonesPregunta5);

        string respuesta3 = p3Conectada ? ObtenerRespuesta(tipoPregunta3, inputPregunta3, dropdownPregunta3, botonesPregunta3) : null;
        string respuesta4 = p4Conectada ? ObtenerRespuesta(tipoPregunta4, inputPregunta4, dropdownPregunta4, botonesPregunta4) : null;
        string respuesta5 = p5Conectada ? ObtenerRespuesta(tipoPregunta5, inputPregunta5, dropdownPregunta5, botonesPregunta5) : null;

        if (!logica.ValidarRespuestasCompletas(
                p3Conectada, respuesta3,
                p4Conectada, respuesta4,
                p5Conectada, respuesta5))
        {
            Debug.Log($"QuestionarioFinal: falta responder. respuesta3='{respuesta3}' respuesta4='{respuesta4}' respuesta5='{respuesta5}'");
            SetMsg("Responde todas las preguntas antes de enviar.");
            return;
        }

        // ── modo revisión: profesor/admin viendo el cuestionario, no un alumno
        // enviando resultados reales — no se guarda ni se envía nada, solo navega.
        string rol = PlayerPrefs.GetString("rol", "alumno");
        if (rol != "alumno")
        {
            IrAProfeExito();
            return;
        }

        if (p3Conectada) logica.GuardarRespuesta(numeroPregunta3, textoPregunta3, respuesta3);
        if (p4Conectada) logica.GuardarRespuesta(numeroPregunta4, textoPregunta4, respuesta4);
        if (p5Conectada) logica.GuardarRespuesta(numeroPregunta5, textoPregunta5, respuesta5);

        StartCoroutine(esResultadoPractica ? EnviarResultadoPractica() : EnviarResultado());
    }

    // ── Flujo alumno: enviar a Supabase ───────────────────────────────────────

    private System.Collections.IEnumerator EnviarResultado()
    {
        SetMsg("Enviando respuestas...");

        string accessToken = PlayerPrefs.GetString("sb_access_token", "");
        int alumnoId = PlayerPrefs.GetInt("alumno_id", 0);

        if (string.IsNullOrEmpty(accessToken) || alumnoId == 0)
        {
            Debug.Log($"QuestionarioFinal: sesion invalida. accessToken vacio={string.IsNullOrEmpty(accessToken)} alumnoId={alumnoId}");
            SetMsg("Error de sesion. Vuelve a iniciar sesion.");
            yield break;
        }

        string respuestasJson = logica.ConstruirRespuestasSatisfaccion();
        Debug.Log("JSON a enviar: " + respuestasJson);

        bool ok = false;
        string errorMsg = null;
        yield return StartCoroutine(repository.EnviarSatisfaccion(
            accessToken, alumnoId, respuestasJson,
            (success, codigoHttp, cuerpo) => { ok = success; errorMsg = success ? null : MensajesHttp.ErrorEnvio(codigoHttp, cuerpo); }));

        if (!ok)
        {
            SetMsg(errorMsg);
            Debug.Log(errorMsg);
            yield break;
        }

        logica.LimpiarRespuestasGuardadas(totalPreguntas: 5);
        SetMsg("");
        SceneManager.LoadScene(sceneExito);
    }

    // ── Flujo alumno: cuestionario de práctica (resultados) ───────────────────
    // Manda las 5 preguntas/respuestas guardadas en PlayerPrefs (q_1..q_5) a la
    // tabla resultados, igual que P1_Instrucciones lo hace directo desde VR —
    // salvo que aquí no hay forma de calificar automáticamente respuestas de
    // texto/opción libre, así que calificacion se manda null (el profesor
    // califica manualmente después).
    private System.Collections.IEnumerator EnviarResultadoPractica()
    {
        SetMsg("Enviando respuestas...");

        string accessToken = PlayerPrefs.GetString("sb_access_token", "");
        int alumnoId = PlayerPrefs.GetInt("alumno_id", 0);
        int practicaId = PlayerPrefs.GetInt("practica_id", 0);

        if (string.IsNullOrEmpty(accessToken) || alumnoId == 0 || practicaId == 0)
        {
            Debug.Log($"QuestionarioFinal: sesion invalida. accessToken vacio={string.IsNullOrEmpty(accessToken)} alumnoId={alumnoId} practicaId={practicaId}");
            SetMsg("Error de sesion. Vuelve a iniciar sesion.");
            yield break;
        }

        string respuestasJson = logica.ConstruirRespuestasPractica(totalPreguntas: 5);
        Debug.Log("JSON a enviar: " + respuestasJson);

        bool ok = false;
        string errorMsg = null;
        yield return StartCoroutine(repository.EnviarResultadoPractica(
            accessToken, alumnoId, practicaId, respuestasJson,
            (success, codigoHttp, cuerpo) => { ok = success; errorMsg = success ? null : MensajesHttp.ErrorEnvio(codigoHttp, cuerpo); }));

        if (!ok)
        {
            SetMsg(errorMsg);
            Debug.Log(errorMsg);
            yield break;
        }

        logica.LimpiarRespuestasGuardadas(totalPreguntas: 5);
        SetMsg("");
        SceneManager.LoadScene(sceneExito);
    }

    // ── Flujo modo revisión: solo navegar, sin guardar ni enviar ──────────────

    private void IrAProfeExito()
    {
        logica.LimpiarRespuestasGuardadas(totalPreguntas: 5);
        SetMsg("");
        SceneManager.LoadScene(sceneModoRevision);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool PreguntaConectada(TMP_InputField input, TMP_Dropdown dropdown, ButtonChoiceSelector botones)
    {
        return input != null || dropdown != null || botones != null;
    }

    private string ObtenerRespuesta(QuestionarioPage.TipoPregunta tipo,
                                    TMP_InputField input, TMP_Dropdown dropdown,
                                    ButtonChoiceSelector botones)
    {
        if (tipo == QuestionarioPage.TipoPregunta.TextoLibre)
            return input != null ? input.text.Trim() : "";
        else if (tipo == QuestionarioPage.TipoPregunta.OpcionMultiple)
            return dropdown != null ? dropdown.options[dropdown.value].text : "";
        else
            return botones != null ? botones.TextoSeleccionado : "";
    }

    private void SetMsg(string msg)
    {
        if (mensajeText != null) mensajeText.text = msg;
    }
}