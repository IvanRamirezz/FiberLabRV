using UnityEngine;

public enum CablePartType { Vaina, Armadura, CintaMilar, Nucleo }

/// <summary>
/// Adjuntar a Vaina, Armadura, cada Cinta Milar y Nucleo.
/// Requiere que el GameObject (o uno de sus hijos) tenga un Collider para que
/// el raycast del Paso 2 pueda detectarlo.
/// </summary>
public class P1_CablePart : MonoBehaviour
{
    public CablePartType partType;
    public Highlightable  highlightable;

    public void SetHovered(bool value)
    {
        if (highlightable != null) highlightable.Highlight(value);
    }
}
