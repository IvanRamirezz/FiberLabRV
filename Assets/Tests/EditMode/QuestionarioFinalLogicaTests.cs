using NUnit.Framework;
using UnityEngine;

public class QuestionarioFinalLogicaTests
{
    QuestionarioFinalLogica logica;

    [SetUp]
    public void SetUp() => logica = new QuestionarioFinalLogica();

    // ── Validación ───────────────────────────────────────────────────────

    [Test]
    public void Validar_TodasConectadasYRespondidas_EsValido() =>
        Assert.IsTrue(logica.ValidarRespuestasCompletas(true, "a", true, "b", true, "c"));

    [TestCase(null)]
    [TestCase("")]
    public void Validar_PreguntaConectadaSinRespuesta_NoEsValido(string vacia)
    {
        Assert.IsFalse(logica.ValidarRespuestasCompletas(true, vacia, true, "b", true, "c"));
        Assert.IsFalse(logica.ValidarRespuestasCompletas(true, "a", true, vacia, true, "c"));
        Assert.IsFalse(logica.ValidarRespuestasCompletas(true, "a", true, "b", true, vacia));
    }

    [Test]
    public void Validar_PreguntaNoConectada_NoSeExigeAunqueEsteVacia()
    {
        Assert.IsTrue(logica.ValidarRespuestasCompletas(false, null, false, null, false, null));
        Assert.IsTrue(logica.ValidarRespuestasCompletas(true, "a", false, null, true, "c"));
    }

    // ── respuestas_json de la encuesta de satisfacción ───────────────────

    [Test]
    public void Satisfaccion_ArmaElEsquemaFijo()
    {
        string json = logica.ConstruirRespuestasSatisfaccion("4", "5", "adecuado", "3", "Sí");

        Assert.AreEqual(
            "{\"ritmo\":\"adecuado\",\"navegacion\":4,\"recomendaria\":true,\"claridad_tema\":3,\"instrucciones\":5}",
            json);
    }

    [Test]
    public void Satisfaccion_RecortaEspaciosYPasaElRitmoAMinusculas()
    {
        string json = logica.ConstruirRespuestasSatisfaccion(" 4 ", " 5 ", "  Adecuado ", " 3 ", " Sí ");

        StringAssert.Contains("\"ritmo\":\"adecuado\"", json);
        StringAssert.Contains("\"navegacion\":4,", json);
        StringAssert.Contains("\"instrucciones\":5}", json);
        StringAssert.Contains("\"recomendaria\":true", json);
    }

    // Una escala vacía o no entera no debe producir JSON inválido ("navegacion":,).
    [TestCase("", "5", "3", "\"navegacion\":null,", "\"instrucciones\":5}", "\"claridad_tema\":3,")]
    [TestCase("4", "", "3", "\"navegacion\":4,", "\"instrucciones\":null}", "\"claridad_tema\":3,")]
    [TestCase("4", "5", "", "\"navegacion\":4,", "\"instrucciones\":5}", "\"claridad_tema\":null,")]
    [TestCase("cuatro", "5", "3", "\"navegacion\":null,", "\"instrucciones\":5}", "\"claridad_tema\":3,")]
    [TestCase("4.5", "5", "3", "\"navegacion\":null,", "\"instrucciones\":5}", "\"claridad_tema\":3,")]
    [TestCase("4", "5\"; DROP", "3", "\"navegacion\":4,", "\"instrucciones\":null}", "\"claridad_tema\":3,")]
    public void Satisfaccion_EscalaNoEntera_SeEnviaComoNullYElJsonSigueValido(
        string q1, string q2, string q4, string esperado1, string esperado2, string esperado4)
    {
        string json = logica.ConstruirRespuestasSatisfaccion(q1, q2, "adecuado", q4, "Sí");

        StringAssert.Contains(esperado1, json);
        StringAssert.Contains(esperado2, json);
        StringAssert.Contains(esperado4, json);
        StringAssert.DoesNotContain(":,", json);
        StringAssert.DoesNotContain(":}", json);
    }

    [Test]
    public void Satisfaccion_TodasLasEscalasInvalidas_ArmaUnObjetoConNulls()
    {
        string json = logica.ConstruirRespuestasSatisfaccion("", "", "adecuado", "", "No");

        Assert.AreEqual(
            "{\"ritmo\":\"adecuado\",\"navegacion\":null,\"recomendaria\":false,\"claridad_tema\":null,\"instrucciones\":null}",
            json);
    }

    [TestCase("No")]
    [TestCase("")]
    [TestCase("sí")]   // la comparación es exacta: solo "Sí" cuenta como recomendaría = true
    public void Satisfaccion_RecomendariaEsFalsoSiNoEsExactamenteSi(string q5)
    {
        string json = logica.ConstruirRespuestasSatisfaccion("4", "5", "adecuado", "3", q5);

        StringAssert.Contains("\"recomendaria\":false", json);
    }

    [Test]
    public void Satisfaccion_EscapaComillasEnElRitmo()
    {
        string json = logica.ConstruirRespuestasSatisfaccion("4", "5", "a\"b\\c", "3", "Sí");

        StringAssert.Contains("\"ritmo\":\"a\\\"b\\\\c\"", json);
    }

    // ── respuestas_json del cuestionario de práctica ─────────────────────

    [Test]
    public void Practica_UnObjetoPorPreguntaNumeradoDesdeUno()
    {
        string json = logica.ConstruirRespuestasPractica(new[]
        {
            ("¿Uno?", "a"),
            ("¿Dos?", "b"),
        });

        Assert.AreEqual(
            "{\"1\":{\"pregunta\":\"¿Uno?\",\"respuesta\":\"a\"},\"2\":{\"pregunta\":\"¿Dos?\",\"respuesta\":\"b\"}}",
            json);
    }

    [Test]
    public void Practica_RecortaEscapaYNoPierdeCaracteresEspeciales()
    {
        string json = logica.ConstruirRespuestasPractica(new[]
        {
            ("  P\"1\"  ", "línea1\nlínea2\t\\fin\r"),
        });

        Assert.AreEqual(
            "{\"1\":{\"pregunta\":\"P\\\"1\\\"\",\"respuesta\":\"línea1\\nlínea2\\t\\\\fin\"}}",
            json);
    }

    [Test]
    public void Practica_SinPreguntasEsObjetoVacio() =>
        Assert.AreEqual("{}", logica.ConstruirRespuestasPractica(new (string, string)[0]));

    // ── PlayerPrefs (se respaldan y restauran las claves q_1..q_5 reales) ─

    const int Total = 5;
    string[] respaldo;

    string[] LeerClaves()
    {
        var valores = new string[Total * 2];
        for (int i = 1; i <= Total; i++)
        {
            valores[(i - 1) * 2] = PlayerPrefs.HasKey($"q_{i}_pregunta") ? PlayerPrefs.GetString($"q_{i}_pregunta") : null;
            valores[(i - 1) * 2 + 1] = PlayerPrefs.HasKey($"q_{i}_respuesta") ? PlayerPrefs.GetString($"q_{i}_respuesta") : null;
        }
        return valores;
    }

    void EscribirClaves(string[] valores)
    {
        for (int i = 1; i <= Total; i++)
        {
            string p = valores[(i - 1) * 2], r = valores[(i - 1) * 2 + 1];
            if (p == null) PlayerPrefs.DeleteKey($"q_{i}_pregunta"); else PlayerPrefs.SetString($"q_{i}_pregunta", p);
            if (r == null) PlayerPrefs.DeleteKey($"q_{i}_respuesta"); else PlayerPrefs.SetString($"q_{i}_respuesta", r);
        }
        PlayerPrefs.Save();
    }

    [Test]
    public void PlayerPrefs_GuardarLeerYLimpiar()
    {
        respaldo = LeerClaves();
        try
        {
            logica.LimpiarRespuestasGuardadas(Total);

            logica.GuardarRespuesta(1, "P1", "R1");
            logica.GuardarRespuesta(2, "P2", "R2");

            Assert.AreEqual("P1", PlayerPrefs.GetString("q_1_pregunta"));
            Assert.AreEqual("R2", PlayerPrefs.GetString("q_2_respuesta"));
            Assert.AreEqual(
                logica.ConstruirRespuestasPractica(new[] { ("P1", "R1"), ("P2", "R2"), ("", ""), ("", ""), ("", "") }),
                logica.ConstruirRespuestasPractica(Total),
                "la versión con PlayerPrefs debe coincidir con la pura");

            logica.LimpiarRespuestasGuardadas(Total);

            for (int i = 1; i <= Total; i++)
            {
                Assert.IsFalse(PlayerPrefs.HasKey($"q_{i}_pregunta"), $"q_{i}_pregunta");
                Assert.IsFalse(PlayerPrefs.HasKey($"q_{i}_respuesta"), $"q_{i}_respuesta");
            }
        }
        finally
        {
            EscribirClaves(respaldo);
        }
    }

    [Test]
    public void PlayerPrefs_SatisfaccionLeeQ1aQ5()
    {
        respaldo = LeerClaves();
        try
        {
            logica.GuardarRespuesta(1, "nav", "4");
            logica.GuardarRespuesta(2, "ins", "5");
            logica.GuardarRespuesta(3, "rit", "Adecuado");
            logica.GuardarRespuesta(4, "cla", "3");
            logica.GuardarRespuesta(5, "rec", "Sí");

            Assert.AreEqual(
                logica.ConstruirRespuestasSatisfaccion("4", "5", "Adecuado", "3", "Sí"),
                logica.ConstruirRespuestasSatisfaccion());
        }
        finally
        {
            EscribirClaves(respaldo);
        }
    }
}
