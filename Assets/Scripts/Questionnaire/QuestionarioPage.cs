using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Debug = UnityEngine.Debug;

/// <summary>
/// Ponlo en las scenes 1 y 2 del cuestionario.
/// Guarda las respuestas en PlayerPrefs y navega a la siguiente scene.
/// Configura las preguntas y el tipo de cada una desde el Inspector.
/// </summary>
public class QuestionarioPage : MonoBehaviour
{
    [Header("Pregunta 1")]
    public int numeroPregunta1 = 1;
    [TextArea] public string textoPregunta1 = "Pregunta 1...";
    public TipoPregunta tipoPregunta1 = TipoPregunta.TextoLibre;

    [Header("Respuesta Pregunta 1")]
    public TMP_InputField inputPregunta1;       // para texto libre
    public TMP_Dropdown dropdownPregunta1;    // para opción múltiple
    public ButtonChoiceSelector botonesPregunta1; // para botones de opción

    [Header("Pregunta 2")]
    public int numeroPregunta2 = 2;
    [TextArea] public string textoPregunta2 = "Pregunta 2...";
    public TipoPregunta tipoPregunta2 = TipoPregunta.TextoLibre;

    [Header("Respuesta Pregunta 2")]
    public TMP_InputField inputPregunta2;
    public TMP_Dropdown dropdownPregunta2;
    public ButtonChoiceSelector botonesPregunta2;

    [Header("Navegación")]
    public string siguienteScene = "";

    [Header("Feedback")]
    public TMP_Text mensajeText;

    public enum TipoPregunta { TextoLibre, OpcionMultiple, BotonesOpcion }

    // ── Botón Continuar ───────────────────────────────────────────────────────

    public void OnClickContinuar()
    {
        string respuesta1 = ObtenerRespuesta(tipoPregunta1, inputPregunta1, dropdownPregunta1, botonesPregunta1);
        string respuesta2 = ObtenerRespuesta(tipoPregunta2, inputPregunta2, dropdownPregunta2, botonesPregunta2);

        if (string.IsNullOrEmpty(respuesta1) || string.IsNullOrEmpty(respuesta2))
        {
            SetMsg("Responde ambas preguntas antes de continuar.");
            return;
        }

        // Guardar en PlayerPrefs
        GuardarRespuesta(numeroPregunta1, textoPregunta1, respuesta1);
        GuardarRespuesta(numeroPregunta2, textoPregunta2, respuesta2);

        SetMsg("");

        if (!string.IsNullOrEmpty(siguienteScene))
            SceneManager.LoadScene(siguienteScene);
        else
            Debug.LogWarning("QuestionarioPage: no se configuró siguienteScene.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string ObtenerRespuesta(TipoPregunta tipo, TMP_InputField input, TMP_Dropdown dropdown, ButtonChoiceSelector botones)
    {
        if (tipo == TipoPregunta.TextoLibre)
            return input != null ? input.text.Trim() : "";
        else if (tipo == TipoPregunta.OpcionMultiple)
            return dropdown != null ? dropdown.options[dropdown.value].text : "";
        else
            return botones != null ? botones.TextoSeleccionado : "";
    }

    private void GuardarRespuesta(int numero, string pregunta, string respuesta)
    {
        PlayerPrefs.SetString($"q_{numero}_pregunta", pregunta);
        PlayerPrefs.SetString($"q_{numero}_respuesta", respuesta);
        PlayerPrefs.Save();
        Debug.Log($"Guardada pregunta {numero}: {respuesta}");
    }

    private void SetMsg(string msg)
    {
        if (mensajeText != null) mensajeText.text = msg;
    }
}