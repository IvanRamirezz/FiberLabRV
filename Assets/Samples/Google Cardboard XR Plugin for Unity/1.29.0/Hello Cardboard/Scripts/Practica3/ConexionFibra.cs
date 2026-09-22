using UnityEngine;

public class CableEnd : MonoBehaviour
{
    public CableSocket connectedSocket;
    public Transform otherEnd;
    public float maxLength = 3f;

    Vector3 startLocalPosition;
    Quaternion startLocalRotation;

    void Awake()
    {
        startLocalPosition = transform.localPosition;
        startLocalRotation = transform.localRotation;
    }

    public void ConnectToSocket(CableSocket socket)
    {
        Debug.Log($"[P3-DEBUG] CableEnd.ConnectToSocket: '{gameObject.name}' (t={Time.time:F1}) -> '{socket.gameObject.name}'");
        connectedSocket = socket;
        transform.position = socket.snapPoint.position;
        transform.rotation = socket.snapPoint.rotation;
    }

    public void Disconnect()
    {
        Debug.Log($"[P3-DEBUG] CableEnd.Disconnect: '{gameObject.name}' (t={Time.time:F1}), tenía connectedSocket='{(connectedSocket != null ? connectedSocket.gameObject.name : "null")}'");
        connectedSocket = null;
    }

    // Desconecta del socket (si aplica) y devuelve la punta a su posición/rotación
    // original — usado al salir del Demo para que las conexiones que el alumno
    // hizo practicando no se arrastren a la práctica real.
    public void ResetToStart()
    {
        Debug.Log($"[P3-DEBUG] CableEnd.ResetToStart: '{gameObject.name}' (t={Time.time:F1}) llamado. connectedSocket actual='{(connectedSocket != null ? connectedSocket.gameObject.name : "null")}'");
        if (connectedSocket != null)
        {
            connectedSocket.Disconnect();
            Disconnect();
        }

        transform.localPosition = startLocalPosition;
        transform.localRotation = startLocalRotation;
    }
}