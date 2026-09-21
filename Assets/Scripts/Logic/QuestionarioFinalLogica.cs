using System.Text;
using UnityEngine;

// Lógica de dominio de QuestionarioFinal: valida respuestas, arma el
// respuestas_json para Supabase y persiste/limpia PlayerPrefs. No sabe de UI
// (eso es de QuestionarioFinal) ni de HTTP (eso es de QuestionarioFinalRepository).
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
        return ConstruirRespuestasSatisfaccion(
            PlayerPrefs.GetString("q_1_respuesta", ""),
            PlayerPrefs.GetString("q_2_respuesta", ""),
            PlayerPrefs.GetString("q_3_respuesta", ""),
            PlayerPrefs.GetString("q_4_respuesta", ""),
            PlayerPrefs.GetString("q_5_respuesta", ""));
    }

    // Misma construcción a partir de las respuestas crudas (sin PlayerPrefs).
    public string ConstruirRespuestasSatisfaccion(
        string respuestaQ1, string respuestaQ2, string respuestaQ3,
        string respuestaQ4, string respuestaQ5)
    {
        string navegacion = respuestaQ1.Trim();
        string instrucciones = respuestaQ2.Trim();
        string ritmo = respuestaQ3.Trim().ToLowerInvariant();
        string claridadTema = respuestaQ4.Trim();
        bool recomendaria = respuestaQ5.Trim() == "Sí";

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
        var pares = new (string pregunta, string respuesta)[totalPreguntas];
        for (int i = 1; i <= totalPreguntas; i++)
        {
            pares[i - 1] = (PlayerPrefs.GetString($"q_{i}_pregunta", ""),
                            PlayerPrefs.GetString($"q_{i}_respuesta", ""));
        }
        return ConstruirRespuestasPractica(pares);
    }

    // Misma construcción a partir de los pares (pregunta, respuesta) ya leídos;
    // la posición 0 del arreglo es la pregunta 1.
    public string ConstruirRespuestasPractica((string pregunta, string respuesta)[] pares)
    {
        var sb = new StringBuilder("{");
        for (int i = 1; i <= pares.Length; i++)
        {
            string pregunta = pares[i - 1].pregunta.Trim();
            string respuesta = pares[i - 1].respuesta.Trim();

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
