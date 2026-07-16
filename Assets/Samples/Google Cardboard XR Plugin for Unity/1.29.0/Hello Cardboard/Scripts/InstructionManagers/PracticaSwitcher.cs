using UnityEngine;

/// <summary>
/// Añadir al GameObject "Instrucciones" en PracticasRV.
/// Coordina Demo + InstructionManagers de práctica.
/// Todos los managers deben estar DESACTIVADOS en el Inspector.
/// </summary>
public class PracticaSwitcher : MonoBehaviour
{
    public static PracticaSwitcher Instance;

    [Header("Instruction Managers")]
    public P1_InstructionManager   instruccionesP1;
    public InstructionManagerPrac3 instruccionesP3;
    public InstructionManager_Demo instruccionesDemo;

    int _practica;

    void Awake()
    {
        Instance = this;

        _practica = PlayerPrefs.GetInt("practica_seleccionada", 0);
        if (_practica == 0)
            _practica = PlayerPrefs.GetInt("practica_id", 0);

        DisableAll();

        // El Demo maneja internamente la lógica de "ya completado / preguntar".
        // Si no hay Demo asignado, va directo a la práctica.
        if (instruccionesDemo != null)
            instruccionesDemo.enabled = true;
        else
            ActivarPractica();

        Debug.Log($"[PracticaSwitcher] practica={_practica}, demo={instruccionesDemo != null}");
    }

    void DisableAll()
    {
        if (instruccionesP1   != null) instruccionesP1.enabled   = false;
        if (instruccionesP3   != null) instruccionesP3.enabled   = false;
        if (instruccionesDemo != null) instruccionesDemo.enabled = false;
    }

    /// <summary>
    /// Llamado por InstructionManager_Demo cuando el tutorial termina o se salta.
    /// Activa el InstructionManager correspondiente a la práctica seleccionada.
    /// </summary>
    public void ActivarPractica()
    {
        if (instruccionesDemo != null) instruccionesDemo.enabled = false;

        switch (_practica)
        {
            case 1:
                if (instruccionesP1 != null) instruccionesP1.enabled = true;
                break;
            case 3:
                if (instruccionesP3 != null) instruccionesP3.enabled = true;
                break;
            default:
                Debug.LogWarning($"[PracticaSwitcher] práctica no reconocida: {_practica} — activa P1 por defecto.");
                if (instruccionesP1 != null) instruccionesP1.enabled = true;
                break;
        }

        Debug.Log($"[PracticaSwitcher] ActivarPractica: {_practica}");
    }
}
