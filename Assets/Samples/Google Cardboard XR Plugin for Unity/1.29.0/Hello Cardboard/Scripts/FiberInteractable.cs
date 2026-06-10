using UnityEngine;

public enum FiberState
{
    Normal,
    Highlighted,    // Fase 1: iluminación automática
    Gaze,           // Seleccionada con joystick, esperando confirmación
    Correct,        // Respuesta correcta (verde temporal)
    Incorrect       // Respuesta incorrecta (naranja temporal)
}

public class P1_FiberInteractable : MonoBehaviour
{
    [Header("Identificación")]
    public int fiberNumber;              // 1-12 (posición en el código de colores)
    public string colorName;             // "Azul", "Naranja", etc.

    [Header("Materiales")]
    public Material normalMaterial;      // Material de color original (Mat_Azul, etc.)
    public Material highlightMaterial;   // Material con emisión activada
    public Material gazeMaterial;        // Material claro/blanco al estar seleccionado
    public Material correctMaterial;     // Material verde
    public Material incorrectMaterial;   // Material naranja

    private MeshRenderer meshRenderer;
    private FiberState currentState;

    public FiberState CurrentState => currentState;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        currentState = FiberState.Normal;
    }

    public void SetState(FiberState newState)
    {
        currentState = newState;

        switch (newState)
        {
            case FiberState.Normal:
                meshRenderer.material = normalMaterial;
                break;
            case FiberState.Highlighted:
                meshRenderer.material = highlightMaterial;
                break;
            case FiberState.Gaze:
                meshRenderer.material = gazeMaterial;
                break;
            case FiberState.Correct:
                meshRenderer.material = correctMaterial;
                break;
            case FiberState.Incorrect:
                meshRenderer.material = incorrectMaterial;
                break;
        }
    }

    /// <summary>
    /// Muestra correcto temporalmente y luego vuelve a normal.
    /// </summary>
    public void FlashCorrect(float duration = 1f)
    {
        StartCoroutine(FlashState(FiberState.Correct, duration));
    }

    /// <summary>
    /// Muestra incorrecto temporalmente y luego vuelve a normal.
    /// </summary>
    public void FlashIncorrect(float duration = 0.5f)
    {
        StartCoroutine(FlashState(FiberState.Incorrect, duration));
    }

    System.Collections.IEnumerator FlashState(FiberState flashState, float duration)
    {
        SetState(flashState);
        yield return new WaitForSeconds(duration);
        SetState(FiberState.Normal);
    }
}