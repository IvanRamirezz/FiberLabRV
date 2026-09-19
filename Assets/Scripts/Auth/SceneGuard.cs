using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Debug = UnityEngine.Debug;

public class SceneGuard : MonoBehaviour
{
    [Header("Popup sesión duplicada (opcional)")]
    [Tooltip("Si no asignas uno, redirige directo al Login sin popup")]
    public GameObject popupSesionDuplicada;

    [Header("Delay antes de validar (segundos)")]
    [Tooltip("Tiempo de espera para que el PATCH del UUID llegue a Supabase")]
    public float delayValidacion = 1.5f;

    private void Start()
    {
        if (SessionManager.Instance == null)
        {
            Debug.LogWarning("SceneGuard: SessionManager no encontrado. Redirigiendo al Login.");
            SceneManager.LoadScene("Login");
            return;
        }

        if (popupSesionDuplicada != null)
            SessionManager.Instance.SetPopupSesionDuplicada(popupSesionDuplicada);

        StartCoroutine(ValidarConDelay());
    }

    private IEnumerator ValidarConDelay()
    {
        yield return new WaitForSeconds(delayValidacion);

        // Verificar que el GameObject todavía existe antes de continuar
        if (this == null || !gameObject) yield break;

        string sceneName = gameObject.scene.name;

        SessionManager.Instance.ValidarSesion(
            onValida: () => { if (this != null) Debug.Log("SceneGuard: sesión válida en " + sceneName); },
            onInvalida: () => { if (this != null) Debug.Log("SceneGuard: sesión inválida en " + sceneName); }
        );
    }
}