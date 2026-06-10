using UnityEngine;
using System;
using System.Collections.Generic;

public class P1_ComponentQuiz : MonoBehaviour
{
    [Header("Componentes del cable (asignar desde inspector)")]
    public List<P1_CableComponent> components;

    [Header("Input")]
    public string navigateAxis = "Horizontal";
    public string selectButton = "Fire1";
    public float inputCooldown = 0.25f;
    public float navigationThreshold = 0.5f;

    private int selectedIndex = -1;
    private bool isActive = false;
    private float lastInputTime;

    public static event Action<CableComponentType> OnComponentSelected;

    void Update()
    {
        if (!isActive) return;

        HandleNavigation();
        HandleSelection();
    }

    void HandleNavigation()
    {
        if (Time.time - lastInputTime < inputCooldown) return;

        float h = Input.GetAxis(navigateAxis);

        if (h > navigationThreshold)
        {
            Navigate(1);
            lastInputTime = Time.time;
        }
        else if (h < -navigationThreshold)
        {
            Navigate(-1);
            lastInputTime = Time.time;
        }
    }

    void HandleSelection()
    {
        if (Input.GetButtonDown(selectButton))
        {
            if (selectedIndex >= 0 && selectedIndex < components.Count)
            {
                OnComponentSelected?.Invoke(components[selectedIndex].componentType);
            }
        }
    }

    void Navigate(int direction)
    {
        if (selectedIndex >= 0 && selectedIndex < components.Count)
            components[selectedIndex].SetNormal();

        selectedIndex += direction;
        if (selectedIndex >= components.Count) selectedIndex = 0;
        if (selectedIndex < 0) selectedIndex = components.Count - 1;

        components[selectedIndex].SetHighlight();
    }

    public void StartQuiz()
    {
        isActive = true;
        selectedIndex = 0;
        components[0].SetHighlight();
    }

    public void StopQuiz()
    {
        isActive = false;
        ResetAll();
        selectedIndex = -1;
    }

    public void ResetAll()
    {
        foreach (var comp in components)
            comp.SetNormal();
    }

    /// <summary>
    /// Ilumina un componente específico (para Fase 1 automática).
    /// </summary>
    public void HighlightComponent(CableComponentType type)
    {
        foreach (var comp in components)
        {
            if (comp.componentType == type)
                comp.SetHighlight();
            else
                comp.SetNormal();
        }
    }

    public P1_CableComponent GetComponent(CableComponentType type)
    {
        foreach (var comp in components)
        {
            if (comp.componentType == type)
                return comp;
        }
        return null;
    }
}