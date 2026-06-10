using UnityEngine;
using System;

public enum LinkScenario
{
    Disconnected,  // No hay continuidad física en el montaje
    Nominal,       // Enlace sano, BER muy por debajo del umbral
    Degraded,      // Enlace con errores visibles, FAIL
    Critical       // Atenuación excesiva, receptor en LOS
}

[Serializable]
public class LinkScenarioData
{
    public LinkScenario scenario;
    public double berNominal;        // BER precalculado para este escenario
    public float potenciaRxDBm;      // Potencia recibida estimada (informativo)
    public float factorQ;            // Factor Q (informativo)
    public bool linkEstablished;     // ¿El PHY reporta link up?
    public string statusText;        // "PASS", "FAIL", "No Link"
    public string quickTestMessage;  // Mensaje del Quick Test para este escenario
}

public class LinkStateManager : MonoBehaviour
{
    public static LinkStateManager Instance;

    [Header("Sockets del enlace óptico")]
    [SerializeField] CableSocket txSocket;
    [SerializeField] CableSocket atenInSocket;
    [SerializeField] CableSocket atenOutSocket;
    [SerializeField] CableSocket rxSocket;

    [Header("Referencia al atenuador")]
    [SerializeField] OpticalAttenuatorController attenuator;

    [Header("Umbrales de atenuación (valor absoluto en dB)")]
    [SerializeField] float umbralDegradado = 10f;  // >= 10 dB -> Degradado
    [SerializeField] float umbralCritico = 20f;    // >= 20 dB -> Critical (LOS)

    [Header("Datos precalculados de cada escenario")]
    [SerializeField] LinkScenarioData dataDisconnected;
    [SerializeField] LinkScenarioData dataNominal;
    [SerializeField] LinkScenarioData dataDegraded;
    [SerializeField] LinkScenarioData dataCritical;

    public LinkScenario CurrentScenario { get; private set; }

    public static event Action<LinkScenario> OnScenarioChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Estado inicial: desconectado
        CurrentScenario = LinkScenario.Disconnected;
    }

    void Start()
    {
        RecalculateScenario();
    }

    void OnEnable()
    {
        OpticalAttenuatorController.OnAttenuatorPower += HandleAttenuatorChanged;
        OpticalAttenuatorController.OnAttenuatorPoweredOn += HandleAttenuatorChanged;
    }

    void OnDisable()
    {
        OpticalAttenuatorController.OnAttenuatorPower -= HandleAttenuatorChanged;
        OpticalAttenuatorController.OnAttenuatorPoweredOn -= HandleAttenuatorChanged;
    }

    void Update()
    {
        // Poll ligero de los sockets. Como no hay evento de "socket conectado",
        // revisamos continuamente si el estado de link cambió.
        RecalculateScenario();
    }

    void HandleAttenuatorChanged()
    {
        RecalculateScenario();
    }

    void RecalculateScenario()
    {
        Debug.LogWarning("Estoy recalculando");
        LinkScenario nuevo = DetermineScenario();

        if (nuevo != CurrentScenario)
        {
            CurrentScenario = nuevo;
            OnScenarioChanged?.Invoke(CurrentScenario);
            Debug.Log($"[LinkStateManager] Escenario cambiado a: {CurrentScenario}");
            Debug.LogWarning("Quedo =");
        }
    }

    LinkScenario DetermineScenario()
    {
        // 1. ¿Hay continuidad física en los cuatro sockets del enlace?
        if (!IsLinkEstablished())
            return LinkScenario.Disconnected;
        Debug.LogWarning("Pasa primera prueba de continuidad");
        // 2. ¿El atenuador está encendido? Si no lo está, asumimos paso directo
        //    (atenuación efectiva = 0). Esto es una simplificación razonable.
        float atenuacionAbs = 0f;
        if (attenuator != null && attenuator.IsOn)
        {
            atenuacionAbs = Mathf.Abs(attenuator.CurrentPower);
        }
        Debug.LogWarning("Pasa segunda prueba de continuidad");
        // 3. Mapeo por rangos
        if (atenuacionAbs >= umbralCritico)
            return LinkScenario.Critical;
        if (atenuacionAbs >= umbralDegradado)
            return LinkScenario.Degraded;
        Debug.LogWarning("Pasa tercera prueba de continuidad");
        return LinkScenario.Nominal;
    }

    bool IsLinkEstablished()
    {
        Debug.Log(txSocket.occupied);
        Debug.Log(atenInSocket.occupied);
        Debug.Log(atenOutSocket.occupied);
        Debug.Log(rxSocket.occupied);

        if (txSocket == null || atenInSocket == null ||
            atenOutSocket == null || rxSocket == null)
        {
           
            Debug.LogWarning("[LinkStateManager] Sockets no asignados en inspector.");
            return false;
        }

        return txSocket.occupied &&
               atenInSocket.occupied &&
               atenOutSocket.occupied &&
               rxSocket.occupied;
    }

    // ─────────────────────────────
    // API pública para consulta
    // ─────────────────────────────

    public LinkScenarioData GetCurrentData()
    {
        switch (CurrentScenario)
        {
            case LinkScenario.Nominal: return dataNominal;
            case LinkScenario.Degraded: return dataDegraded;
            case LinkScenario.Critical: return dataCritical;
            default: return dataDisconnected;
        }
    }

    public bool HasLink()
    {
        return CurrentScenario != LinkScenario.Disconnected &&
               CurrentScenario != LinkScenario.Critical;
    }
}