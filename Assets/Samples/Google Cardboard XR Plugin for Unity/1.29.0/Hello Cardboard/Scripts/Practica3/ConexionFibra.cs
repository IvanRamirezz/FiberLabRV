using UnityEngine;

public class CableEnd : MonoBehaviour
{
    public CableSocket connectedSocket;
    public Transform otherEnd;
    public float maxLength = 3f;
    public void ConnectToSocket(CableSocket socket)
    {
        connectedSocket = socket;
        transform.position = socket.snapPoint.position;
        transform.rotation = socket.snapPoint.rotation;
    }

    public void Disconnect()
    {
        connectedSocket = null;
    }
}