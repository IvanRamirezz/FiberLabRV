using NUnit.Framework;

public class CheckPracticaLogicaTests
{
    CheckPracticaLogica logica;

    [SetUp]
    public void SetUp() => logica = new CheckPracticaLogica();

    // ── Sesión ───────────────────────────────────────────────────────────

    [Test]
    public void Sesion_ConTokenYAlumno_EsValida() =>
        Assert.IsTrue(logica.SesionValida("token", 5));

    [TestCase(null, 5)]
    [TestCase("", 5)]
    [TestCase("token", 0)]
    public void Sesion_SinTokenOSinAlumno_NoEsValida(string token, int alumnoId) =>
        Assert.IsFalse(logica.SesionValida(token, alumnoId));

    // ── Grupo del alumno ─────────────────────────────────────────────────

    [Test]
    public void Grupo_ConGrupo_ContinuaConElId()
    {
        var r = logica.InterpretarGrupo(true, 200, "[{\"grupo_id\":9}]");

        Assert.AreEqual(CheckPracticaLogica.Accion.Continuar, r.accion);
        Assert.AreEqual(9, r.grupoId);
    }

    [TestCase("[]")]
    [TestCase("")]
    [TestCase(null)]
    [TestCase("[{\"grupo_id\":null}]")]
    public void Grupo_SinGrupo_MuestraSinGrupoAsignado(string body)
    {
        var r = logica.InterpretarGrupo(true, 200, body);

        Assert.AreEqual(CheckPracticaLogica.Accion.MostrarError, r.accion);
        Assert.AreEqual("Sin grupo asignado.", r.mensaje);
    }

    // Una falla real ya no se disfraza de "Sin grupo asignado.".
    [Test]
    public void Grupo_SinRed_MuestraErrorDeRedNoSinGrupo()
    {
        var r = logica.InterpretarGrupo(false, 0, null);

        Assert.AreEqual(CheckPracticaLogica.Accion.MostrarError, r.accion);
        Assert.AreEqual("Error de red.", r.mensaje);
    }

    [TestCase(401)]
    [TestCase(500)]
    public void Grupo_ErrorHttp_MuestraElCodigoNoSinGrupo(long codigo)
    {
        var r = logica.InterpretarGrupo(false, codigo, "[{\"grupo_id\":9}]");

        Assert.AreEqual(CheckPracticaLogica.Accion.MostrarError, r.accion);
        Assert.AreEqual($"Error {codigo}", r.mensaje);
    }

    // ── Práctica activa ──────────────────────────────────────────────────

    [Test]
    public void PracticaActiva_Hay_ContinuaConElId()
    {
        var r = logica.InterpretarPracticaActiva(true, 200, "[{\"practica_id\":3}]");

        Assert.AreEqual(CheckPracticaLogica.Accion.Continuar, r.accion);
        Assert.AreEqual(3, r.practicaId);
    }

    [Test]
    public void PracticaActiva_NoHay_VaASinPractica()
    {
        var r = logica.InterpretarPracticaActiva(true, 200, "[]");

        Assert.AreEqual(CheckPracticaLogica.Accion.IrASinPractica, r.accion);
        Assert.AreEqual(0, r.practicaId);
    }

    [Test]
    public void PracticaActiva_SinRed_MuestraErrorDeRed()
    {
        var r = logica.InterpretarPracticaActiva(false, 0, null);

        Assert.AreEqual(CheckPracticaLogica.Accion.MostrarError, r.accion);
        Assert.AreEqual("Error de red.", r.mensaje);
    }

    [TestCase(401)]
    [TestCase(500)]
    public void PracticaActiva_ErrorHttp_MuestraElCodigo(long codigo)
    {
        var r = logica.InterpretarPracticaActiva(false, codigo, "{}");

        Assert.AreEqual(CheckPracticaLogica.Accion.MostrarError, r.accion);
        Assert.AreEqual($"Error {codigo}", r.mensaje);
    }

    // ── Práctica ya realizada ────────────────────────────────────────────

    [Test]
    public void Resultado_NoHayPrevio_Continua() =>
        Assert.AreEqual(CheckPracticaLogica.Accion.Continuar,
            logica.InterpretarResultado(true, 200, "[]").accion);

    [Test]
    public void Resultado_YaRealizada_VaASinPractica() =>
        Assert.AreEqual(CheckPracticaLogica.Accion.IrASinPractica,
            logica.InterpretarResultado(true, 200, "[{\"resultado_id\":77}]").accion);

    [Test]
    public void Resultado_SinRed_MuestraErrorDeRed()
    {
        var r = logica.InterpretarResultado(false, 0, null);

        Assert.AreEqual(CheckPracticaLogica.Accion.MostrarError, r.accion);
        Assert.AreEqual("Error de red.", r.mensaje);
    }

    [Test]
    public void Resultado_ErrorHttp_MuestraElCodigo()
    {
        var r = logica.InterpretarResultado(false, 403, "{}");

        Assert.AreEqual(CheckPracticaLogica.Accion.MostrarError, r.accion);
        Assert.AreEqual("Error 403", r.mensaje);
    }

    // ── Mensajes ─────────────────────────────────────────────────────────

    [Test]
    public void MensajeError_CodigoCeroEsRed_OtroLlevaElCodigo()
    {
        Assert.AreEqual("Error de red.", logica.MensajeError(0));
        Assert.AreEqual("Error 500", logica.MensajeError(500));
    }
}
