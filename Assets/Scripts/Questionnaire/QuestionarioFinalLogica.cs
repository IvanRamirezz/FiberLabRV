using System.Text;
using UnityEngine;

// Lógica de dominio de QuestionarioFinal: valida respuestas, arma el
// respuestas_json para Supabase y persiste/limpia PlayerPrefs. No sabe de UI
// (eso es de Questionariofinal) ni de HTTP (eso es de QuestionarioFinalRepository).
public class QuestionarioFinalLogica
{
    public bool ValidarRespuestasCompletas(
        bool p3Conectada, string respuesta3,
        bool p4Conectada, string respuesta4,
        bool p5Conectada, string respuesta5)
    {
        return !((p3Conectada && string.IsNullOrEmpty(respuesta3)) ||
                  (p4Conectada && string.IsNullOrEmpty(respuesta4)) ||
                  (p5Conectada && string.IsNullOrEmpty(respuesta5)));
    }

    // Arma el respuestas_json de encuestas_satisfaccion con esquema fijo:
    // {"ritmo":"adecuado","navegacion":4,"recomendaria":true,"claridad_tema":4,"instrucciones":5}
    // q_1 = navegación, q_2 = instrucciones, q_3 = ritmo, q_4 = claridad_tema, q_5 = recomendaría.
    public string ConstruirRespuestasSatisfaccion()
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

    // Arma respuestas_json para el cuestionario de práctica: un objeto por cada
    // pregunta guardada en PlayerPrefs (q_1..q_N), con el texto de la pregunta
    // y la respuesta dada. Genérico porque las preguntas de texto/opción libre
    // no tienen un esquema fijo como el de la encuesta de satisfacción.
    public string ConstruirRespuestasPractica(int totalPreguntas)
    {
        var sb = new StringBuilder("{");
        for (int i = 1; i <= totalPreguntas; i++)
        {
            string pregunta = PlayerPrefs.GetString($"q_{i}_pregunta", "").Trim();
            string respuesta = PlayerPrefs.GetString($"q_{i}_respuesta", "").Trim();

            if (i > 1) sb.Append(",");
            sb.Append($"\"{i}\":{{\"pregunta\":\"{EscapeJson(pregunta)}\",\"respuesta\":\"{EscapeJson(respuesta)}\"}}");
        }
        sb.Append("}");
        return sb.ToString();
    }

    public void GuardarRespuesta(int numero, string pregunta, string respuesta)
    {
        PlayerPrefs.SetString($"q_{numero}_pregunta", pregunta);
        PlayerPrefs.SetString($"q_{numero}_respuesta", respuesta);
        PlayerPrefs.Save();
    }

    public void LimpiarRespuestasGuardadas(int totalPreguntas)
    {
        for (int i = 1; i <= totalPreguntas; i++)
        {
            PlayerPrefs.DeleteKey($"q_{i}_pregunta");
            PlayerPrefs.DeleteKey($"q_{i}_respuesta");
        }
        PlayerPrefs.Save();
    }

    private string EscapeJson(string value)
    {
        return value.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t");
    }
}
