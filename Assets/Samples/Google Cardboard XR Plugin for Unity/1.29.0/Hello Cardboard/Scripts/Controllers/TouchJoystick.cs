using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Fixed on-screen joystick for Modo Táctil. Must be added (via
/// AddComponent) to a GameObject that already has a RectTransform + Image
/// covering the touch zone — see TouchControlsBootstrap. Builds its own
/// background/knob visuals as children in Awake().
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class TouchJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Layout")]
    public float backgroundSize = 280f;
    public float knobSize = 130f;
    public Vector2 screenMargin = new Vector2(160f, 60f);

    [Header("Feel")]
    public float knobRange = 100f;

    public static Vector2 MoveInput { get; private set; }

    RectTransform _background;
    RectTransform _knob;

    void Awake()
    {
        var bgSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        var knobSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");

        var bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(transform, false);
        var bgImage = bgGO.GetComponent<Image>();
        bgImage.sprite = bgSprite;
        bgImage.color = new Color(1f, 1f, 1f, 0.35f);
        bgImage.raycastTarget = false;
        _background = bgGO.GetComponent<RectTransform>();
        _background.anchorMin = new Vector2(0f, 0f);
        _background.anchorMax = new Vector2(0f, 0f);
        _background.pivot = new Vector2(0.5f, 0.5f);
        _background.sizeDelta = new Vector2(backgroundSize, backgroundSize);
        _background.anchoredPosition = new Vector2(screenMargin.x + backgroundSize / 2f, screenMargin.y + backgroundSize / 2f);

        var knobGO = new GameObject("Knob", typeof(RectTransform), typeof(Image));
        knobGO.transform.SetParent(bgGO.transform, false);
        var knobImage = knobGO.GetComponent<Image>();
        knobImage.sprite = knobSprite;
        knobImage.color = new Color(1f, 1f, 1f, 0.7f);
        knobImage.raycastTarget = false;
        _knob = knobGO.GetComponent<RectTransform>();
        _knob.anchorMin = new Vector2(0.5f, 0.5f);
        _knob.anchorMax = new Vector2(0.5f, 0.5f);
        _knob.pivot = new Vector2(0.5f, 0.5f);
        _knob.sizeDelta = new Vector2(knobSize, knobSize);
        _knob.anchoredPosition = Vector2.zero;
    }

    public void OnPointerDown(PointerEventData eventData) => OnDrag(eventData);

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 backgroundScreenPos = RectTransformUtility.WorldToScreenPoint(eventData.pressEventCamera, _background.position);
        Vector2 offset = eventData.position - backgroundScreenPos;
        offset = Vector2.ClampMagnitude(offset, knobRange);
        _knob.anchoredPosition = offset;
        MoveInput = offset / knobRange;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _knob.anchoredPosition = Vector2.zero;
        MoveInput = Vector2.zero;
    }

    void OnDisable()
    {
        MoveInput = Vector2.zero;
    }
}
