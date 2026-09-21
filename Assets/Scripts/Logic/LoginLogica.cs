using UnityEngine;

// Lógica de dominio de LoginSupabaseREST: interpreta la respuesta de cada paso
// del login (autenticar → usuario_id → ¿es alumno?) y decide qué sigue. No sabe
// de UI (popups, PlayerPrefs, escenas: eso es de LoginSupabaseREST) ni de HTTP
// (eso es de LoginRepository). code == 0 significa falla de red.
public class LoginLogica
{
    // Qué debe hacer el MonoBehaviour tras interpretar un paso.
    public enum Resultado
    {
        Continuar,
        SinRed,                 // popup de wifi
        CredencialesIncorrectas, // popup "Credenciales incorrectas"
        ErrorVerificacion       // popup "No pudimos verificar tu cuenta"
    }

    public enum Destino { ConGrupo, SinGrupo, NoAlumno }

    public class ResultadoSignIn
    {
        public Resultado resultado;
        public string accessToken;
        public string refreshToken;
        public string authUid;
    }

    public class ResultadoUsuario
    {
        public Resultado resultado;
        public long usuarioId;
    }

    public class ResultadoAlumno
    {
        public Resultado resultado;
        public bool esAlumno;
        public int alumnoId;
        public bool tieneGrupo;
        public Destino destino;
    }

    [System.Serializable] private class AuthResponse { public string access_token; public string refresh_token; public UserData user; }
    [System.Serializable] private class UserData { public string id; }
    [System.Serializable] private class UsuarioRow { public long usuario_id; }
    [System.Serializable] private class AlumnoRow { public long alumno_id; }

    // Paso 1. Ante cualquier error HTTP se culpa a las credenciales
    // (hallazgo incidental pendiente: un 500 también culpa al usuario).
    public ResultadoSignIn InterpretarSignIn(bool ok, long code, string body)
    {
        if (!ok && code == 0) return new ResultadoSignIn { resultado = Resultado.SinRed };
        if (!ok) return new ResultadoSignIn { resultado = Resultado.CredencialesIncorrectas };

        var auth = JsonUtility.FromJson<AuthResponse>(body);
        if (auth == null || string.IsNullOrEmpty(auth.access_token) || auth.user == null)
            return new ResultadoSignIn { resultado = Resultado.CredencialesIncorrectas };

        return new ResultadoSignIn
        {
            resultado = Resultado.Continuar,
            accessToken = auth.access_token,
            refreshToken = auth.refresh_token,
            authUid = auth.user.id
        };
    }

    // Paso 2. Mismo criterio de error que el paso 1.
    public ResultadoUsuario InterpretarUsuario(bool ok, long code, string body)
    {
        if (!ok && code == 0) return new ResultadoUsuario { resultado = Resultado.SinRed };
        if (!ok) return new ResultadoUsuario { resultado = Resultado.CredencialesIncorrectas };

        var arr = JsonHelper.FromJson<UsuarioRow>(body);
        if (arr == null || arr.Length == 0)
            return new ResultadoUsuario { resultado = Resultado.CredencialesIncorrectas };

        return new ResultadoUsuario { resultado = Resultado.Continuar, usuarioId = arr[0].usuario_id };
    }

    // Paso 3. El ROL se decide solo por este cuerpo: distinto de "[]" = alumno.
    // Ver la NOTA de LoginRepository.ObtenerAlumno sobre el RLS silencioso.
    // Un error HTTP aquí NO culpa a las credenciales sino a la verificación.
    public ResultadoAlumno InterpretarAlumno(bool ok, long code, string body)
    {
        if (!ok && code == 0) return new ResultadoAlumno { resultado = Resultado.SinRed };
        if (!ok) return new ResultadoAlumno { resultado = Resultado.ErrorVerificacion };

        bool esAlumno = !string.IsNullOrEmpty(body) && body != "[]";
        var rol = new ResultadoAlumno { resultado = Resultado.Continuar, esAlumno = esAlumno };

        if (esAlumno)
        {
            rol.tieneGrupo = body.Contains("\"grupo_id\"")
                          && !body.Contains("\"grupo_id\":null");

            // Hallazgo incidental pendiente: arr[0] sin validar (si el cuerpo no es
            // "[]" pero tampoco parsea, lanza excepción y el login queda colgado).
            var arr = JsonHelper.FromJson<AlumnoRow>(body);
            rol.alumnoId = (int)arr[0].alumno_id;
            rol.destino = rol.tieneGrupo ? Destino.ConGrupo : Destino.SinGrupo;
        }
        else
        {
            // profesor, admin, o cualquier otro rol válido → misma escena
            rol.destino = Destino.NoAlumno;
        }

        return rol;
    }
}
