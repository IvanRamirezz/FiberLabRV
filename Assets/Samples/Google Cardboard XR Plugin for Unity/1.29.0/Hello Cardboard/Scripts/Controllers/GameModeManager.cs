using UnityEngine;

/// <summary>
/// Persists the chosen game mode (VR or Normal) between scenes via PlayerPrefs.
/// No GameObject needed — purely static access.
/// </summary>
public static class GameModeManager
{
    const string Key = "GameMode";

    public static bool IsVRMode
    {
        get => PlayerPrefs.GetInt(Key, 0) == 1;
        set => PlayerPrefs.SetInt(Key, value ? 1 : 0);
    }
}
