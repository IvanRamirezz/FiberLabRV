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

    private LoginRepository repository;
    private LoginLogica logica;

    private void Awake()
    {
        repository = new LoginRepository(supabaseConfig);
        logica = new LoginLogica();
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

        var auth = logica.InterpretarSignIn(ok, code, body);
        if (!Continuar(auth.resultado)) yield break;

        PlayerPrefs.SetString("sb_access_token", auth.accessToken);
        PlayerPrefs.SetString("sb_refresh_token", auth.refreshToken);
        PlayerPrefs.SetString("auth_uid", auth.authUid);
        PlayerPrefs.SetString("ultimo_email", email);
        PlayerPrefs.Save();

        yield return StartCoroutine(ObtenerUsuarioId(auth.accessToken, auth.authUid));
    }

    // ── Paso 2: obtener usuario_id ────────────────────────────────────────────

    private IEnumerator ObtenerUsuarioId(string token, string authUid)
    {
        bool ok = false;
        long code = 0;
        string body = null;
        yield return StartCoroutine(repository.ObtenerUsuarioId(token, authUid,
            (success, responseCode, responseBody) => { ok = success; code = responseCode; body = responseBody; }));

        var usuario = logica.InterpretarUsuario(ok, code, body);
        if (!Continuar(usuario.resultado)) yield break;

        yield return StartCoroutine(VerificarSiEsAlumno(token, usuario.usuarioId));
    }

    // ── Paso 3: ¿es alumno? → flujo alumno. ¿no? → Practicas-Profe ──────────

    private IEnumerator VerificarSiEsAlumno(string token, long usuarioId)
    {
        bool ok = false;
        long code = 0;
        string body = null;
        yield return StartCoroutine(repository.ObtenerAlumno(token, usuarioId,
            (success, responseCode, responseBody) => { ok = success; code = responseCode; body = responseBody; }));

        var rol = logica.InterpretarAlumno(ok, code, body);
        if (!Continuar(rol.resultado)) yield break;

        PlayerPrefs.SetInt("usuario_id", (int)usuarioId);
        PlayerPrefs.SetString("rol", rol.esAlumno ? "alumno" : "otro");

        if (rol.esAlumno)
        {
            PlayerPrefs.SetInt("alumno_id", rol.alumnoId);
            PlayerPrefs.SetInt("tiene_grupo", rol.tieneGrupo ? 1 : 0);
        }

        PlayerPrefs.Save();
        yield return StartCoroutine(RegistrarYNavegar(token, EscenaDe(rol.destino)));
    }

    // ── Traducir el resultado de la lógica a UI / escena ──────────────────────

    // Muestra el popup que corresponda; devuelve true si el flujo debe continuar.
    private bool Continuar(LoginLogica.Resultado resultado)
    {
        switch (resultado)
        {
            case LoginLogica.Resultado.SinRed: ShowWifi(); return false;
            case LoginLogica.Resultado.CredencialesIncorrectas: ShowCredenciales(); return false;
            case LoginLogica.Resultado.ErrorVerificacion: ShowErrorVerificacion(); return false;
            default: return true;
        }
    }

    private string EscenaDe(LoginLogica.Destino destino)
    {
        switch (destino)
        {
            case LoginLogica.Destino.ConGrupo: return sceneConGrupo;
            case LoginLogica.Destino.SinGrupo: return sceneSinGrupo;
            default: return sceneNoAlumno;
        }
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