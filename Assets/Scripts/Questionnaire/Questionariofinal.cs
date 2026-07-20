using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using TMPro;
using System.Text;
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
    public string sceneProfeExito = "EnvioProfe_Cues"; // profe / admin

    [Header("Feedback")]
    public TMP_Text mensajeText;

    [Header("Supabase Config")]
    public SupabaseConfig supabaseConfig;

    // ── Botón Enviar ──────────────────────────────────────────────────────────

    public void OnClickEnviar()
    {
        Debug.Log("QuestionarioFinal: OnClickEnviar() invocado.");
        string respuesta3 = ObtenerRespuesta(tipoPregunta3, inputPregunta3, dropdownPregunta3, botonesPregunta3);
        string respuesta4 = ObtenerRespuesta(tipoPregunta4, inputPregunta4, dropdownPregunta4, botonesPregunta4);
        string respuesta5 = ObtenerRespuesta(tipoPregunta5, inputPregunta5, dropdownPregunta5, botonesPregunta5);

        if (string.IsNullOrEmpty(respuesta3) || string.IsNullOrEmpty(respuesta4) || string.IsNullOrEmpty(respuesta5))
        {
            Debug.Log($"QuestionarioFinal: falta responder. respuesta3='{respuesta3}' respuesta4='{respuesta4}' respuesta5='{respuesta5}'");
            SetMsg("Responde todas las preguntas antes de enviar.");
            return;
        }

        GuardarRespuesta(numeroPregunta3, textoPregunta3, respuesta3);
        GuardarRespuesta(numeroPregunta4, textoPregunta4, respuesta4);
        GuardarRespuesta(numeroPregunta5, textoPregunta5, respuesta5);

        // ── decisión por rol ──────────────────────────────────────────────────
        string rol = PlayerPrefs.GetString("rol", "alumno");

        if (rol == "alumno")
            StartCoroutine(EnviarResultado());   // guarda en Supabase → EnvioExitoso
        else
            IrAProfeExito();                     // solo navega → EnvioProfe_Cues
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

        string respuestasJson = ConstruirRespuestasSatisfaccion();
        Debug.Log("JSON a enviar: " + respuestasJson);

        string bodyStr = "{" +
            $"\"alumno_id\":{alumnoId}," +
            $"\"respuestas_json\":{respuestasJson}" +
        "}";

        byte[] bodyBytes = Encoding.UTF8.GetBytes(bodyStr);
        string url = $"{supabaseConfig.url}/rest/v1/encuestas_satisfaccion";

        var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(bodyBytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.timeout = 10;
        req.SetRequestHeader("apikey", supabaseConfig.anonKey);
        req.SetRequestHeader("Authorization", "Bearer " + accessToken);
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Prefer", "return=minimal");

        yield return req.SendWebRequest();

        if (IsNetworkFailure(req))
        {
            SetMsg("Error de red. Intenta de nuevo.");
            yield break;
        }

        if (req.responseCode < 200 || req.responseCode >= 300)
        {
            SetMsg($"Error {req.responseCode}: {req.downloadHandler.text}");
            Debug.Log($"Error {req.responseCode}: {req.downloadHandler.text}");
            yield break;
        }

        LimpiarRespuestasGuardadas(totalPreguntas: 5);
        SetMsg("");
        SceneManager.LoadScene(sceneExito);
    }

    // ── Flujo profe/admin: solo navegar ───────────────────────────────────────

    private void IrAProfeExito()
    {
        LimpiarRespuestasGuardadas(totalPreguntas: 5);
        SetMsg("");
        SceneManager.LoadScene(sceneProfeExito);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // Arma el respuestas_json de encuestas_satisfaccion con esquema fijo:
    // {"ritmo":"adecuado","navegacion":4,"recomendaria":true,"claridad_tema":4,"instrucciones":5}
    // q_1 = navegación, q_2 = instrucciones, q_3 = ritmo, q_4 = claridad_tema, q_5 = recomendaría.
    private string ConstruirRespuestasSatisfaccion()
    {
        string navegacion = PlayerPrefs.GetString("q_1_respuesta", "").Trim();
        string instrucciones = PlayerPrefs.GetString("q_2_respuesta", "").Trim();
        string ritmo = PlayerPrefs.GetString("q_3_respuesta", "").Trim().ToLowerInvariant();
        string claridadTema = PlayerPrefs.GetString("q_4_respuesta", "").Trim();
        bool recomendaria = PlayerPrefs.GetString("q_5_respuesta", "").Trim() == "Sí";

        return "{" +
            $"\"ritmo\":\"{EscapeJson(ritmo)}\"," +
            $"\"navegacion\":{navegacion}," +
            $"\"recomendaria\":{(recomendaria ? "true" : "false")}," +
            $"\"claridad_tema\":{claridadTema}," +
            $"\"instrucciones\":{instrucciones}" +
        "}";
    }

    private string EscapeJson(string value)
    {
        return value.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t");
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

    private void GuardarRespuesta(int numero, string pregunta, string respuesta)
    {
        PlayerPrefs.SetString($"q_{numero}_pregunta", pregunta);
        PlayerPrefs.SetString($"q_{numero}_respuesta", respuesta);
        PlayerPrefs.Save();
    }

    private void LimpiarRespuestasGuardadas(int totalPreguntas)
    {
        for (int i = 1; i <= totalPreguntas; i++)
        {
            PlayerPrefs.DeleteKey($"q_{i}_pregunta");
            PlayerPrefs.DeleteKey($"q_{i}_respuesta");
        }
        PlayerPrefs.Save();
    }

    private bool IsNetworkFailure(UnityWebRequest req) =>
        req.result == UnityWebRequest.Result.ConnectionError ||
        req.result == UnityWebRequest.Result.DataProcessingError;

    private void SetMsg(string msg)
    {
        if (mensajeText != null) mensajeText.text = msg;
    }
}