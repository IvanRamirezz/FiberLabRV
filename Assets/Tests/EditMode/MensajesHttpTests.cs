using NUnit.Framework;

public class MensajesHttpTests
{
    [Test]
    public void CodigoCero_EsFalloDeRed() =>
        Assert.AreEqual("Error de red. Intenta de nuevo.", MensajesHttp.ErrorEnvio(0, null));

    [Test]
    public void CodigoHttp_IncluyeCodigoYCuerpo() =>
        Assert.AreEqual("Error 409: duplicado", MensajesHttp.ErrorEnvio(409, "duplicado"));

    [Test]
    public void CodigoHttp_ConCuerpoNulo_NoFalla() =>
        Assert.AreEqual("Error 500: ", MensajesHttp.ErrorEnvio(500, null));
}
