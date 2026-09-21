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

    // Un error del servidor (5xx) no es culpa del usuario: se avisa que no se pudo
    // verificar la cuenta en vez de decir "credenciales incorrectas".
    private Resultado ErrorHttp(long code) =>
        code >= 500 ? Resultado.ErrorVerificacion : Resultado.CredencialesIncorrectas;

    // Paso 1. Un error 4xx se toma como credenciales inválidas; un 5xx, como fallo
    // de verificación.
    public ResultadoSignIn InterpretarSignIn(bool ok, long code, string body)
    {
        if (!ok && code == 0) return new ResultadoSignIn { resultado = Resultado.SinRed };
        if (!ok) return new ResultadoSignIn { resultado = ErrorHttp(code) };

        var auth = JsonUtility.FromJson<AuthResponse>(body);
        // JsonUtility nunca deja `user` en null (crea un UserData vacío), así que la
        // ausencia del usuario se detecta por su id, que luego usa ObtenerUsuarioId.
        if (auth == null || string.IsNullOrEmpty(auth.access_token)
            || auth.user == null || string.IsNullOrEmpty(auth.user.id))
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
        if (!ok) return new ResultadoUsuario { resultado = ErrorHttp(code) };

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
            // Un cuerpo que no parsea a una lista no permite decidir el rol: se
            // reporta como error de verificación en vez de dejar el login colgado.
            AlumnoRow[] arr;
            try { arr = JsonHelper.FromJson<AlumnoRow>(body); }
            catch (System.ArgumentException) { arr = null; }

            if (arr == null) return new ResultadoAlumno { resultado = Resultado.ErrorVerificacion };

            // Lista vacía con otra forma que "[]" (p. ej. "[ ]"): tampoco es alumno.
            if (arr.Length == 0) esAlumno = false;
            else
            {
                rol.esAlumno = true;
                rol.tieneGrupo = body.Contains("\"grupo_id\"")
                              && !body.Contains("\"grupo_id\":null");
                rol.alumnoId = (int)arr[0].alumno_id;
                rol.destino = rol.tieneGrupo ? Destino.ConGrupo : Destino.SinGrupo;
            }
        }

        if (!esAlumno)
        {
            // profesor, admin, o cualquier otro rol válido → misma escena
            rol.esAlumno = false;
            rol.destino = Destino.NoAlumno;
        }

        return rol;
    }
}
