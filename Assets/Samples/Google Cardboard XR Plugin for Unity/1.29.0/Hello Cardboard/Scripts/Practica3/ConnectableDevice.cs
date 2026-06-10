using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PortInfo
{
    public string displayName;       // "SFP+ Input", "SFP+ Output", etc.
    public CableSocket socket;       // Referencia al CableSocket real
}

public class ConnectableDevice : MonoBehaviour
{
    [Header("Identificación")]
    public string deviceName;        // "Atenuador EXFO FVA-600"
    public Sprite deviceImage;
    [Header("Puertos del dispositivo")]
    public List<PortInfo> ports;     // Configurar en el inspector

    /// <summary>
    /// Devuelve los puertos que aún no tienen cable conectado.
    /// </summary>
    public List<PortInfo> GetAvailablePorts()
    {
        List<PortInfo> available = new List<PortInfo>();
        foreach (var port in ports)
        {
            if (port.socket != null && !port.socket.occupied)
                available.Add(port);
        }
        return available;
    }

    /// <summary>
    /// Devuelve true si hay al menos un puerto libre. 
    /// </summary>
    public bool HasAvailablePorts()
    {
        return GetAvailablePorts().Count > 0;
    }
}