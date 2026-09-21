using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

/// <summary>
/// Asignar a cada botón de práctica en Practicas-Profe.
/// En el Inspector del botón: OnClick → SeleccionarPractica.Seleccionar(número)
/// Ejemplo: Practica1 → Seleccionar(1), Practica2 → Seleccionar(2), etc.
/// </summary>
public class SeleccionarPractica : MonoBehaviour
{
    [Header("Escena destino")]
    public string sceneDestino = "InicioRV";

    /// <summary>
    /// Guarda el número de práctica seleccionada y navega a InicioRV.
    /// Llamar desde el OnClick del botón pasando 1, 2 o 3.
    /// </summary>
    public void Seleccionar(int numeroPractica)
    {
        if (numeroPractica <= 0)
        {
            Debug.LogWarning("SeleccionarPractica: número inválido → " + numeroPractica);
            return;
        }

        PlayerPrefs.SetInt("practica_seleccionada", numeroPractica);
        PlayerPrefs.Save();

        Debug.Log($">>> Práctica seleccionada: {numeroPractica} → {sceneDestino}");
        SceneManager.LoadScene(sceneDestino);
    }
}