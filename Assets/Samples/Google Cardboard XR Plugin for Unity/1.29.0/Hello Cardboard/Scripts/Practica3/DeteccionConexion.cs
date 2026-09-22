using UnityEngine;
public enum SocketType
{
    TxOutput,
    AtenuadorInput,
    AtenuadorOutput,
    RxInput
}
public class CableSocket : MonoBehaviour
{
    public SocketType socketType;
    public Transform snapPoint;
    public bool occupied => connectedCable != null;
    public CableEnd connectedCable;
    void OnTriggerEnter(Collider other)
    {
        if (occupied) return;

        if (other.CompareTag("CableEnd"))
        {
            CableEnd cable = other.GetComponent<CableEnd>();

            // Si el cable ya está conectado a OTRO socket, no lo robamos por una
            // simple colisión física — esto pasa cuando dos puertos están muy
            // pegados: al conectar por el menú a uno, la punta se teletransporta
            // ahí y ese mismo movimiento puede caer dentro del trigger del socket
            // vecino, "robando" el cable sin que el alumno haga nada.
            if (cable != null && cable.connectedSocket == null)
            {
                Debug.Log($"[P3-DEBUG] CableSocket.OnTriggerEnter: '{gameObject.name}' (t={Time.time:F1}) auto-conectando con '{cable.gameObject.name}' (colisión física, sin pasar por el menú)");
                cable.ConnectToSocket(this);
                this.Connect(cable);
            }
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("CableEnd"))
        {
            CableEnd cable = other.GetComponent<CableEnd>();
            if (cable != null && cable == connectedCable)
            {
                Debug.Log($"[P3-DEBUG] CableSocket.OnTriggerExit: '{gameObject.name}' (t={Time.time:F1}) desconectando de '{cable.gameObject.name}' (salió del trigger)");
                cable.Disconnect();
                this.Disconnect();
            }
        }
    }
    public void Connect(CableEnd cable)
    {
        Debug.Log($"[P3-DEBUG] CableSocket.Connect: '{gameObject.name}' (t={Time.time:F1}) <- '{cable.gameObject.name}'");
        connectedCable = cable;
    }

    public void Disconnect()
    {
        Debug.Log($"[P3-DEBUG] CableSocket.Disconnect: '{gameObject.name}' (t={Time.time:F1}), tenía conectado='{(connectedCable != null ? connectedCable.gameObject.name : "null")}'");
        connectedCable = null;
    }
}