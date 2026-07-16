using UnityEngine;

/// <summary>
/// Rotates the camera using the right joystick of a connected gamepad.
/// Attach to the "Player (Camara)" GameObject.
/// Requires "RightStickHorizontal" and "RightStickVertical" axes defined
/// in Project Settings > Input Manager (see setup instructions below).
///
/// Input Manager axis setup:
///   Name: RightStickHorizontal  | Type: Joystick Axis | Axis: 4th axis
///   Name: RightStickVertical    | Type: Joystick Axis | Axis: 5th axis
/// </summary>
public class CameraLookController : MonoBehaviour
{
    [Header("Input Axes")]
    public string lookHorizontalAxis = "RightStickHorizontal";
    public string lookVerticalAxis   = "RightStickVertical";

    [Header("Settings")]
    public float sensitivity  = 120f;
    public bool  invertY      = true;
    [Range(-90f, 0f)]  public float pitchMin = -80f;
    [Range(0f,  90f)]  public float pitchMax =  80f;

    float _pitch;

    void Start()
    {
        // Sync internal pitch with current camera X rotation
        _pitch = transform.eulerAngles.x;
        if (_pitch > 180f) _pitch -= 360f;
    }

    void Update()
    {
        float h = GetAxisSafe(lookHorizontalAxis) + TouchLook.LookInput.x;
        float v = GetAxisSafe(lookVerticalAxis)   + TouchLook.LookInput.y;

        if (invertY) v = -v;

        float delta = sensitivity * Time.deltaTime;

        // Yaw: rotate around world Y so movement direction stays consistent
        transform.Rotate(Vector3.up, h * delta, Space.World);

        // Pitch: tilt up/down, clamped to avoid flipping
        _pitch -= v * delta;
        _pitch  = Mathf.Clamp(_pitch, pitchMin, pitchMax);

        Vector3 angles = transform.eulerAngles;
        transform.eulerAngles = new Vector3(_pitch, angles.y, 0f);
    }

    // Returns 0 silently if the axis hasn't been defined yet in Input Manager
    float GetAxisSafe(string axisName)
    {
        try { return Input.GetAxis(axisName); }
        catch { return 0f; }
    }
}
