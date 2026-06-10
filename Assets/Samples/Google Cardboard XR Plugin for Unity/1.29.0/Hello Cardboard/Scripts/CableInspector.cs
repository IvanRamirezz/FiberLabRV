using UnityEngine;
using System;
using System.Collections.Generic;

public class P1_CableInspector : MonoBehaviour
{
    [Header("Fibras (asignar en orden 1-12 desde inspector)")]
    public List<P1_FiberInteractable> fibers;

    [Header("Input")]
    public string navigateAxis = "Horizontal";
    public string selectButton = "Fire1";
    public float inputCooldown = 0.25f;
    public float navigationThreshold = 0.5f;

    private int selectedIndex = -1;      // -1 = ninguna seleccionada
    private bool isInspecting = false;
    private float lastInputTime;
    [Header("Materiales compartidos")]
    public Material gazeMaterial;
    public Material correctMaterial;
    public Material incorrectMaterial;
    /// <summary>
    /// Se dispara cuando el alumno confirma una fibra.
    /// El int es el fiberNumber de la fibra seleccionada.
    /// </summary>
    public static event Action<int> OnFiberSelected;

    /// <summary>
    /// Se dispara cuando cambia la fibra bajo el cursor.
    /// </summary>
    public static event Action<int> OnFiberGazed;

    void Start()
    {
        // Propagar materiales compartidos a todas las fibras
        foreach (var fiber in fibers)
        {
            fiber.gazeMaterial = gazeMaterial;
            fiber.correctMaterial = correctMaterial;
            fiber.incorrectMaterial = incorrectMaterial;
        }
    }
    void Update()
    {
        if (!isInspecting) return;

        HandleNavigation();
        HandleSelection();
    }

    void HandleNavigation()
    {
        if (Time.time - lastInputTime < inputCooldown) return;

        float h = Input.GetAxis(navigateAxis);

        if (h > navigationThreshold)
        {
            Navigate(1);  // Derecha → siguiente fibra
            lastInputTime = Time.time;
        }
        else if (h < -navigationThreshold)
        {
            Navigate(-1); // Izquierda → fibra anterior
            lastInputTime = Time.time;
        }
    }

    void HandleSelection()
    {
        if (Input.GetButtonDown(selectButton))
        {
            if (selectedIndex >= 0 && selectedIndex < fibers.Count)
            {
                int fiberNumber = fibers[selectedIndex].fiberNumber;
                OnFiberSelected?.Invoke(fiberNumber);
                Debug.Log("[CableInspector] Fibra seleccionada: " +
                          fiberNumber + " (" + fibers[selectedIndex].colorName + ")");
            }
        }
    }

    void Navigate(int direction)
    {
        // Quitar gaze de la fibra actual
        if (selectedIndex >= 0 && selectedIndex < fibers.Count)
        {
            fibers[selectedIndex].SetState(FiberState.Normal);
        }

        // Mover índice
        selectedIndex += direction;

        // Wrap-around
        if (selectedIndex >= fibers.Count) selectedIndex = 0;
        if (selectedIndex < 0) selectedIndex = fibers.Count - 1;

        // Aplicar gaze a la nueva fibra
        fibers[selectedIndex].SetState(FiberState.Gaze);

        OnFiberGazed?.Invoke(fibers[selectedIndex].fiberNumber);
    }

    // ══════════════════════════════════════════════════════════
    // API PÚBLICA (llamada desde InstructionManager)
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Activa el modo de inspección. El joystick navega entre fibras.
    /// </summary>
    public void StartInspection()
    {
        isInspecting = true;
        selectedIndex = 0;
        fibers[0].SetState(FiberState.Gaze);
        OnFiberGazed?.Invoke(fibers[0].fiberNumber);
    }

    /// <summary>
    /// Desactiva el modo de inspección. Todas las fibras vuelven a normal.
    /// </summary>
    public void StopInspection()
    {
        isInspecting = false;
        ResetAllFibers();
        selectedIndex = -1;
    }

    /// <summary>
    /// Devuelve todas las fibras a su estado normal.
    /// </summary>
    public void ResetAllFibers()
    {
        foreach (var fiber in fibers)
        {
            fiber.SetState(FiberState.Normal);
        }
    }

    /// <summary>
    /// Ilumina una fibra específica (para Fase 1 y Fase 3).
    /// </summary>
    public void HighlightFiber(int fiberNumber)
    {
        foreach (var fiber in fibers)
        {
            if (fiber.fiberNumber == fiberNumber)
                fiber.SetState(FiberState.Highlighted);
            else
                fiber.SetState(FiberState.Normal);
        }
    }

    /// <summary>
    /// Ilumina todas las fibras simultáneamente (resumen de Fase 3).
    /// </summary>
    public void HighlightAllFibers()
    {
        foreach (var fiber in fibers)
        {
            fiber.SetState(FiberState.Highlighted);
        }
    }

    /// <summary>
    /// Marca la fibra seleccionada como correcta (flash verde).
    /// </summary>
    public void MarkCorrect(int fiberNumber)
    {
        foreach (var fiber in fibers)
        {
            if (fiber.fiberNumber == fiberNumber)
            {
                fiber.FlashCorrect();
                break;
            }
        }
    }

    /// <summary>
    /// Marca la fibra seleccionada como incorrecta (flash naranja).
    /// </summary>
    public void MarkIncorrect(int fiberNumber)
    {
        foreach (var fiber in fibers)
        {
            if (fiber.fiberNumber == fiberNumber)
            {
                fiber.FlashIncorrect();
                break;
            }
        }
    }

    /// <summary>
    /// Devuelve el P1_FiberInteractable de una fibra por su número.
    /// </summary>
    public P1_FiberInteractable GetFiber(int fiberNumber)
    {
        foreach (var fiber in fibers)
        {
            if (fiber.fiberNumber == fiberNumber)
                return fiber;
        }
        return null;
    }
}