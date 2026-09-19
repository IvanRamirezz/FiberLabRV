using UnityEngine;

/// <summary>
/// Ponlo en el botón de Logout.
/// Siempre encuentra el SessionManager via Singleton, sin importar la escena.
/// </summary>
public class LogoutButton : MonoBehaviour
{
    public void OnClickLogout()
    {
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.Logout();
        }
        else
        {
            // Fallback si por alguna razón no existe el singleton
            UnityEngine.SceneManagement.SceneManager.LoadScene("Login");
        }
    }
}
