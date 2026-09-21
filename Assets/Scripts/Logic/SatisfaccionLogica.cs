// Lógica de dominio de SatisfaccionGate: decide si hay que consultar si el
// alumno ya respondió la encuesta y qué hacer con la respuesta (mostrar la
// encuesta o saltar a "ya respondió"). No sabe de UI ni de escenas (eso es de
// SatisfaccionGate) ni de HTTP (eso es de SatisfaccionRepository).
// code == 0 significa falla de red.
public class SatisfaccionLogica
{
    // Ante cualquier duda (error de red o HTTP) se deja pasar al alumno a la
    // encuesta en lugar de bloquearlo.
    public enum Decision
    {
        MostrarPorErrorDeRed,
        MostrarPorErrorHttp,
        MostrarSinRespuestaPrevia,
        YaRespondio
    }

    [System.Serializable] private class EncuestaRow { public long encuesta_id; }

    // Solo los alumnos con sesión completa se verifican; profesores/admin y
    // sesiones incompletas ven la encuesta sin consulta.
    public bool DebeConsultar(string rol, string accessToken, int alumnoId) =>
        rol == "alumno" && !string.IsNullOrEmpty(accessToken) && alumnoId != 0;

    public Decision InterpretarRespuesta(bool ok, long code, string body)
    {
        if (!ok && code == 0) return Decision.MostrarPorErrorDeRed;
        if (!ok) return Decision.MostrarPorErrorHttp;

        var arr = JsonHelper.FromJson<EncuestaRow>(body);
        return (arr != null && arr.Length > 0)
            ? Decision.YaRespondio
            : Decision.MostrarSinRespuestaPrevia;
    }
}
