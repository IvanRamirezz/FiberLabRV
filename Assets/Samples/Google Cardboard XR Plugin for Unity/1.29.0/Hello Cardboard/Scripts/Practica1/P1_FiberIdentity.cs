using UnityEngine;

/// <summary>
/// Adjuntar a cada fibra individual dentro de una Cinta Milar.
/// bufferIndex  : índice del búfer (0 = Cinta Milar 1, 1 = Cinta Milar 2, …)
/// fiberPosition: posición 1-12 dentro del búfer según TIA/EIA-598-C
/// </summary>
public class P1_FiberIdentity : MonoBehaviour
{
    public int          bufferIndex;    // 0-based
    public int          fiberPosition;  // 1-based (1-12)
    public Highlightable highlightable;

    void Awake()
    {
        if (highlightable == null)
            highlightable = GetComponent<Highlightable>();
    }
}
