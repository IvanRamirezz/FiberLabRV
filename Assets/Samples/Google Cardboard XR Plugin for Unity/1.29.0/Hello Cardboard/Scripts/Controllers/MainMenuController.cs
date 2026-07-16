using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to a GameObject in the 2D menu scene.
/// Wire the two UI Buttons to EnterVRMode() and EnterNormalMode() via OnClick.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Tooltip("Exact name of the game scene in Build Settings")]
    public string gameSceneName = "Practica3";

    public void EnterVRMode()
    {
        GameModeManager.IsVRMode = true;
        SceneManager.LoadScene(gameSceneName);
    }

    public void EnterNormalMode()
    {
        GameModeManager.IsVRMode = false;
        SceneManager.LoadScene(gameSceneName);
    }
}
