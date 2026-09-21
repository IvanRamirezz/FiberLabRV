using NUnit.Framework;

public class SatisfaccionLogicaTests
{
    SatisfaccionLogica logica;

    [SetUp]
    public void SetUp() => logica = new SatisfaccionLogica();

    // ── ¿Hay que consultar? ──────────────────────────────────────────────

    [Test]
    public void DebeConsultar_AlumnoConSesionCompleta() =>
        Assert.IsTrue(logica.DebeConsultar("alumno", "token", 5));

    [TestCase("profe")]
    [TestCase("otro")]
    [TestCase("")]
    public void DebeConsultar_RolDistintoDeAlumno_NoConsulta(string rol) =>
        Assert.IsFalse(logica.DebeConsultar(rol, "token", 5));

    [TestCase(null, 5)]
    [TestCase("", 5)]
    [TestCase("token", 0)]
    public void DebeConsultar_AlumnoConSesionIncompleta_NoConsulta(string token, int alumnoId) =>
        Assert.IsFalse(logica.DebeConsultar("alumno", token, alumnoId));

    // ── Qué hacer con la respuesta ───────────────────────────────────────

    [Test]
    public void Respuesta_YaRespondio_SaltaAlFlujoPosterior() =>
        Assert.AreEqual(SatisfaccionLogica.Decision.YaRespondio,
            logica.InterpretarRespuesta(true, 200, "[{\"encuesta_id\":31}]"));

    [Test]
    public void Respuesta_NoHayFila_MuestraLaEncuesta() =>
        Assert.AreEqual(SatisfaccionLogica.Decision.MostrarSinRespuestaPrevia,
            logica.InterpretarRespuesta(true, 200, "[]"));

    [Test]
    public void Respuesta_SinRed_DejaPasarALaEncuesta() =>
        Assert.AreEqual(SatisfaccionLogica.Decision.MostrarPorErrorDeRed,
            logica.InterpretarRespuesta(false, 0, null));

    [TestCase(401)]
    [TestCase(500)]
    public void Respuesta_ErrorHttp_DejaPasarALaEncuesta(long codigo) =>
        Assert.AreEqual(SatisfaccionLogica.Decision.MostrarPorErrorHttp,
            logica.InterpretarRespuesta(false, codigo, "{}"));
}
