using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to a GameObject in the 2D menu scene.
/// Wire the two UI Buttons to EnterVRMode() and EnterNormalMode() via OnClick.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Tooltip("Exact name of the game scene in Build Settings (used when vrSceneName/normalSceneName are empty)")]
    public string gameSceneName = "Practica3";

    [Tooltip("Scene to load when Realidad Virtual is chosen. Leave empty to use gameSceneName.")]
    public string vrSceneName = "";

    [Tooltip("Scene to load when Modo Táctil is chosen. Leave empty to use gameSceneName.")]
    public string normalSceneName = "";

    public void EnterVRMode()
    {
        GameModeManager.IsVRMode = true;
        SceneManager.LoadScene(string.IsNullOrEmpty(vrSceneName) ? gameSceneName : vrSceneName);
    }

    public void EnterNormalMode()
    {
        GameModeManager.IsVRMode = false;
        SceneManager.LoadScene(string.IsNullOrEmpty(normalSceneName) ? gameSceneName : normalSceneName);
    }
}
