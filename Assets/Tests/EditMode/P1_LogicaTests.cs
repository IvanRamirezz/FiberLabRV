using System.Linq;
using NUnit.Framework;

public class P1_LogicaTests
{
    P1_Logica logica;

    [SetUp]
    public void SetUp() => logica = new P1_Logica();

    // ── Búfer / posición de la fibra global ──────────────────────────────

    [TestCase(1, 1)]
    [TestCase(12, 1)]
    [TestCase(13, 2)]
    [TestCase(24, 2)]
    [TestCase(25, 3)]
    [TestCase(60, 5)]
    public void CalcularBuffer_AgrupaDeDoceEnDoce(int numeroGlobal, int esperado) =>
        Assert.AreEqual(esperado, logica.CalcularBuffer(numeroGlobal));

    [TestCase(1, 1)]
    [TestCase(12, 12)]
    [TestCase(13, 1)]
    [TestCase(24, 12)]
    [TestCase(60, 12)]
    public void CalcularPosicion_VaDe1a12DentroDelBufer(int numeroGlobal, int esperado) =>
        Assert.AreEqual(esperado, logica.CalcularPosicion(numeroGlobal));

    [Test]
    public void BufferYPosicion_ReconstruyenElNumeroGlobalDel1Al60()
    {
        for (int n = 1; n <= 60; n++)
        {
            int buffer = logica.CalcularBuffer(n);
            int posicion = logica.CalcularPosicion(n);

            Assert.That(buffer, Is.InRange(1, 5), $"buffer de {n}");
            Assert.That(posicion, Is.InRange(1, 12), $"posición de {n}");
            Assert.AreEqual(n, (buffer - 1) * 12 + posicion, $"reconstrucción de {n}");
        }
    }

    // ── Calificación y respuestas ────────────────────────────────────────

    [TestCase(4, 4, 6, 10f)]
    [TestCase(0, 0, 0, 0f)]
    [TestCase(1, 2, 2, 5f / 14f * 10f)]
    [TestCase(3, 4, 5, 12f / 14f * 10f)]
    public void CalcularCalificacionFinal_EsProporcionalAlTotalSobre14(int s2, int s5, int s6, float esperado) =>
        Assert.AreEqual(esperado, logica.CalcularCalificacionFinal(s2, s5, s6), 0.0001f);

    [Test]
    public void EvaluarRespuesta_ComparaExactoYDistingueMayusculas()
    {
        Assert.IsTrue(logica.EvaluarRespuesta("Azul", "Azul"));
        Assert.IsFalse(logica.EvaluarRespuesta("Azul", "Naranja"));
        Assert.IsFalse(logica.EvaluarRespuesta("azul", "Azul"));
    }

    // ── respuestas_json de P1 ────────────────────────────────────────────

    [Test]
    public void ConstruirRespuestasJson_ResumenPorPaso()
    {
        Assert.AreEqual(
            "{\"identificacion_partes\":\"3/4\",\"identificacion_fibras\":\"4/4\"," +
            "\"calculo_posicion_global\":\"5/6\",\"puntuacion_total\":\"12/14\"}",
            logica.ConstruirRespuestasJson(3, 4, 5));
    }

    [Test]
    public void ConstruirRespuestasJson_PuntajeMaximoYMinimo()
    {
        StringAssert.Contains("\"puntuacion_total\":\"14/14\"", logica.ConstruirRespuestasJson(4, 4, 6));
        StringAssert.Contains("\"puntuacion_total\":\"0/14\"", logica.ConstruirRespuestasJson(0, 0, 0));
    }

    // ── Generadores (aleatorios: se prueban las invariantes, repetidas) ──

    const int Repeticiones = 50;

    [Test]
    public void GenerarNumerosGlobales_UnicosYEnRango()
    {
        for (int i = 0; i < Repeticiones; i++)
        {
            int[] numeros = logica.GenerarNumerosGlobales(6);

            Assert.AreEqual(6, numeros.Length);
            Assert.AreEqual(6, numeros.Distinct().Count());
            Assert.That(numeros, Has.All.InRange(1, 60));
        }
    }

    [Test]
    public void GenerarNumerosGlobales_ConTodoElRangoDevuelveLos60()
    {
        int[] numeros = logica.GenerarNumerosGlobales(60);

        CollectionAssert.AreEquivalent(Enumerable.Range(1, 60), numeros);
    }

    [Test]
    public void GenerarNumerosGlobales_CeroDevuelveVacio() =>
        Assert.IsEmpty(logica.GenerarNumerosGlobales(0));

    [Test]
    public void GenerarOpcionesBufer_CuatroDistintasIncluyendoLaCorrecta()
    {
        for (int correcto = 1; correcto <= 5; correcto++)
        {
            for (int i = 0; i < Repeticiones; i++)
            {
                string[] opciones = logica.GenerarOpcionesBufer(correcto);

                Assert.AreEqual(4, opciones.Length);
                Assert.AreEqual(4, opciones.Distinct().Count());
                Assert.Contains($"Cinta Milar {correcto}", opciones);
                Assert.That(opciones, Has.All.Matches<string>(o => System.Text.RegularExpressions.Regex.IsMatch(o, "^Cinta Milar [1-5]$")));
            }
        }
    }

    [Test]
    public void GenerarPreguntasQuiz_UnicasYEnRango()
    {
        for (int i = 0; i < Repeticiones; i++)
        {
            var preguntas = logica.GenerarPreguntasQuiz(10);

            Assert.AreEqual(10, preguntas.Length);
            Assert.AreEqual(10, preguntas.Distinct().Count());
            foreach (var (buffer, fibra) in preguntas)
            {
                Assert.That(buffer, Is.InRange(0, 4));
                Assert.That(fibra, Is.InRange(1, 12));
            }
        }
    }

    [Test]
    public void GenerarOpcionesColor_CuatroDistintasIncluyendoLaCorrecta()
    {
        foreach (string correcto in P1_Logica.COLOR_NAMES)
        {
            string[] opciones = logica.GenerarOpcionesColor(correcto);

            Assert.AreEqual(4, opciones.Length);
            Assert.AreEqual(4, opciones.Distinct().Count());
            Assert.Contains(correcto, opciones);
            Assert.That(opciones, Has.All.Matches<string>(o => P1_Logica.COLOR_NAMES.Contains(o)));
        }
    }

    [Test]
    public void ColorNames_SonDoceDistintos()
    {
        Assert.AreEqual(12, P1_Logica.COLOR_NAMES.Length);
        Assert.AreEqual(12, P1_Logica.COLOR_NAMES.Distinct().Count());
    }
}
