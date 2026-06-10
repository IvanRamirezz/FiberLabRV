using UnityEngine;
using System;
using System.Collections;

public enum CableComponentType
{
    Cubierta,      // Vaina
    FRP,           // Armadura / miembro de fuerza
    Buffer,        // Buffer / tubo
    Fibras         // Conjunto de fibras (no individual)
}

public class P1_CableComponent : MonoBehaviour
{
    [Header("Identificación")]
    public CableComponentType componentType;
    public string displayName;           // "Cubierta exterior", "Miembro de fuerza (FRP)", etc.
    public string description;           // "Protege contra golpes, humedad y rayos UV."

    [Header("Materiales")]
    public Material normalMaterial;
    public Material highlightMaterial;
    public Material correctMaterial;
    public Material incorrectMaterial;

    private MeshRenderer meshRenderer;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    public void SetNormal()
    {
        meshRenderer.material = normalMaterial;
    }

    public void SetHighlight()
    {
        meshRenderer.material = highlightMaterial;
    }

    public void FlashCorrect(float duration = 1f)
    {
        StartCoroutine(FlashRoutine(correctMaterial, duration));
    }

    public void FlashIncorrect(float duration = 0.5f)
    {
        StartCoroutine(FlashRoutine(incorrectMaterial, duration));
    }

    IEnumerator FlashRoutine(Material flashMat, float duration)
    {
        meshRenderer.material = flashMat;
        yield return new WaitForSeconds(duration);
        meshRenderer.material = normalMaterial;
    }
}