using System.Globalization;
using NUnit.Framework;

public class CalificacionesLogicaTests
{
    CalificacionesLogica logica;
    CultureInfo culturaOriginal;

    // ToString("0.0") depende de la cultura: se fija la invariante para que las
    // pruebas no dependan del idioma del equipo.
    [SetUp]
    public void SetUp()
    {
        logica = new CalificacionesLogica();
        culturaOriginal = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
    }

    [TearDown]
    public void TearDown() => CultureInfo.CurrentCulture = culturaOriginal;

    // ── Mensajes de error ────────────────────────────────────────────────

    [Test]
    public void MensajeError_SinRed() =>
        Assert.AreEqual("Error de red. Intenta de nuevo.", logica.MensajeError(0));

    [TestCase(401)]
    [TestCase(403)]
    public void MensajeError_SinPermisos(long codigo) =>
        Assert.AreEqual("Sin permisos para ver calificaciones (revisa RLS/policies).", logica.MensajeError(codigo));

    [TestCase(400)]
    [TestCase(500)]
    public void MensajeError_Generico(long codigo) =>
        Assert.AreEqual("Error al consultar calificaciones.", logica.MensajeError(codigo));

    // ── Calificaciones y promedio ────────────────────────────────────────

    [Test]
    public void Interpretar_TresPracticas_MuestraCadaUnaYElPromedio()
    {
        var r = logica.Interpretar(
            "[{\"practica_id\":1,\"calificacion\":8.5}," +
            "{\"practica_id\":2,\"calificacion\":7},{\"practica_id\":3,\"calificacion\":10}]");

        Assert.AreEqual("8.5", r.practica1);
        Assert.AreEqual("7.0", r.practica2);
        Assert.AreEqual("10.0", r.practica3);
        Assert.AreEqual("8.5", r.promedio);   // (8.5 + 7 + 10) / 3
    }

    [Test]
    public void Interpretar_SoloAlgunasPracticas_LasDemasQuedanPendientes()
    {
        var r = logica.Interpretar("[{\"practica_id\":3,\"calificacion\":6}]");

        Assert.AreEqual("-", r.practica1);
        Assert.AreEqual("-", r.practica2);
        Assert.AreEqual("6.0", r.practica3);
        Assert.AreEqual("6.0", r.promedio);
    }

    [Test]
    public void Interpretar_SinResultados_TodoPendienteYPromedioGuion()
    {
        var r = logica.Interpretar("[]");

        Assert.AreEqual("-", r.practica1);
        Assert.AreEqual("-", r.practica2);
        Assert.AreEqual("-", r.practica3);
        Assert.AreEqual("-", r.promedio);
    }

    [Test]
    public void Interpretar_UnaCalificacionCero_CuentaParaElPromedio()
    {
        var r = logica.Interpretar(
            "[{\"practica_id\":1,\"calificacion\":0},{\"practica_id\":2,\"calificacion\":10}]");

        Assert.AreEqual("0.0", r.practica1);
        Assert.AreEqual("5.0", r.promedio);
    }

    [Test]
    public void Interpretar_RedondeaAUnDecimal()
    {
        var r = logica.Interpretar("[{\"practica_id\":1,\"calificacion\":8.57}]");

        Assert.AreEqual("8.6", r.practica1);
    }

    [Test]
    public void Interpretar_PracticaRepetida_MuestraLaUltimaYAmbasCuentanParaElPromedio()
    {
        var r = logica.Interpretar(
            "[{\"practica_id\":1,\"calificacion\":4},{\"practica_id\":1,\"calificacion\":8}]");

        Assert.AreEqual("8.0", r.practica1);
        Assert.AreEqual("6.0", r.promedio);
    }

    [Test]
    public void Interpretar_PracticaFueraDe1a3_NoSeMuestraPeroSiPromedia()
    {
        var r = logica.Interpretar(
            "[{\"practica_id\":1,\"calificacion\":10},{\"practica_id\":9,\"calificacion\":0}]");

        Assert.AreEqual("10.0", r.practica1);
        Assert.AreEqual("-", r.practica2);
        Assert.AreEqual("5.0", r.promedio);
    }
}
