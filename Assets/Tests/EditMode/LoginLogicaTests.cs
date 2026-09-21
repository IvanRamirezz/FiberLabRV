using NUnit.Framework;

public class LoginLogicaTests
{
    LoginLogica logica;

    [SetUp]
    public void SetUp() => logica = new LoginLogica();

    // ── Paso 1: autenticar ───────────────────────────────────────────────

    [Test]
    public void SignIn_Ok_DevuelveTokensYAuthUid()
    {
        var r = logica.InterpretarSignIn(true, 200,
            "{\"access_token\":\"AT\",\"refresh_token\":\"RT\",\"user\":{\"id\":\"uid-1\"}}");

        Assert.AreEqual(LoginLogica.Resultado.Continuar, r.resultado);
        Assert.AreEqual("AT", r.accessToken);
        Assert.AreEqual("RT", r.refreshToken);
        Assert.AreEqual("uid-1", r.authUid);
    }

    [Test]
    public void SignIn_SinRed_MuestraWifi()
    {
        var r = logica.InterpretarSignIn(false, 0, null);

        Assert.AreEqual(LoginLogica.Resultado.SinRed, r.resultado);
        Assert.IsNull(r.accessToken);
    }

    [TestCase(400)]
    [TestCase(401)]
    public void SignIn_CredencialesRechazadas(long codigo) =>
        Assert.AreEqual(LoginLogica.Resultado.CredencialesIncorrectas,
            logica.InterpretarSignIn(false, codigo, "{\"error\":\"invalid_grant\"}").resultado);

    [TestCase("{}")]
    [TestCase("{\"access_token\":\"\",\"user\":{\"id\":\"u\"}}")]
    [TestCase("{\"refresh_token\":\"RT\",\"user\":{\"id\":\"u\"}}")]
    public void SignIn_SinAccessToken_EsCredencialesIncorrectas(string body) =>
        Assert.AreEqual(LoginLogica.Resultado.CredencialesIncorrectas,
            logica.InterpretarSignIn(true, 200, body).resultado);

    // ── Paso 2: usuario_id ───────────────────────────────────────────────

    [Test]
    public void Usuario_Ok_DevuelveElUsuarioId()
    {
        var r = logica.InterpretarUsuario(true, 200, "[{\"usuario_id\":42}]");

        Assert.AreEqual(LoginLogica.Resultado.Continuar, r.resultado);
        Assert.AreEqual(42L, r.usuarioId);
    }

    [Test]
    public void Usuario_SinRed_MuestraWifi() =>
        Assert.AreEqual(LoginLogica.Resultado.SinRed, logica.InterpretarUsuario(false, 0, null).resultado);

    [Test]
    public void Usuario_401_EsCredencialesIncorrectas() =>
        Assert.AreEqual(LoginLogica.Resultado.CredencialesIncorrectas,
            logica.InterpretarUsuario(false, 401, "{}").resultado);

    [Test]
    public void Usuario_ListaVacia_EsCredencialesIncorrectas() =>
        Assert.AreEqual(LoginLogica.Resultado.CredencialesIncorrectas,
            logica.InterpretarUsuario(true, 200, "[]").resultado);

    // ── Paso 3: ¿es alumno? ──────────────────────────────────────────────

    [Test]
    public void Alumno_ConGrupo_VaAlDestinoConGrupo()
    {
        var r = logica.InterpretarAlumno(true, 200, "[{\"alumno_id\":7,\"grupo_id\":3}]");

        Assert.AreEqual(LoginLogica.Resultado.Continuar, r.resultado);
        Assert.IsTrue(r.esAlumno);
        Assert.AreEqual(7, r.alumnoId);
        Assert.IsTrue(r.tieneGrupo);
        Assert.AreEqual(LoginLogica.Destino.ConGrupo, r.destino);
    }

    [Test]
    public void Alumno_GrupoNull_VaAlDestinoSinGrupo()
    {
        var r = logica.InterpretarAlumno(true, 200, "[{\"alumno_id\":7,\"grupo_id\":null}]");

        Assert.IsTrue(r.esAlumno);
        Assert.AreEqual(7, r.alumnoId);
        Assert.IsFalse(r.tieneGrupo);
        Assert.AreEqual(LoginLogica.Destino.SinGrupo, r.destino);
    }

    [Test]
    public void Alumno_SinCampoGrupo_VaAlDestinoSinGrupo()
    {
        var r = logica.InterpretarAlumno(true, 200, "[{\"alumno_id\":7}]");

        Assert.IsFalse(r.tieneGrupo);
        Assert.AreEqual(LoginLogica.Destino.SinGrupo, r.destino);
    }

    [TestCase("[]")]
    [TestCase("")]
    [TestCase(null)]
    public void NoAlumno_ProfesorOAdmin_VaAlDestinoNoAlumno(string body)
    {
        var r = logica.InterpretarAlumno(true, 200, body);

        Assert.AreEqual(LoginLogica.Resultado.Continuar, r.resultado);
        Assert.IsFalse(r.esAlumno);
        Assert.AreEqual(LoginLogica.Destino.NoAlumno, r.destino);
    }

    [Test]
    public void Alumno_SinRed_MuestraWifi() =>
        Assert.AreEqual(LoginLogica.Resultado.SinRed, logica.InterpretarAlumno(false, 0, null).resultado);

    [TestCase(401)]
    [TestCase(403)]
    [TestCase(500)]
    public void Alumno_ErrorHttp_EsErrorDeVerificacionNoCredenciales(long codigo) =>
        Assert.AreEqual(LoginLogica.Resultado.ErrorVerificacion,
            logica.InterpretarAlumno(false, codigo, "{}").resultado);
}
