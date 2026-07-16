using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Drag-to-look zone for Modo Táctil. Must be added (via AddComponent) to
/// a GameObject that already has a RectTransform + Image covering the
/// right half of the screen — see TouchControlsBootstrap. Draws nothing.
///
/// LookInput stays valid for one extra frame after the last OnDrag call,
/// not just the exact write-frame. This matters because Unity does not
/// guarantee Update() execution order between unrelated scripts: if a
/// consumer's Update() happens to always run before EventSystem's Update()
/// (which fires OnDrag) in every frame, an "exact frame match" scheme
/// would read stale/zero data forever. A one-frame grace window guarantees
/// the consumer's next read sees the value regardless of which order the
/// two Update() calls land in — worst case a bounded 1-frame lag, never a
/// permanent miss. OnEndDrag/OnPointerUp force an immediate reset so the
/// camera doesn't keep drifting after the finger stops moving or lifts.
/// </summary>
public class TouchLook : MonoBehaviour, IDragHandler, IEndDragHandler, IPointerUpHandler
{
    [Header("Feel")]
    public float pixelsPerAxisUnit = 15f;

    static Vector2 _raw;
    static int _lastDragFrame = -10;

    public static Vector2 LookInput =>
        (Time.frameCount - _lastDragFrame <= 1) ? _raw : Vector2.zero;

    public void OnDrag(PointerEventData eventData)
    {
        _raw = Vector2.ClampMagnitude(eventData.delta / pixelsPerAxisUnit, 1f);
        _lastDragFrame = Time.frameCount;
    }

    public void OnEndDrag(PointerEventData eventData) => Reset();
    public void OnPointerUp(PointerEventData eventData) => Reset();

    static void Reset()
    {
        _raw = Vector2.zero;
        _lastDragFrame = -10;
    }
}
