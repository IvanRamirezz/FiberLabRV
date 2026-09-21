using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

public class LoadPracticaScene : MonoBehaviour
{
    [Header("Escenas por práctica")]
    public string scenePractica1 = "RVPractica1";
    public string scenePractica2 = "RVPractica2";
    public string scenePractica3 = "RVPractica3";

    public void CargarPractica()
    {
        // practica_seleccionada: guardado por SeleccionarPractica (botón manual)
        // practica_id: guardado por CheckPractica (verificación automática)
        // practica_seleccionada tiene prioridad
        int practicaId = PlayerPrefs.GetInt("practica_seleccionada", 0);
        if (practicaId == 0)
            practicaId = PlayerPrefs.GetInt("practica_id", 0);

        Debug.Log($">>> CargarPractica: practica_id={practicaId}");

        if (practicaId == 0)
        {
            Debug.LogWarning("LoadPracticaScene: no hay práctica seleccionada.");
            return;
        }

        string scene = practicaId switch
        {
            1 => scenePractica1,
            2 => scenePractica2,
            3 => scenePractica3,
            _ => null
        };

        if (string.IsNullOrEmpty(scene))
        {
            Debug.LogWarning($"LoadPracticaScene: sin escena para practica_id={practicaId}");
            return;
        }

        SceneManager.LoadScene(scene);
    }
}