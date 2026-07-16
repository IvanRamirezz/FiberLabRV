using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Navega menús UI con el joystick izquierdo y confirma con el botón A (Fire1).
/// Usa Time.unscaledTime — funciona con Time.timeScale = 0 (pausa, inicio de práctica, etc.).
///
/// Uso:
///  - MenuPrincipal:  adjuntar a cualquier GameObject; dejar activePanels vacío.
///  - Practica3:      asignar PauseMenuPanel y BotonIniciarPractica en activePanels;
///                    la navegación solo actúa cuando alguno de ellos está activo.
/// </summary>
public class MenuNavigationController : MonoBehaviour
{
    [Header("Input")]
    public string horizontalAxis = "Horizontal";
    public string verticalAxis   = "Vertical";
    public string submitButton   = "Fire1";

    [Header("Settings")]
    public float inputCooldown = 0.25f;
    public float deadzone      = 0.4f;

    [Header("Contexto (opcional)")]
    [Tooltip("Navegación activa solo cuando AL MENOS UNO de estos paneles/botones está activo. " +
             "Dejar vacío para que siempre esté activa (p.ej. en MenuPrincipal).")]
    public GameObject[] activePanels;

    float _lastInputTime;

    void Update()
    {
        if (!ShouldBeActive()) return;

        EnsureSomethingSelected();
        HandleNavigation();
        HandleSubmit();
    }

    bool ShouldBeActive()
    {
        if (activePanels == null || activePanels.Length == 0) return true;
        foreach (var panel in activePanels)
            if (panel != null && panel.activeInHierarchy) return true;
        return false;
    }

    void HandleNavigation()
    {
        if (Time.unscaledTime - _lastInputTime < inputCooldown) return;

        float h = Input.GetAxisRaw(horizontalAxis);
        float v = Input.GetAxisRaw(verticalAxis);

        MoveDirection dir;
        if      (v >  deadzone) dir = MoveDirection.Up;
        else if (v < -deadzone) dir = MoveDirection.Down;
        else if (h >  deadzone) dir = MoveDirection.Right;
        else if (h < -deadzone) dir = MoveDirection.Left;
        else return;

        var axisData = new AxisEventData(EventSystem.current)
        {
            moveDir    = dir,
            moveVector = new Vector2(h, v)
        };

        ExecuteEvents.Execute(
            EventSystem.current.currentSelectedGameObject,
            axisData,
            ExecuteEvents.moveHandler
        );

        _lastInputTime = Time.unscaledTime;
    }

    void HandleSubmit()
    {
        if (!Input.GetButtonDown(submitButton)) return;

        ExecuteEvents.Execute(
            EventSystem.current.currentSelectedGameObject,
            new BaseEventData(EventSystem.current),
            ExecuteEvents.submitHandler
        );
    }

    void EnsureSomethingSelected()
    {
        if (EventSystem.current == null) return;
        if (EventSystem.current.currentSelectedGameObject != null &&
            EventSystem.current.currentSelectedGameObject.activeInHierarchy) return;

        foreach (var s in Selectable.allSelectablesArray)
        {
            if (s.isActiveAndEnabled && s.interactable)
            {
                EventSystem.current.SetSelectedGameObject(s.gameObject);
                break;
            }
        }
    }
}
