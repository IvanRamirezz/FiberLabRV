using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Lógica de dominio de P1_InstructionManager: generación de preguntas/opciones
// del quiz, la fórmula de calificación y el contenido de respuestas_json. No
// sabe de UI ni de Supabase (eso es de P1_InstructionManager/P1_DatosRepository).
//
// Nota: las 4 funciones Generar* dependen de UnityEngine.Random (estado
// global del motor) para barajar resultados — no son puras en sentido
// estricto, pero siguen siendo lógica de generación de contenido del
// dominio, no de evaluación ni de presentación.
public class P1_Logica
{
    public static readonly string[] COLOR_NAMES = {
        "Azul","Naranja","Verde","Café","Gris","Blanco",
        "Rojo","Negro","Amarillo","Violeta","Rosa","Aguamarina"
    };

    // Genera `count` números globales únicos aleatorios del rango 1-60
    public int[] GenerarNumerosGlobales(int count)
    {
        return Enumerable.Range(1, 60)
            .OrderBy(_ => Random.Range(0f, 1f))
            .Take(count)
            .ToArray();
    }

    // Genera 4 opciones de búfer (1-5) incluyendo el correcto + 3 distractores
    public string[] GenerarOpcionesBufer(int correctBuf)
    {
        var ops = new List<string> { $"Cinta Milar {correctBuf}" };
        var distractores = Enumerable.Range(1, 5)
            .Where(b => b != correctBuf)
            .OrderBy(_ => Random.Range(0f, 1f))
            .Take(3)
            .Select(b => $"Cinta Milar {b}");
        ops.AddRange(distractores);
        return ops.OrderBy(_ => Random.Range(0f, 1f)).ToArray();
    }

    // Genera `count` preguntas únicas (bufferIdx 0-4, fibraPos 1-12)
    public (int buffer, int fibra)[] GenerarPreguntasQuiz(int count)
    {
        var pool = new List<(int, int)>();
        for (int b = 0; b < 5; b++)
            for (int f = 1; f <= 12; f++)
                pool.Add((b, f));
        return pool.OrderBy(_ => Random.Range(0f, 1f)).Take(count).ToArray();
    }

    // Devuelve array de 4 colores mezclados: 1 correcto + 3 distractores
    public string[] GenerarOpcionesColor(string colorCorrecto)
    {
        var distractores = COLOR_NAMES
            .Where(c => c != colorCorrecto)
            .OrderBy(_ => Random.Range(0f, 1f))
            .Take(3)
            .ToList();

        var opciones = new List<string> { colorCorrecto };
        opciones.AddRange(distractores);
        return opciones.OrderBy(_ => Random.Range(0f, 1f)).ToArray();
    }

    // Búfer (Cinta Milar) al que pertenece la fibra global N: 1-based (1-5)
    public int CalcularBuffer(int numeroGlobal) => Mathf.CeilToInt(numeroGlobal / 12f);

    // Posición dentro del búfer de la fibra global N: 1-based (1-12)
    public int CalcularPosicion(int numeroGlobal) => (numeroGlobal - 1) % 12 + 1;

    // Calificación final de la práctica sobre 10, a partir de los 3 scores por paso
    public float CalcularCalificacionFinal(int scoreStep2, int scoreStep5, int scoreStep6) =>
        (scoreStep2 + scoreStep5 + scoreStep6) / 14f * 10f;

    // Contenido de respuestas_json de P1: resumen por paso (no pares pregunta/respuesta).
    // El sobre del POST (alumno_id, practica_id, calificacion) lo arma P1_DatosRepository.
    public string ConstruirRespuestasJson(int scoreStep2, int scoreStep5, int scoreStep6)
    {
        return "{" +
            $"\"identificacion_partes\":\"{scoreStep2}/4\"," +
            $"\"identificacion_fibras\":\"{scoreStep5}/4\"," +
            $"\"calculo_posicion_global\":\"{scoreStep6}/6\"," +
            $"\"puntuacion_total\":\"{scoreStep2 + scoreStep5 + scoreStep6}/14\"" +
        "}";
    }

    // Evalúa si la respuesta del alumno coincide con la esperada. Usado en
    // Paso 5 (color de fibra) y Paso 6 (búfer y color global) — no en Paso 2,
    // que compara un enum dentro del handler de interacción por raycast.
    public bool EvaluarRespuesta(string respuesta, string esperado) => respuesta == esperado;
}
