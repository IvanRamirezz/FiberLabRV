using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// On-screen "Start" button for Modo Táctil — the single dedicated tap
/// target for confirming dialog steps, selecting menu options, and
/// grabbing/dropping cables (see TouchInput.ButtonDown). Must be added
/// (via AddComponent) to a GameObject that already has a RectTransform +
/// Image sized/positioned as the visible circle — see
/// TouchControlsBootstrap. Adds its own text label as a child in Awake().
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class TouchActionButton : MonoBehaviour, IPointerDownHandler
{
    static int _pressFrame = -10;

    // True for one frame after a press, with a 1-frame grace window — because
    // Unity does not guarantee Update() order between EventSystem and
    // whichever script reads this next, and that order can differ between
    // the Editor and an IL2CPP device build. An exact single-frame match
    // trades a permanent miss risk for the double-fire this grace window
    // reintroduces — consumers must dedupe via PressId (see
    // TouchInput.ButtonDown) rather than narrowing this window.
    public static bool Pressed => Time.frameCount - _pressFrame <= 1;

    // Identifies which physical tap is currently visible through Pressed —
    // just the frame it was registered on. Consumers that must act exactly
    // once per tap (not once per frame the grace window still reads
    // Pressed==true) remember the last PressId they acted on and compare.
    public static int PressId => _pressFrame;

    // True only while this component is enabled in the current scene, so
    // TouchInput can fall back to physical input in scenes that never
    // bootstrap this button (e.g. the mode-selection menu scene).
    public static bool Exists { get; private set; }

    void Awake()
    {
        var labelGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelGO.transform.SetParent(transform, false);
        var label = labelGO.GetComponent<Text>();
        label.text = "Start";
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 28;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;
        var labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }

    void OnEnable()  => Exists = true;
    void OnDisable() => Exists = false;

    public void OnPointerDown(PointerEventData eventData) => _pressFrame = Time.frameCount;
}
