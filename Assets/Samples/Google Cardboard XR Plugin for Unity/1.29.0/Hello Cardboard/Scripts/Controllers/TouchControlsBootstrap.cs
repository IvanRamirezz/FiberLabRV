using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to an empty GameObject in the game scene. Builds the on-screen
/// joystick (left half) and look-drag zone (right half) at runtime — no
/// prefab or manual UI setup needed. GameModeInitializer enables/disables
/// this GameObject based on GameModeManager.IsVRMode.
/// </summary>
public class TouchControlsBootstrap : MonoBehaviour
{
    void Awake()
    {
        var canvasGO = new GameObject("TouchControlsCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        var leftZoneGO = new GameObject("LeftJoystickZone", typeof(RectTransform), typeof(Image));
        leftZoneGO.transform.SetParent(canvasGO.transform, false);
        var leftRect = leftZoneGO.GetComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0f, 0f);
        leftRect.anchorMax = new Vector2(0.5f, 1f);
        leftRect.offsetMin = Vector2.zero;
        leftRect.offsetMax = Vector2.zero;
        var leftImage = leftZoneGO.GetComponent<Image>();
        leftImage.color = new Color(0f, 0f, 0f, 0f);
        leftZoneGO.AddComponent<TouchJoystick>();

        var rightZoneGO = new GameObject("RightLookZone", typeof(RectTransform), typeof(Image));
        rightZoneGO.transform.SetParent(canvasGO.transform, false);
        var rightRect = rightZoneGO.GetComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(0.5f, 0f);
        rightRect.anchorMax = new Vector2(1f, 1f);
        rightRect.offsetMin = Vector2.zero;
        rightRect.offsetMax = Vector2.zero;
        var rightImage = rightZoneGO.GetComponent<Image>();
        rightImage.color = new Color(0f, 0f, 0f, 0f);
        rightZoneGO.AddComponent<TouchLook>();

        // Dedicated button drawn on top of RightLookZone (later siblings
        // raycast first), bottom-right corner. A tap starting on the
        // button is captured by the button, not by RightLookZone below it.
        var actionBtnGO = new GameObject("TouchActionButton", typeof(RectTransform), typeof(Image));
        actionBtnGO.transform.SetParent(canvasGO.transform, false);
        var actionRect = actionBtnGO.GetComponent<RectTransform>();
        actionRect.anchorMin = new Vector2(1f, 0f);
        actionRect.anchorMax = new Vector2(1f, 0f);
        actionRect.pivot = new Vector2(1f, 0f);
        actionRect.sizeDelta = new Vector2(190f, 190f);
        actionRect.anchoredPosition = new Vector2(-40f, 40f);
        var actionImage = actionBtnGO.GetComponent<Image>();
        actionImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        actionImage.color = new Color(1f, 1f, 1f, 0.35f);
        actionBtnGO.AddComponent<TouchActionButton>();
    }
}
