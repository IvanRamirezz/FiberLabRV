using NUnit.Framework;

public class JoinGroupLogicaTests
{
    JoinGroupLogica logica;

    [SetUp]
    public void SetUp() => logica = new JoinGroupLogica();

    // ── Sesión ───────────────────────────────────────────────────────────

    [Test]
    public void Sesion_ConTokenYAlumno_EsValida() =>
        Assert.IsTrue(logica.SesionValida("token", 5));

    [TestCase(null, 5)]
    [TestCase("", 5)]
    [TestCase("token", 0)]
    [TestCase("", 0)]
    public void Sesion_SinTokenOSinAlumno_NoEsValida(string token, int alumnoId) =>
        Assert.IsFalse(logica.SesionValida(token, alumnoId));

    // ── Paso 1: buscar por código ────────────────────────────────────────

    [Test]
    public void Busqueda_CodigoValido_ContinuaConElGrupo()
    {
        var r = logica.InterpretarBusqueda(true, 200, "[{\"grupo_id\":12,\"codigo_acceso\":\"ABC123\"}]");

        Assert.AreEqual(JoinGroupLogica.Accion.Continuar, r.accion);
        Assert.AreEqual(12, r.grupoId);
    }

    [Test]
    public void Busqueda_CodigoInvalido_MuestraPopup()
    {
        var r = logica.InterpretarBusqueda(true, 200, "[]");

        Assert.AreEqual(JoinGroupLogica.Accion.CodigoInvalido, r.accion);
        Assert.AreEqual("", r.mensaje);
    }

    [Test]
    public void Busqueda_SinRed_MuestraMensajeDeRed()
    {
        var r = logica.InterpretarBusqueda(false, 0, null);

        Assert.AreEqual(JoinGroupLogica.Accion.MostrarMensaje, r.accion);
        Assert.AreEqual("Error de red. Intenta de nuevo.", r.mensaje);
    }

    [TestCase(401)]
    [TestCase(403)]
    public void Busqueda_SinPermisos_MuestraMensajeDePermisos(long codigo)
    {
        var r = logica.InterpretarBusqueda(false, codigo, "{}");

        Assert.AreEqual(JoinGroupLogica.Accion.MostrarMensaje, r.accion);
        Assert.AreEqual("Sin permisos para validar el código (revisa RLS/policies).", r.mensaje);
    }

    [TestCase(400)]
    [TestCase(404)]
    [TestCase(500)]
    public void Busqueda_ErrorHttp_MuestraMensajeGenerico(long codigo)
    {
        var r = logica.InterpretarBusqueda(false, codigo, "{}");

        Assert.AreEqual(JoinGroupLogica.Accion.MostrarMensaje, r.accion);
        Assert.AreEqual("Error al validar el código. Intenta de nuevo.", r.mensaje);
    }

    // ── Paso 2: asignar al grupo ─────────────────────────────────────────

    [TestCase(200)]
    [TestCase(204)]
    public void Asignacion_Exito_Continua(long codigo) =>
        Assert.AreEqual(JoinGroupLogica.Accion.Continuar, logica.InterpretarAsignacion(true, codigo).accion);

    [Test]
    public void Asignacion_SinRed_MuestraMensajeDeRed()
    {
        var r = logica.InterpretarAsignacion(false, 0);

        Assert.AreEqual(JoinGroupLogica.Accion.MostrarMensaje, r.accion);
        Assert.AreEqual("Error de red al unirse al grupo.", r.mensaje);
    }

    [TestCase(401)]
    [TestCase(403)]
    public void Asignacion_SinPermisos_MuestraMensajeDePermisos(long codigo)
    {
        var r = logica.InterpretarAsignacion(false, codigo);

        Assert.AreEqual(JoinGroupLogica.Accion.MostrarMensaje, r.accion);
        Assert.AreEqual("Sin permisos para unirte al grupo (revisa RLS/policies).", r.mensaje);
    }

    [Test]
    public void Asignacion_ErrorHttp_MuestraMensajeGenerico()
    {
        var r = logica.InterpretarAsignacion(false, 500);

        Assert.AreEqual(JoinGroupLogica.Accion.MostrarMensaje, r.accion);
        Assert.AreEqual("Error al unirse al grupo. Intenta de nuevo.", r.mensaje);
    }
}
