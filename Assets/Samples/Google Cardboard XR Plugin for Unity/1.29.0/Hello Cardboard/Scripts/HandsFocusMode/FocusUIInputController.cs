using UnityEngine;
using UnityEngine.EventSystems;

public class FocusUIInputController : MonoBehaviour
{
    [Header("Input Names (Legacy)")]
    public string horizontalAxis = "Horizontal";
    public string verticalAxis = "Vertical";
    public string submitButton = "Fire1";
    public string cancelButton = "Fire2";

    [Header("Settings")]
    public float navigationThreshold = 0.5f;
    public float inputCooldown = 0.3f;

    float lastInputTime;

    void Update()
    {
        if (!FocusModeManager.Instance.IsInFocusMode)
            return;

        HandleNavigation();
        HandleSubmitCancel();
    }

    void HandleNavigation()
    {
        if (Time.time - lastInputTime < inputCooldown)
            return;

        float h = Input.GetAxis(horizontalAxis);
        float v = Input.GetAxis(verticalAxis);

        if (v > navigationThreshold)
            Move(EventSystem.current, Vector2.up);
        else if (v < -navigationThreshold)
            Move(EventSystem.current, Vector2.down);
        else if (h > navigationThreshold)
            Move(EventSystem.current, Vector2.right);
        else if (h < -navigationThreshold)
            Move(EventSystem.current, Vector2.left);
    }

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

    void Move(EventSystem es, Vector2 dir)
    {
        if (es.currentSelectedGameObject == null)
            return;

        AxisEventData data = new AxisEventData(es);
        data.moveDir = GetMoveDirection(dir);
        data.moveVector = dir;

        ExecuteEvents.Execute(
            es.currentSelectedGameObject,
            data,
            ExecuteEvents.moveHandler
        );

        lastInputTime = Time.time;
    }

    MoveDirection GetMoveDirection(Vector2 dir)
    {
        if (dir == Vector2.up) return MoveDirection.Up;
        if (dir == Vector2.down) return MoveDirection.Down;
        if (dir == Vector2.left) return MoveDirection.Left;
        return MoveDirection.Right;
    }
}