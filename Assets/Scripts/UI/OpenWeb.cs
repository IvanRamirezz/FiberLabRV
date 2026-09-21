using UnityEngine;

public class OpenWeb : MonoBehaviour
{
    public void OpenWebsite(string url)
    {
        if (!string.IsNullOrWhiteSpace(url))
        {
            Application.OpenURL(url);
        }
        else
        {
            UnityEngine.Debug.LogWarning("La URL está vacía.");
        }
    }
}