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
