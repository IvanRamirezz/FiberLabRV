# On-Screen Touch Controls for Modo Normal — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an on-screen joystick + look-drag zone + action button for Modo Normal (Modo Táctil), and route every `"Fire1"` read in the project through them, without touching the VR/Normal mode-selection system that already exists in this repo.

**Architecture:** Five new MonoBehaviours build a runtime-constructed touch UI (no prefabs): `TouchJoystick` (movement), `TouchLook` (camera drag), `TouchActionButton` (dedicated confirm/grab tap target), `TouchControlsBootstrap` (assembles the Canvas + the three above at `Awake()`), and `TouchInput` (static helper — the single place that decides whether a `"Fire1"` read comes from the touch button or physical input). Every existing `Input.GetButtonDown("Fire1")` / `Input.GetMouseButtonDown(0)` site is replaced with a call through `TouchInput`. `GameModeInitializer` (already in the project) gets one new field to toggle the touch canvas on/off based on `GameModeManager.IsVRMode`, which also already exists.

**Tech Stack:** Unity 6 (6000.0.42f1), Legacy Input Manager only (no `UnityEngine.InputSystem`), uGUI (`UnityEngine.UI`), C#.

## Global Constraints

- Legacy Input Manager only — never add `using UnityEngine.InputSystem;` anywhere in this plan.
- Do not modify `GameModeManager.cs`, `MainMenuController.cs` — they already implement VR/Normal mode selection and are out of scope for this feature; touching them risks breaking already-wired Inspector references and scene routing (`MainMenuController.gameSceneName` loads `"Practica3"` for both modes today — a different shape than a hypothetical "separate VR/Normal scene" version; do not "fix" this, it is unrelated to this plan).
- There is no CLI build/test command for this project (Unity project, no automated test suite — confirmed in `CLAUDE.md`). Every "verify" step in this plan means: save the file, switch to the Unity Editor, let it recompile, and confirm the Console has no errors. The final task is a manual Play Mode pass covering both modes.
- Every new/modified script goes in `Assets/Samples/Google Cardboard XR Plugin for Unity/1.29.0/Hello Cardboard/Scripts/Controllers/` (new touch scripts) or the existing file's current location (edits) — do not relocate existing files.
- Confirmed via scene inspection of `PracticasRV.unity`: `MotionObjectController` lives on GameObject `RealPlayer (Movimiento)`; `CameraLookController` lives on `Player (Camara)`; `GameModeInitializer` and `MenuNavigationController` both live on GameObject `GameManager`. `HandInteraction.BotonAgarrar` is `"Submit"` everywhere (not overridden to `"Fire1"` in any scene) — it is correctly out of scope; do not route it through `TouchInput`.

---

## File Structure

| File | Action | Responsibility |
|---|---|---|
| `.../Controllers/TouchActionButton.cs` | Create | Dedicated on-screen tap target; exposes `static bool Pressed`, `static int PressId`, `static bool Exists` |
| `.../Controllers/TouchInput.cs` | Create | Static gate: `"Fire1"` reads go through the touch button when in Normal mode and the button exists; everything else falls through to `Input` |
| `.../Controllers/TouchJoystick.cs` | Create | Fixed on-screen joystick; exposes `static Vector2 MoveInput` |
| `.../Controllers/TouchLook.cs` | Create | Invisible drag-to-look zone; exposes `static Vector2 LookInput` |
| `.../Controllers/TouchControlsBootstrap.cs` | Create | Builds the Canvas + joystick zone + look zone + action button at `Awake()` |
| `.../Controllers/GameModeInitializer.cs` | Modify | Add `touchControlsRoot` field, toggle it in `Start()` |
| `.../Controllers/MotionObjectController.cs` | Modify | Sum `TouchJoystick.MoveInput` into existing `Input.GetAxis` reads |
| `.../Controllers/CameraLookController.cs` | Modify | Sum `TouchLook.LookInput` into existing `GetAxisSafe` reads |
| `.../Controllers/MenuNavigationController.cs` | Modify | Route `HandleSubmit()` through `TouchInput.ButtonDown` |
| `.../Practica3/ConnectionMenuUI.cs` | Modify | Route `HandleButtons()`'s submit read through `TouchInput.ButtonDown` |
| `.../HandsFocusMode/FocusUIInputController.cs` | Modify | Route `HandleSubmitCancel()`'s submit read through `TouchInput.ButtonDown` |
| `.../InstructionManagers/InstructionManager_Demo.cs` | Modify | Route all 4 `Fire1`/mouse sites through `TouchInput.ButtonDown` |
| `.../Practica1/P1_Instrucciones.cs` | Modify | Route all 3 `Fire1`/mouse sites through `TouchInput.ButtonDown` |

---

### Task 1: `TouchActionButton.cs`

**Files:**
- Create: `Assets/Samples/Google Cardboard XR Plugin for Unity/1.29.0/Hello Cardboard/Scripts/Controllers/TouchActionButton.cs`

**Interfaces:**
- Produces: `TouchActionButton.Pressed` (`static bool`), `TouchActionButton.PressId` (`static int`), `TouchActionButton.Exists` (`static bool`)

- [ ] **Step 1: Write the file**

```csharp
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
```

- [ ] **Step 2: Verify it compiles**

Switch to the Unity Editor, wait for recompile, confirm the Console has no errors mentioning `TouchActionButton`.

- [ ] **Step 3: Commit**

```bash
git add "Assets/Samples/Google Cardboard XR Plugin for Unity/1.29.0/Hello Cardboard/Scripts/Controllers/TouchActionButton.cs"
git commit -m "feat: add TouchActionButton for on-screen confirm tap target"
```

---

### Task 2: `TouchInput.cs`

**Files:**
- Create: `Assets/Samples/Google Cardboard XR Plugin for Unity/1.29.0/Hello Cardboard/Scripts/Controllers/TouchInput.cs`

**Interfaces:**
- Consumes: `GameModeManager.IsVRMode` (`static bool`, already exists), `TouchActionButton.Exists`/`Pressed`/`PressId` (Task 1)
- Produces: `TouchInput.ButtonDown(string buttonName, ref int lastTouchPressId)` — returns `bool`

- [ ] **Step 1: Write the file**

```csharp
using UnityEngine;

/// <summary>
/// Centralizes touch-button substitution for every input site that reads
/// "Fire1" (confirm/select/grab). TouchJoystick/TouchLook cover the full
/// screen, and Unity simulates mouse button 0 with any touch, so without
/// this substitution every Fire1 site would also fire from taps meant for
/// movement/look.
/// </summary>
public static class TouchInput
{
    // true only when the action button exists AND is enabled in the
    // current scene (game scene in Normal mode). Scenes that never
    // bootstrap it (e.g. the mode-selection menu) always fall back to
    // physical input.
    static bool TouchActionAvailable => !GameModeManager.IsVRMode && TouchActionButton.Exists;

    // Every "Fire1" site in this project (WaitForConfirm, submitButton
    // fields, HandInteraction.BotonAgarrar) replaces physical input with
    // the touch button when available — OR-ing them back in would
    // reintroduce the same false touch, since Fire1 includes mouse 0.
    //
    // lastTouchPressId is a small piece of state the CALLER owns (one
    // field per call site, initialized to -10) and passes back in every
    // time. TouchActionButton.Pressed stays true for a 1-frame grace
    // window so no tap is ever silently missed regardless of Update()
    // order, but that means a caller who just checks the bool would see
    // "true" on two consecutive frames for one physical tap. Comparing
    // against PressId here makes each call site react exactly once per
    // tap, independent of every other call site.
    public static bool ButtonDown(string buttonName, ref int lastTouchPressId)
    {
        if (buttonName != "Fire1")
            return Input.GetButtonDown(buttonName);

        if (TouchActionAvailable)
        {
            if (!TouchActionButton.Pressed || TouchActionButton.PressId == lastTouchPressId)
                return false;
            lastTouchPressId = TouchActionButton.PressId;
            return true;
        }

        return Input.GetButtonDown("Fire1") || Input.GetMouseButtonDown(0);
    }
}
```

- [ ] **Step 2: Verify it compiles**

Switch to the Unity Editor, wait for recompile, confirm the Console has no errors mentioning `TouchInput`.

- [ ] **Step 3: Commit**

```bash
git add "Assets/Samples/Google Cardboard XR Plugin for Unity/1.29.0/Hello Cardboard/Scripts/Controllers/TouchInput.cs"
git commit -m "feat: add TouchInput gate for Fire1 touch-button substitution"
```

---

### Task 3: `TouchJoystick.cs`

**Files:**
- Create: `Assets/Samples/Google Cardboard XR Plugin for Unity/1.29.0/Hello Cardboard/Scripts/Controllers/TouchJoystick.cs`

**Interfaces:**
- Produces: `TouchJoystick.MoveInput` (`static Vector2`)

- [ ] **Step 1: Write the file**

```csharp
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
}
```

- [ ] **Step 2: Verify it compiles**

Switch to the Unity Editor, wait for recompile, confirm the Console has no errors mentioning `TouchJoystick`.

- [ ] **Step 3: Commit**

```bash
git add "Assets/Samples/Google Cardboard XR Plugin for Unity/1.29.0/Hello Cardboard/Scripts/Controllers/TouchJoystick.cs"
git commit -m "feat: add TouchJoystick for on-screen movement input"
```

---

### Task 4: `TouchLook.cs`

**Files:**
- Create: `Assets/Samples/Google Cardboard XR Plugin for Unity/1.29.0/Hello Cardboard/Scripts/Controllers/TouchLook.cs`

**Interfaces:**
- Produces: `TouchLook.LookInput` (`static Vector2`)

- [ ] **Step 1: Write the file**

```csharp
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
```

- [ ] **Step 2: Verify it compiles**

Switch to the Unity Editor, wait for recompile, confirm the Console has no errors mentioning `TouchLook`.

- [ ] **Step 3: Commit**

```bash
git add "Assets/Samples/Google Cardboard XR Plugin for Unity/1.29.0/Hello Cardboard/Scripts/Controllers/TouchLook.cs"
git commit -m "feat: add TouchLook for on-screen camera-drag input"
```

---

### Task 5: `TouchControlsBootstrap.cs`

**Files:**
- Create: `Assets/Samples/Google Cardboard XR Plugin for Unity/1.29.0/Hello Cardboard/Scripts/Controllers/TouchControlsBootstrap.cs`

**Interfaces:**
- Consumes: `TouchJoystick` (Task 3), `TouchLook` (Task 4), `TouchActionButton` (Task 1)

- [ ] **Step 1: Write the file**

```csharp
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
```

- [ ] **Step 2: Verify it compiles**

Switch to the Unity Editor, wait for recompile, confirm the Console has no errors mentioning `TouchControlsBootstrap`.

- [ ] **Step 3: Commit**

```bash
git add "Assets/Samples/Google Cardboard XR Plugin for Unity/1.29.0/Hello Cardboard/Scripts/Controllers/TouchControlsBootstrap.cs"
git commit -m "feat: add TouchControlsBootstrap to assemble touch UI at runtime"
```

---

### Task 6: Wire touch canvas into `GameModeInitializer.cs`

**Files:**
- Modify: `Assets/Samples/Google Cardboard XR Plugin for Unity/1.29.0/Hello Cardboard/Scripts/Controllers/GameModeInitializer.cs`

**Interfaces:**
- Consumes: `TouchControlsBootstrap` (Task 5, referenced only via the GameObject it sits on — no direct API call)

The current file (already in the repo) is:

```csharp
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

/// <summary>
/// Place on any persistent GameObject in the game scene (e.g. Player or GameManager).
/// Reads the chosen mode on startup and configures XR + camera control accordingly.
/// </summary>
public class GameModeInitializer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The CameraLookController on Player (Camara)")]
    public CameraLookController cameraLookController;

    void Start()
    {
        bool vr = GameModeManager.IsVRMode;

        // Joystick camera only in Normal mode
        if (cameraLookController != null)
            cameraLookController.enabled = !vr;

        if (vr)
            EnableXR();
        else
            DisableXR();
    }

    static void EnableXR()
    {
        var manager = XRGeneralSettings.Instance?.Manager;
        if (manager == null) return;

        if (!manager.isInitializationComplete)
            manager.InitializeLoaderSync();

        manager.StartSubsystems();
    }

    static void DisableXR()
    {
        var manager = XRGeneralSettings.Instance?.Manager;
        if (manager == null) return;

        manager.StopSubsystems();
        manager.DeinitializeLoader();
    }
}
```

- [ ] **Step 1: Add the `touchControlsRoot` field and toggle it in `Start()`**

```csharp
    [Header("References")]
    [Tooltip("The CameraLookController on Player (Camara)")]
    public CameraLookController cameraLookController;

    [Tooltip("Root GameObject holding TouchControlsBootstrap (on-screen joystick + look zone). Active only in Normal mode.")]
    public GameObject touchControlsRoot;

    void Start()
    {
        bool vr = GameModeManager.IsVRMode;

        // Joystick camera only in Normal mode
        if (cameraLookController != null)
            cameraLookController.enabled = !vr;

        // On-screen touch controls only in Normal mode
        if (touchControlsRoot != null)
            touchControlsRoot.SetActive(!vr);

        if (vr)
            EnableXR();
        else
            DisableXR();
    }
```

(`cameraLookController`'s existing serialized Inspector reference is untouched — this only adds one new field and one new `if` block.)

- [ ] **Step 2: Verify it compiles**

Switch to the Unity Editor, wait for recompile, confirm the Console has no errors mentioning `GameModeInitializer`. Confirm in the Inspector (select `GameManager` in `PracticasRV.unity`) that the existing `Camera Look Controller` reference is still assigned (proves the field rename risk didn't apply here — we only added a field, we didn't rename one).

- [ ] **Step 3: Commit**

```bash
git add "Assets/Samples/Google Cardboard XR Plugin for Unity/1.29.0/Hello Cardboard/Scripts/Controllers/GameModeInitializer.cs"
git commit -m "feat: toggle touch-controls canvas based on GameModeManager.IsVRMode"
```

---

### Task 7: Wire `TouchJoystick.MoveInput` into `MotionObjectController.cs`

**Files:**
- Modify: `Assets/Samples/Google Cardboard XR Plugin for Unity/1.29.0/Hello Cardboard/Scripts/Controllers/MotionObjectController.cs:46-47`

**Interfaces:**
- Consumes: `TouchJoystick.MoveInput` (Task 3)

- [ ] **Step 1: Replace the two movement-axis lines**

Current (`MotionObjectController.cs:46-47`):

```csharp
        // Joystick input for movement
        float x = Input.GetAxis("Horizontal"); // Sideways movement
        float z = Input.GetAxis("Vertical");   // Forward/backward movement
```

New:

```csharp
        // Joystick input for movement
        float x = Input.GetAxis("Horizontal") + TouchJoystick.MoveInput.x; // Sideways movement
        float z = Input.GetAxis("Vertical")   + TouchJoystick.MoveInput.y; // Forward/backward movement
```

Nothing else in this file changes — gravity, jump, cable-tension clamp, and the `CharacterController.Move()` calls stay exactly as they are.

- [ ] **Step 2: Verify it compiles**

Switch to the Unity Editor, wait for recompile, confirm the Console has no errors mentioning `MotionObjectController`.

- [ ] **Step 3: Commit**

```bash
git add "Assets/Samples/Google Cardboard XR Plugin for Unity/1.29.0/Hello Cardboard/Scripts/Controllers/MotionObjectController.cs"
git commit -m "feat: add on-screen joystick input to player movement"
```

---

### Task 8: Wire `TouchLook.LookInput` into `CameraLookController.cs`

**Files:**
- Modify: `Assets/Samples/Google Cardboard XR Plugin for Unity/1.29.0/Hello Cardboard/Scripts/Controllers/CameraLookController.cs:36-37`

**Interfaces:**
- Consumes: `TouchLook.LookInput` (Task 4)

- [ ] **Step 1: Replace the two axis-read lines in `Update()`**

Current (`CameraLookController.cs:36-37`):

```csharp
        float h = GetAxisSafe(lookHorizontalAxis);
        float v = GetAxisSafe(lookVerticalAxis);
```

New:

```csharp
        float h = GetAxisSafe(lookHorizontalAxis) + TouchLook.LookInput.x;
        float v = GetAxisSafe(lookVerticalAxis)   + TouchLook.LookInput.y;
```

Nothing else in this file changes — `invertY`, pitch clamping, and `GetAxisSafe`'s try/catch stay exactly as they are. (This is also the component `GameModeInitializer` already disables in VR mode, so `TouchLook.LookInput` never gets summed in while `cameraLookController.enabled == false`.)

- [ ] **Step 2: Verify it compiles**

Switch to the Unity Editor, wait for recompile, confirm the Console has no errors mentioning `CameraLookController`.

- [ ] **Step 3: Commit**

```bash
git add "Assets/Samples/Google Cardboard XR Plugin for Unity/1.29.0/Hello Cardboard/Scripts/Controllers/CameraLookController.cs"
git commit -m "feat: add on-screen look-drag input to camera rotation"
```

---

### Task 9: Route `MenuNavigationController.HandleSubmit()` through `TouchInput`

**Files:**
- Modify: `Assets/Samples/Google Cardboard XR Plugin for Unity/1.29.0/Hello Cardboard/Scripts/Controllers/MenuNavigationController.cs`

**Interfaces:**
- Consumes: `TouchInput.ButtonDown(string, ref int)` (Task 2)

- [ ] **Step 1: Add a per-instance dedup field**

Add next to the existing `float _lastInputTime;` field (`MenuNavigationController.cs:30`):

```csharp
    float _lastInputTime;
    int _lastTouchPressId = -10;
```

- [ ] **Step 2: Replace the submit read in `HandleSubmit()`**

Current (`MenuNavigationController.cs:78-87`):

```csharp
    void HandleSubmit()
    {
        if (!Input.GetButtonDown(submitButton)) return;

        ExecuteEvents.Execute(
            EventSystem.current.currentSelectedGameObject,
            new BaseEventData(EventSystem.current),
            ExecuteEvents.submitHandler
        );
    }
```

New:

```csharp
    void HandleSubmit()
    {
        if (!TouchInput.ButtonDown(submitButton, ref _lastTouchPressId)) return;

        ExecuteEvents.Execute(
            EventSystem.current.currentSelectedGameObject,
            new BaseEventData(EventSystem.current),
            ExecuteEvents.submitHandler
        );
    }
```

(`HandleNavigation()`, `ShouldBeActive()`, `EnsureSomethingSelected()` are unchanged. `submitButton` stays a public string field default `"Fire1"`; `TouchInput.ButtonDown` only substitutes when `buttonName == "Fire1"`, so this is safe even if a scene ever rebinds it to something else.)

- [ ] **Step 3: Verify it compiles**

Switch to the Unity Editor, wait for recompile, confirm the Console has no errors mentioning `MenuNavigationController`.

- [ ] **Step 4: Commit**

```bash
git add "Assets/Samples/Google Cardboard XR Plugin for Unity/1.29.0/Hello Cardboard/Scripts/Controllers/MenuNavigationController.cs"
git commit -m "feat: route menu confirm through TouchInput"
```

---

### Task 10: Route `ConnectionMenuUI.HandleButtons()` through `TouchInput`

**Files:**
- Modify: `Assets/Samples/Google Cardboard XR Plugin for Unity/1.29.0/Hello Cardboard/Scripts/Practica3/ConnectionMenuUI.cs`

**Interfaces:**
- Consumes: `TouchInput.ButtonDown(string, ref int)` (Task 2)

- [ ] **Step 1: Add a per-instance dedup field**

Add next to the existing `float lastInputTime;` field (`ConnectionMenuUI.cs:35`):

```csharp
    float lastInputTime;
    int lastTouchPressId = -10;
```

- [ ] **Step 2: Replace the submit read in `HandleButtons()`**

Current (`ConnectionMenuUI.cs:82-96`):

```csharp
    void HandleButtons()
    {
        if (Input.GetButtonDown(submitButton))
        {
            if (selectedIndex >= 0 && selectedIndex < currentButtons.Count)
            {
                currentButtons[selectedIndex].onClick.Invoke();
            }
        }

        if (Input.GetButtonDown(cancelButton))
        {
            HandleCancel();
        }
    }
```

New:

```csharp
    void HandleButtons()
    {
        if (TouchInput.ButtonDown(submitButton, ref lastTouchPressId))
        {
            if (selectedIndex >= 0 && selectedIndex < currentButtons.Count)
            {
                currentButtons[selectedIndex].onClick.Invoke();
            }
        }

        if (Input.GetButtonDown(cancelButton))
        {
            HandleCancel();
        }
    }
```

(`cancelButton` defaults to `"Fire2"`, which has no touch-button equivalent by design — the on-screen layer only ever substitutes `"Fire1"` — so it correctly stays a direct `Input.GetButtonDown` read.)

- [ ] **Step 3: Verify it compiles**

Switch to the Unity Editor, wait for recompile, confirm the Console has no errors mentioning `ConnectionMenuUI`.

- [ ] **Step 4: Commit**

```bash
git add "Assets/Samples/Google Cardboard XR Plugin for Unity/1.29.0/Hello Cardboard/Scripts/Practica3/ConnectionMenuUI.cs"
git commit -m "feat: route connection-menu submit through TouchInput"
```

---

### Task 11: Route `FocusUIInputController.HandleSubmitCancel()` through `TouchInput`

**Files:**
- Modify: `Assets/Samples/Google Cardboard XR Plugin for Unity/1.29.0/Hello Cardboard/Scripts/HandsFocusMode/FocusUIInputController.cs`

**Interfaces:**
- Consumes: `TouchInput.ButtonDown(string, ref int)` (Task 2)

- [ ] **Step 1: Add a per-instance dedup field**

Add next to the existing `float lastInputTime;` field (`FocusUIInputController.cs:16`):

```csharp
    float lastInputTime;
    int lastTouchPressId = -10;
```

- [ ] **Step 2: Replace the submit read in `HandleSubmitCancel()`**

Current (`FocusUIInputController.cs:45-60`):

```csharp
    void HandleSubmitCancel()
    {
        if (Input.GetButtonDown(submitButton))
        {
            ExecuteEvents.Execute(
                EventSystem.current.currentSelectedGameObject,
                new BaseEventData(EventSystem.current),
                ExecuteEvents.submitHandler
            );
        }

        if (Input.GetButtonDown(cancelButton))
        {
            FocusModeManager.Instance.Exit();
        }
    }
```

New:

```csharp
    void HandleSubmitCancel()
    {
        if (TouchInput.ButtonDown(submitButton, ref lastTouchPressId))
        {
            ExecuteEvents.Execute(
                EventSystem.current.currentSelectedGameObject,
                new BaseEventData(EventSystem.current),
                ExecuteEvents.submitHandler
            );
        }

        if (Input.GetButtonDown(cancelButton))
        {
            FocusModeManager.Instance.Exit();
        }
    }
```

- [ ] **Step 3: Verify it compiles**

Switch to the Unity Editor, wait for recompile, confirm the Console has no errors mentioning `FocusUIInputController`.

- [ ] **Step 4: Commit**

```bash
git add "Assets/Samples/Google Cardboard XR Plugin for Unity/1.29.0/Hello Cardboard/Scripts/HandsFocusMode/FocusUIInputController.cs"
git commit -m "feat: route focus-mode submit through TouchInput"
```

---

### Task 12: Route all 4 `Fire1` sites in `InstructionManager_Demo.cs` through `TouchInput`

**Files:**
- Modify: `Assets/Samples/Google Cardboard XR Plugin for Unity/1.29.0/Hello Cardboard/Scripts/InstructionManagers/InstructionManager_Demo.cs`

**Interfaces:**
- Consumes: `TouchInput.ButtonDown(string, ref int)` (Task 2)

- [ ] **Step 1: Add a per-instance dedup field**

This class runs one coroutine-driven state machine at a time (per the project's `InstructionManager` pattern — only one `WaitFor*`/polling step is ever active), so a single shared field covers all 4 sites. Add it near the top of the class, right after the `public static class... Instance`-less class declaration's existing fields (after `public TextMeshProUGUI instructionText;`):

```csharp
    [Header("UI — Instrucciones")]
    public TextMeshProUGUI instructionText;

    int _lastTouchPressId = -10;
```

- [ ] **Step 2: Site 1 — replay countdown poll (line ~100)**

Current:

```csharp
            if (Input.GetButtonDown("Fire1") || Input.GetMouseButtonDown(0))
                presionado = true;
```

New:

```csharp
            if (TouchInput.ButtonDown("Fire1", ref _lastTouchPressId))
                presionado = true;
```

- [ ] **Step 3: Site 2 — gaze-and-select poll (line ~209)**

Current:

```csharp
            if (apuntando && (Input.GetButtonDown("Fire1") || Input.GetMouseButtonDown(0)))
                seleccionado = true;
```

New:

```csharp
            if (apuntando && TouchInput.ButtonDown("Fire1", ref _lastTouchPressId))
                seleccionado = true;
```

- [ ] **Step 4: Site 3 — shared `WaitForConfirm()` (line ~408-413)**

Current:

```csharp
    IEnumerator WaitForConfirm()
    {
        yield return null;
        yield return new WaitUntil(() =>
            Input.GetButtonDown("Fire1") || Input.GetMouseButtonDown(0));
    }
```

New:

```csharp
    IEnumerator WaitForConfirm()
    {
        yield return null;
        yield return new WaitUntil(() =>
            TouchInput.ButtonDown("Fire1", ref _lastTouchPressId));
    }
```

- [ ] **Step 5: Site 4 — button-hover selection (line ~454-456)**

Current:

```csharp
            if (hoveredIdx >= 0 &&
                (Input.GetButtonDown("Fire1") || Input.GetMouseButtonDown(0)))
```

New:

```csharp
            if (hoveredIdx >= 0 &&
                TouchInput.ButtonDown("Fire1", ref _lastTouchPressId))
```

- [ ] **Step 6: Verify it compiles**

Switch to the Unity Editor, wait for recompile, confirm the Console has no errors mentioning `InstructionManager_Demo`.

- [ ] **Step 7: Commit**

```bash
git add "Assets/Samples/Google Cardboard XR Plugin for Unity/1.29.0/Hello Cardboard/Scripts/InstructionManagers/InstructionManager_Demo.cs"
git commit -m "feat: route demo tutorial confirm inputs through TouchInput"
```

---

### Task 13: Route all 3 `Fire1` sites in `P1_Instrucciones.cs` through `TouchInput`

**Files:**
- Modify: `Assets/Samples/Google Cardboard XR Plugin for Unity/1.29.0/Hello Cardboard/Scripts/Practica1/P1_Instrucciones.cs`

**Interfaces:**
- Consumes: `TouchInput.ButtonDown(string, ref int)` (Task 2)

- [ ] **Step 1: Add a per-instance dedup field**

Same reasoning as Task 12 — one coroutine-driven state machine, one shared field. Add it right after `public static P1_InstructionManager Instance;`:

```csharp
public class P1_InstructionManager : MonoBehaviour
{
    public static P1_InstructionManager Instance;

    int _lastTouchPressId = -10;
```

- [ ] **Step 2: Site 1 — cable-part hover selection (line ~123)**

Current:

```csharp
        if (hovered != null && Input.GetButtonDown("Fire1"))
```

New:

```csharp
        if (hovered != null && TouchInput.ButtonDown("Fire1", ref _lastTouchPressId))
```

- [ ] **Step 3: Site 2 — `WaitForConfirm()` (line ~157-161)**

Current:

```csharp
    IEnumerator WaitForConfirm()
    {
        yield return new WaitUntil(() =>
            Input.GetButtonDown("Fire1") || Input.GetMouseButtonDown(0));
        // Skip one frame so GetButtonDown resets before the next WaitForConfirm check
        yield return null;
    }
```

New:

```csharp
    IEnumerator WaitForConfirm()
    {
        yield return new WaitUntil(() =>
            TouchInput.ButtonDown("Fire1", ref _lastTouchPressId));
        // Skip one frame so GetButtonDown resets before the next WaitForConfirm check
        yield return null;
    }
```

- [ ] **Step 4: Site 3 — button-hover selection (line ~779-780)**

Current:

```csharp
            if (hovered >= 0 && (Input.GetButtonDown("Fire1") || Input.GetMouseButtonDown(0)))
```

New:

```csharp
            if (hovered >= 0 && TouchInput.ButtonDown("Fire1", ref _lastTouchPressId))
```

- [ ] **Step 5: Verify it compiles**

Switch to the Unity Editor, wait for recompile, confirm the Console has no errors mentioning `P1_InstructionManager`.

- [ ] **Step 6: Commit**

```bash
git add "Assets/Samples/Google Cardboard XR Plugin for Unity/1.29.0/Hello Cardboard/Scripts/Practica1/P1_Instrucciones.cs"
git commit -m "feat: route P1 confirm inputs through TouchInput"
```

---

### Task 14: Editor wiring in `PracticasRV.unity` (manual, no code)

**Files:** none (Unity scene edits only — there is no CLI for this)

- [ ] **Step 1: Add the touch-controls GameObject**

Open `PracticasRV.unity`. Under the `GameManager` GameObject (confirmed above to already hold `GameModeInitializer` and `MenuNavigationController`), create a new empty child GameObject named `TouchControlsRoot`. Add the `Touch Controls Bootstrap` component to it. Leave it **active** in the scene — `GameModeInitializer.Start()` sets its active state at runtime, but Unity needs it enabled so `Awake()` (which builds the canvas) actually runs before `GameModeInitializer.Start()` disables it in VR mode.

- [ ] **Step 2: Wire `GameModeInitializer`**

Select `GameManager`. In the `Game Mode Initializer` component, drag `TouchControlsRoot` into the new `Touch Controls Root` field. Confirm `Camera Look Controller` is still assigned to the `CameraLookController` on `Player (Camara)` (it should already be — this task only adds a field, it doesn't touch that reference).

- [ ] **Step 3: Save the scene**

`File > Save` (or Ctrl/Cmd+S) on `PracticasRV.unity`.

- [ ] **Step 4: Commit the scene change**

```bash
git add "Assets/Samples/Google Cardboard XR Plugin for Unity/1.29.0/Hello Cardboard/Scenes/PracticasRV.unity"
git commit -m "chore: wire TouchControlsRoot into GameModeInitializer in PracticasRV scene"
```

---

### Task 15: Manual Play Mode validation (both modes)

**Files:** none — validation only, per `CLAUDE.md` ("no automated test suites, validation is manual through Unity Play Mode")

- [ ] **Step 1: Normal mode — controls appear and work**

From `MenuPrincipal`, choose Modo Normal/Táctil, enter `PracticasRV`. Confirm: single camera (no stereo split), joystick visible bottom-left, action button visible bottom-right, no XR subsystems started (no double-camera rendering).

- [ ] **Step 2: Normal mode — joystick moves the player**

Drag the left-side joystick in each of the 4 directions; confirm `RealPlayer (Movimiento)` moves correspondingly and gravity/jump (`Input.GetButtonDown("Jump")`, unchanged) still works from a physical keyboard/gamepad if one is connected.

- [ ] **Step 3: Normal mode — right-side drag rotates the camera**

Drag anywhere on the right half of the screen; confirm `Player (Camara)` yaws/pitches, clamped to `pitchMin`/`pitchMax`, and stops immediately on release (no drift).

- [ ] **Step 4: Normal mode — action button confirms without false triggers**

Step through the Demo tutorial's dialog prompts and P1's cable-part selection using only the action button. Confirm dragging the joystick or the look zone does **not** advance any dialog or selection, and tapping the action button does not also move the joystick knob or rotate the camera (it must sit on top and capture the tap first).

- [ ] **Step 5: Normal mode — cable connection flow**

Reach the point in P3 where a cable is grabbed and dropped onto a socket (`HandInteraction.BotonAgarrar`, bound to `"Submit"`). Confirm this still requires the physical `Submit` input (keyboard/gamepad) and is unaffected by the touch layer, since `TouchInput.ButtonDown` only intercepts `"Fire1"`.

- [ ] **Step 6: VR mode — no regression**

From `MenuPrincipal`, choose Realidad Virtual. Confirm: XR starts, no on-screen joystick/look-zone/action-button canvas is visible, and physical `Fire1`/`Submit`/`Jump` inputs behave exactly as before this change (Cardboard tap for confirm, grab button for cable, trigger for focus mode).

- [ ] **Step 7: Device test (not just Editor)**

Per the plan's own warning in `TouchActionButton.Pressed`'s comment: Update() order between `EventSystem` and consumers can differ between the Editor and an IL2CPP build. Build to an actual Android device and repeat Steps 1-5 there — do not sign off on Editor-only testing for this feature.

---

## Self-Review

**Spec coverage:** All 12 numbered sections of the original request map to a task — §1/§2 (`GameModeManager`/`MainMenuController`) are explicitly out of scope per Global Constraints (already implemented, correctly diverge from the prompt's idealized version); §3 → Task 6; §4 → Task 5; §5 → Task 3; §6 → Task 4; §7 → Task 1; §8 → Task 2; §9 → Tasks 7-8; §10 → Tasks 9-13; §11 → Task 14; §12 → Task 15.

**Placeholder scan:** No "TBD"/"add error handling"/"similar to Task N" — every step has full code or an exact Editor action.

**Type consistency:** `TouchInput.ButtonDown(string buttonName, ref int lastTouchPressId)` signature is identical across Tasks 2, 9, 10, 11, 12, 13. `TouchJoystick.MoveInput` / `TouchLook.LookInput` types (`Vector2`) match their consumers in Tasks 7-8. `TouchActionButton.Pressed`/`PressId`/`Exists` types match their one consumer, `TouchInput` (Task 2).
