using UnityEngine;

public class JoystickDebugRaw : MonoBehaviour
{
    void Update()
    {
        // Detecta hasta 20 botones físicos del joystick
        for (int i = 0; i < 20; i++)
        {
            if (Input.GetKeyDown("joystick button " + i))
            {
                Debug.Log("Joystick físico detectado: joystick button " + i);
            }
        }

        // También prueba con KeyCode (por si acaso)
        if (Input.GetKeyDown(KeyCode.JoystickButton0))
            Debug.Log("KeyCode.JoystickButton0");

        if (Input.GetKeyDown(KeyCode.JoystickButton1))
            Debug.Log("KeyCode.JoystickButton1");

        if (Input.GetKeyDown(KeyCode.JoystickButton2))
            Debug.Log("KeyCode.JoystickButton2");
    }
}