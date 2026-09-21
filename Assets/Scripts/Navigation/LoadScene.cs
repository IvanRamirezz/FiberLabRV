using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScene : MonoBehaviour
{
    [Header("Popups (opcional)")]
    public GameObject[] popupsToClose;

    /// <summary>
    /// Carga cualquier escena por nombre.
    /// Asígnalo en el Inspector del botón: OnClick → LoadScene.Load → escribe el nombre.
    /// </summary>
    public void Load(string sceneName)
    {
        CloseAllPopups();
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Cierra todos los popups asignados sin cambiar de escena.
    /// </summary>
    public void QuitarPopups()
    {
        CloseAllPopups();
    }

    private void CloseAllPopups()
    {
        foreach (var popup in popupsToClose)
        {
            if (popup != null)
                popup.SetActive(false);
        }
    }
}
