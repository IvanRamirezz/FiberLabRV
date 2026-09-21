// Lógica de dominio de JoinGroup: valida la sesión e interpreta la respuesta de
// cada paso (buscar grupo por código, asignar al alumno) para decidir qué sigue
// y qué mensaje mostrar. No sabe de UI (eso es de JoinGroup) ni de HTTP (eso es
// de JoinGroupRepository). code == 0 significa falla de red.
public class JoinGroupLogica
{
    public const string MensajeSesionInvalida = "Sesión inválida. Vuelve a iniciar sesión.";

    public enum Accion
    {
        Continuar,
        MostrarMensaje,   // texto en statusText y el flujo se detiene
        CodigoInvalido    // popup de código incorrecto y el flujo se detiene
    }

    public class Resultado
    {
        public Accion accion;
        public string mensaje = "";
        public int grupoId;
    }

    [System.Serializable] private class GrupoRow { public int grupo_id; public string codigo_acceso; }

    public bool SesionValida(string accessToken, int alumnoId) =>
        !string.IsNullOrEmpty(accessToken) && alumnoId != 0;

    // Paso 1: buscar el grupo por codigo_acceso.
    public Resultado InterpretarBusqueda(bool ok, long code, string body)
    {
        if (!ok && code == 0) return Mensaje("Error de red. Intenta de nuevo.");

        var basico = HandleReqBasics(code,
            "Sin permisos para validar el código (revisa RLS/policies).",
            "Error al validar el código. Intenta de nuevo.");
        if (basico != null) return basico;

        var grupos = JsonHelper.FromJson<GrupoRow>(body);
        if (grupos == null || grupos.Length == 0)
            return new Resultado { accion = Accion.CodigoInvalido };

        return new Resultado { accion = Accion.Continuar, grupoId = grupos[0].grupo_id };
    }

    // Paso 2: PATCH de alumnos.grupo_id.
    public Resultado InterpretarAsignacion(bool ok, long code)
    {
        if (!ok && code == 0) return Mensaje("Error de red al unirse al grupo.");

        return HandleReqBasics(code,
                   "Sin permisos para unirte al grupo (revisa RLS/policies).",
                   "Error al unirse al grupo. Intenta de nuevo.")
               ?? new Resultado { accion = Accion.Continuar };
    }

    // null = el código HTTP es 2xx y el flujo puede seguir.
    private Resultado HandleReqBasics(long code, string mensajeSinPermisos, string mensajeError)
    {
        if (code == 401 || code == 403) return Mensaje(mensajeSinPermisos);
        if (code < 200 || code >= 300) return Mensaje(mensajeError);
        return null;
    }

    private Resultado Mensaje(string texto) =>
        new Resultado { accion = Accion.MostrarMensaje, mensaje = texto };
}
