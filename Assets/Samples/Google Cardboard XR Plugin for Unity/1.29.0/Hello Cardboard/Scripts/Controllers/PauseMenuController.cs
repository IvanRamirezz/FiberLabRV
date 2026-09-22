using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;
using System.Collections;

/// <summary>
/// Gestiona el menú de pausa en la escena de juego.
/// - Botón en pantalla visible solo en Modo Normal.
/// - Botón Start del mando abre/cierra el menú en ambos modos.
/// Adjuntar a cualquier GameObject persistente en Practica3.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Panel raíz del menú de pausa (empieza desactivado)")]
    public GameObject pauseMenuPanel;

    [Tooltip("Botón flotante en pantalla para abrir el menú (solo Modo Normal)")]
    public GameObject openMenuButton;

    [Header("Input")]
    [Tooltip("Botón Start del mando. JoystickButton7 = Start en la mayoría de mandos Android")]
    public KeyCode pauseKey = KeyCode.JoystickButton7;

    [Header("Navegación")]
    [Tooltip("Escena a cargar desde el botón de menú principal del menú de pausa")]
    public string sceneMenuPrincipal = "Bienvenida";

    bool _paused;

    void Start()
    {
        // El botón flotante solo aparece en Modo Normal
        if (openMenuButton != null)
            openMenuButton.SetActive(!GameModeManager.IsVRMode);

        // El menú empieza cerrado
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
    }

    void Update()
    {
        // Start/Options del mando funciona en ambos modos
        if (Input.GetKeyDown(pauseKey))
            TogglePause();
    }

    // --- Llamadas desde botones UI ---

    public void TogglePause()
    {
        _paused = !_paused;
        ApplyPauseState();
    }

    public void Resume()
    {
        _paused = false;
        ApplyPauseState();
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        StartCoroutine(GoToMainMenuRoutine());
    }

    IEnumerator GoToMainMenuRoutine()
    {
        // Detener Cardboard/XR antes de cargar la escena 2D de Bienvenida
        // (misma técnica que P1_Instrucciones/InstructionManager_P3 al salir de una práctica)
        var xrMgr = XRGeneralSettings.Instance?.Manager;
        if (xrMgr != null && xrMgr.isInitializationComplete)
        {
            xrMgr.StopSubsystems();
            xrMgr.DeinitializeLoader();
        }
        yield return null; // un frame para que XR termine de cerrarse

        SceneManager.LoadScene(sceneMenuPrincipal);
    }

    // ---------------------------------

    void ApplyPauseState()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(_paused);

        // Congela el tiempo al pausar (el input sigue funcionando con timeScale = 0)
        Time.timeScale = _paused ? 0f : 1f;
    }
}
