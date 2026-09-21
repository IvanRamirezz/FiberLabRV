using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using Debug = UnityEngine.Debug;

public class LoginSupabaseREST : MonoBehaviour
{
    [Header("UI (TMP)")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;

    [Header("Popups")]
    public GameObject popupWifi;
    public GameObject popupCredenciales;
    public TMP_Text popupCredencialesTitulo;
    public TMP_Text popupCredencialesCuerpo;

    [Header("Scenes")]
    public string sceneConGrupo = "Bienvenida";
    public string sceneSinGrupo = "Codigo";
    public string sceneNoAlumno = "Demo";

    [Header("Supabase Config")]
    public SupabaseConfig supabaseConfig;

    [System.Serializable] private class AuthResponse { public string access_token; public string refresh_token; public UserData user; }
    [System.Serializable] private class UserData { public string id; }
    [System.Serializable] private class UsuarioRow { public long usuario_id; }
    [System.Serializable] private class AlumnoRow { public long alumno_id; }

    // Resultado de decidir el rol del usuario (ver ProcesarAlumno).
    private class ResultadoRol
    {
        public bool esAlumno;
        public int alumnoId;
        public bool tieneGrupo;
        public string destino;
    }

    private LoginRepository repository;

    private void Awake()
    {
        repository = new LoginRepository(supabaseConfig);
    }

    // ── Start ─────────────────────────────────────────────────────────────────

    private void Start()
    {
        string emailGuardado = PlayerPrefs.GetString("ultimo_email", "");
        if (!string.IsNullOrEmpty(emailGuardado))
            emailInput.text = emailGuardado;
    }

    // ── Botón login ───────────────────────────────────────────────────────────

    public void OnClickLogin()
    {
        HidePopups();
        string email = emailInput.text.Trim();
        string pass = passwordInput.text;
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass)) { ShowCredenciales(); return; }
        StartCoroutine(SignIn(email, pass));
    }

    // ── Paso 1: autenticar ────────────────────────────────────────────────────

    private IEnumerator SignIn(string email, string pass)
    {
        bool ok = false;
        long code = 0;
        string body = null;
        yield return StartCoroutine(repository.IniciarSesion(email, pass,
            (success, responseCode, responseBody) => { ok = success; code = responseCode; body = responseBody; }));

        if (!ProcesarSignIn(ok, code, body, out AuthResponse auth)) yield break;

        PlayerPrefs.SetString("sb_access_token", auth.access_token);
        PlayerPrefs.SetString("sb_refresh_token", auth.refresh_token);
        PlayerPrefs.SetString("auth_uid", auth.user.id);
        PlayerPrefs.SetString("ultimo_email", email);
        PlayerPrefs.Save();

        yield return StartCoroutine(ObtenerUsuarioId(auth.access_token, auth.user.id));
    }

    // ── Paso 2: obtener usuario_id ────────────────────────────────────────────

    private IEnumerator ObtenerUsuarioId(string token, string authUid)
    {
        bool ok = false;
        long code = 0;
        string body = null;
        yield return StartCoroutine(repository.ObtenerUsuarioId(token, authUid,
            (success, responseCode, responseBody) => { ok = success; code = responseCode; body = responseBody; }));

        if (!ProcesarUsuario(ok, code, body, out long usuarioId)) yield break;

        yield return StartCoroutine(VerificarSiEsAlumno(token, usuarioId));
    }

    // ── Paso 3: ¿es alumno? → flujo alumno. ¿no? → Practicas-Profe ──────────

    private IEnumerator VerificarSiEsAlumno(string token, long usuarioId)
    {
        bool ok = false;
        long code = 0;
        string body = null;
        yield return StartCoroutine(repository.ObtenerAlumno(token, usuarioId,
            (success, responseCode, responseBody) => { ok = success; code = responseCode; body = responseBody; }));

        if (!ProcesarAlumno(ok, code, body, out ResultadoRol rol)) yield break;

        PlayerPrefs.SetInt("usuario_id", (int)usuarioId);
        PlayerPrefs.SetString("rol", rol.esAlumno ? "alumno" : "otro");

        if (rol.esAlumno)
        {
            PlayerPrefs.SetInt("alumno_id", rol.alumnoId);
            PlayerPrefs.SetInt("tiene_grupo", rol.tieneGrupo ? 1 : 0);
        }

        PlayerPrefs.Save();
        yield return StartCoroutine(RegistrarYNavegar(token, rol.destino));
    }

    // ── Seams: interpretan la respuesta de cada paso (sin PlayerPrefs ni navegación) ──

    // Devuelve true si el login fue válido y el flujo debe continuar al paso 2.
    // Pasos 1 y 2 muestran "Credenciales incorrectas" ante cualquier error HTTP
    // (hallazgo incidental pendiente: un 500 también culpa al usuario).
    private bool ProcesarSignIn(bool ok, long code, string body, out AuthResponse auth)
    {
        auth = null;

        if (!ok && code == 0) { ShowWifi(); return false; }
        if (!ok) { ShowCredenciales(); return false; }

        auth = JsonUtility.FromJson<AuthResponse>(body);
        if (auth == null || string.IsNullOrEmpty(auth.access_token) || auth.user == null)
        { ShowCredenciales(); return false; }

        return true;
    }

    // Devuelve true si se obtuvo el usuario_id y el flujo debe continuar al paso 3.
    private bool ProcesarUsuario(bool ok, long code, string body, out long usuarioId)
    {
        usuarioId = 0;

        if (!ok && code == 0) { ShowWifi(); return false; }
        if (!ok) { ShowCredenciales(); return false; }

        var arr = JsonHelper.FromJson<UsuarioRow>(body);
        if (arr == null || arr.Length == 0) { ShowCredenciales(); return false; }

        usuarioId = arr[0].usuario_id;
        return true;
    }

    // Devuelve true si se pudo decidir el rol y el flujo debe continuar a registrar
    // sesión y navegar. Decide también la escena destino (alumno con/sin grupo, o
    // no-alumno). Ver la NOTA de LoginRepository.ObtenerAlumno sobre el RLS silencioso.
    private bool ProcesarAlumno(bool ok, long code, string body, out ResultadoRol rol)
    {
        rol = null;

        if (!ok && code == 0) { ShowWifi(); return false; }
        if (!ok) { ShowErrorVerificacion(); return false; }

        bool esAlumno = !string.IsNullOrEmpty(body) && body != "[]";
        rol = new ResultadoRol { esAlumno = esAlumno };

        if (esAlumno)
        {
            rol.tieneGrupo = body.Contains("\"grupo_id\"")
                          && !body.Contains("\"grupo_id\":null");

            // Hallazgo incidental pendiente: arr[0] sin validar (si el cuerpo no es
            // "[]" pero tampoco parsea, lanza excepción y el login queda colgado).
            var arr = JsonHelper.FromJson<AlumnoRow>(body);
            rol.alumnoId = (int)arr[0].alumno_id;
            rol.destino = rol.tieneGrupo ? sceneConGrupo : sceneSinGrupo;
        }
        else
        {
            // profesor, admin, o cualquier otro rol válido → misma escena
            rol.destino = sceneNoAlumno;
        }

        return true;
    }

    // ── Registrar sesión y navegar ────────────────────────────────────────────

    private IEnumerator RegistrarYNavegar(string token, string destino)
    {
        if (SessionManager.Instance != null)
        {
            bool listo = false;
            SessionManager.Instance.RegistrarSesion(() => listo = true);
            yield return new WaitUntil(() => listo);
        }
        Debug.Log($">>> Navegando a: {destino}");
        SceneManager.LoadScene(destino);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void HidePopups()
    {
        if (popupWifi) popupWifi.SetActive(false);
        if (popupCredenciales) popupCredenciales.SetActive(false);
    }

    private void ShowWifi() { HidePopups(); if (popupWifi) popupWifi.SetActive(true); }

    private void ShowCredenciales() =>
        MostrarPopupCredenciales("Credenciales incorrectas.", "Correo o contraseña ingresada no validos.");

    private void ShowErrorVerificacion() =>
        MostrarPopupCredenciales("No pudimos verificar tu cuenta", "Ocurrió un problema al iniciar sesión. Intenta de nuevo en unos momentos.");

    private void MostrarPopupCredenciales(string titulo, string cuerpo)
    {
        HidePopups();
        if (popupCredencialesTitulo != null) popupCredencialesTitulo.text = titulo;
        if (popupCredencialesCuerpo != null) popupCredencialesCuerpo.text = cuerpo;
        if (popupCredenciales != null) popupCredenciales.SetActive(true);
    }
}