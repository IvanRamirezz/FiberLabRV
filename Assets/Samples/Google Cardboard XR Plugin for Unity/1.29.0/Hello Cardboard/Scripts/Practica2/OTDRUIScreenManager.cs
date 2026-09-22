using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// ════════════════════════════════════════════════════════════════
// OTDRUIScreenManager.cs
// Gestor de pantallas del OTDR virtual (Práctica 2).
//
// Pantallas: Setup, Progress (carga), Trace, EventTable e Info.
// Quiz y Feedback se retiraron de la aplicación: las preguntas de
// predicción se resuelven en el cuestionario escrito de la práctica.
//
// Cada pantalla declara su primer Selectable, porque en Cardboard el
// joystick se queda sin objeto seleccionado al desactivar un panel y
// la navegación deja de responder.
//
// Los eventos estáticos permiten que el futuro P2_InstructionManager
// avance pasos al detectar que el alumno llegó a la pantalla correcta.
// ════════════════════════════════════════════════════════════════

public enum OTDRScreenID
{
    Setup,
    Progress,
    Trace,
    EventTable,
    Info,
    Startup
}

[Serializable]
public class OTDRScreen
{
    public OTDRScreenID id;
    public GameObject root;            // Panel de la pantalla
    public GameObject firstSelected;   // Control que recibe el foco al abrirla
}

public class OTDRUIScreenManager : MonoBehaviour
{
    public static OTDRUIScreenManager Instance { get; private set; }

    // ──────────────── EVENTOS PARA P2_InstructionManager ────────────────
    public static event Action<OTDRScreenID> OnScreenChanged;
    public static event Action<bool> OnPowerChanged;

    [Header("Pantallas")]
    public List<OTDRScreen> screens = new List<OTDRScreen>();

    [Header("Encendido")]
    public Button powerButton;          // debe vivir FUERA de los paneles y del screenOffPanel

    [Header("Estado apagado")]
    public GameObject screenOffPanel;   // Panel negro encima de la pantalla

    [Header("Arranque")]
    public bool startPoweredOn = false;
    public OTDRScreenID startScreen = OTDRScreenID.Setup;

    OTDRScreen currentScreen;
    bool isOn;

    public bool IsOn => isOn;
    public OTDRScreenID CurrentScreen =>
        currentScreen != null ? currentScreen.id : OTDRScreenID.Setup;

    // ──────────────── LIFECYCLE ────────────────

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Estado inicial coherente: ninguna pantalla activa, panel negro puesto.
        foreach (var s in screens)
            if (s.root != null) s.root.SetActive(false);

        isOn = false;
        if (screenOffPanel != null) screenOffPanel.SetActive(true);

        if (startPoweredOn) PowerOn();
    }

    // ──────────────── ENCENDIDO / APAGADO ────────────────

    /// <summary>Botón físico de encendido del OTDR.</summary>
    public void TogglePower()
    {
        if (isOn) PowerOff();
        else PowerOn();
    }

    public void PowerOn()
    {
        if (isOn) return;
        isOn = true;

        if (screenOffPanel != null) screenOffPanel.SetActive(false);
        Show(startScreen);

        OnPowerChanged?.Invoke(true);
    }

    public void PowerOff()
    {
        if (!isOn) return;
        isOn = false;

        if (currentScreen != null && currentScreen.root != null)
            currentScreen.root.SetActive(false);
        currentScreen = null;

        if (screenOffPanel != null) screenOffPanel.SetActive(true);

        // Con el equipo apagado el único control alcanzable es el de
        // encendido. Dejar la selección en null mata la navegación por
        // joystick y obliga a salir y volver a entrar al Focus Mode.
        SetSelection(powerButton != null ? powerButton.gameObject : null);

        OnPowerChanged?.Invoke(false);
    }

    // ──────────────── CAMBIO DE PANTALLA ────────────────

    public void Show(OTDRScreenID id)
    {
        if (!isOn)
        {
            Debug.LogWarning($"[OTDR] Se pidió la pantalla {id} con el equipo apagado. Ignorado.");
            return;
        }

        OTDRScreen target = screens.Find(s => s.id == id);
        if (target == null || target.root == null)
        {
            Debug.LogError($"[OTDR] No hay pantalla asignada para {id} en el inspector.");
            return;
        }

        if (currentScreen != null && currentScreen.root != null && currentScreen != target)
            currentScreen.root.SetActive(false);

        currentScreen = target;
        currentScreen.root.SetActive(true);

        FocusFirstSelectable(currentScreen);
        OnScreenChanged?.Invoke(id);
    }

    /// <summary>
    /// Mueve la selección del EventSystem al primer control de la pantalla.
    /// La pantalla Progress no tiene controles a propósito: ahí la selección
    /// queda vacía durante la adquisición y se restaura al mostrar Trace.
    /// </summary>
    void FocusFirstSelectable(OTDRScreen screen)
    {
        GameObject target = screen.firstSelected;

        // Respaldo: si no se asignó, tomar el primer Selectable activo del panel.
        if (target == null || !target.activeInHierarchy)
        {
            Selectable fallback = screen.root.GetComponentInChildren<Selectable>(false);
            target = fallback != null ? fallback.gameObject : null;
        }

        SetSelection(target);
    }

    /// <summary>
    /// Unity ignora la asignación si el objeto ya estaba seleccionado,
    /// por eso el paso previo por null.
    /// </summary>
    void SetSelection(GameObject target)
    {
        if (EventSystem.current == null) return;

        EventSystem.current.SetSelectedGameObject(null);
        if (target != null)
            EventSystem.current.SetSelectedGameObject(target);
    }

    // ──────────────── WRAPPERS PARA LOS BOTONES DEL INSPECTOR ────────────────
    // (OnClick de Unity no puede pasar enums, por eso los métodos sueltos)

    public void ShowSetup() { Show(OTDRScreenID.Setup); }
    public void ShowProgress() { Show(OTDRScreenID.Progress); }
    public void ShowTrace() { Show(OTDRScreenID.Trace); }
    public void ShowEventTable() { Show(OTDRScreenID.EventTable); }
    public void ShowInfo() { Show(OTDRScreenID.Info); }
}