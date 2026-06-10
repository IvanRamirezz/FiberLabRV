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

            if (cable != null)
            {
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
                cable.Disconnect();
                this.Disconnect();
            }
        }
    }
    public void Connect(CableEnd cable)
    {
        connectedCable = cable;
    }

    public void Disconnect()
    {
        connectedCable = null;
    }
}

