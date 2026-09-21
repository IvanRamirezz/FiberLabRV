// Lógica de dominio de CheckPractica: valida la sesión e interpreta la respuesta
// de cada consulta (grupo del alumno, práctica activa, resultado previo) para
// decidir qué sigue. No sabe de UI ni de escenas (eso es de CheckPractica) ni de
// HTTP (eso es de CheckPracticaRepository). code == 0 significa falla de red.
// La vigencia de fechas la filtra el servidor en CheckPracticaRepository.
public class CheckPracticaLogica
{
    public const string MensajeSesionInvalida = "Sesión inválida.";
    public const string MensajeSinGrupo = "Sin grupo asignado.";

    public enum Accion
    {
        Continuar,
        MostrarError,        // texto en statusText y el flujo se detiene
        IrASinPractica       // no hay práctica activa, o el alumno ya la realizó
    }

    public class Resultado
    {
        public Accion accion;
        public string mensaje = "";
        public int practicaId;
        public int grupoId;
    }

    [System.Serializable] private class PracticaGrupoRow { public int practica_id; }
    [System.Serializable] private class AlumnoGrupoRow { public int grupo_id; }
    [System.Serializable] private class ResultadoRow { public int resultado_id; }

    public bool SesionValida(string accessToken, int alumnoId) =>
        !string.IsNullOrEmpty(accessToken) && alumnoId != 0;

    // Grupo del alumno. Continuar trae el grupoId; una falla de red/HTTP se
    // reporta como error (no como "sin grupo") y solo la ausencia real de grupo
    // da MensajeSinGrupo.
    public Resultado InterpretarGrupo(bool ok, long code, string body)
    {
        if (!ok) return Error(code);

        if (string.IsNullOrEmpty(body) || body == "[]" || body.Contains("\"grupo_id\":null"))
            return SinGrupo();

        var arr = JsonHelper.FromJson<AlumnoGrupoRow>(body);
        if (arr == null || arr.Length == 0 || arr[0].grupo_id == 0) return SinGrupo();

        return new Resultado { accion = Accion.Continuar, grupoId = arr[0].grupo_id };
    }

    // Paso 1: práctica activa del grupo. Continuar trae el practicaId.
    public Resultado InterpretarPracticaActiva(bool ok, long code, string body)
    {
        if (!ok) return Error(code);

        var practicas = JsonHelper.FromJson<PracticaGrupoRow>(body);

        if (practicas == null || practicas.Length == 0)
            return new Resultado { accion = Accion.IrASinPractica };

        return new Resultado { accion = Accion.Continuar, practicaId = practicas[0].practica_id };
    }

    // Paso 2: Continuar = NO hay resultado previo; IrASinPractica = ya la realizó.
    public Resultado InterpretarResultado(bool ok, long code, string body)
    {
        if (!ok) return Error(code);

        var resultados = JsonHelper.FromJson<ResultadoRow>(body);

        if (resultados != null && resultados.Length > 0)
            return new Resultado { accion = Accion.IrASinPractica };

        return new Resultado { accion = Accion.Continuar };
    }

    // code == 0 significa falla de red (contrato de CheckPracticaRepository).
    public string MensajeError(long code) => code == 0 ? "Error de red." : $"Error {code}";

    private Resultado SinGrupo() =>
        new Resultado { accion = Accion.MostrarError, mensaje = MensajeSinGrupo };

    private Resultado Error(long code) =>
        new Resultado { accion = Accion.MostrarError, mensaje = MensajeError(code) };
}
