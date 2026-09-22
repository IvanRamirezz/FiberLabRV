using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // IMPORTANTE: Necesario para controlar la Imagen
using TMPro;

public class HandInteraction : MonoBehaviour
{
    [Header("Configuración de Interacción")]
    public Transform holdPoint;
    public float grabRange = 3.0f;
    public LayerMask interactableLayer;
    public LayerMask placementLayer;
    public string BotonAgarrar = "Submit";
    public string BotonInteractuar = "Jump";

    [Header("Configuración de Retícula (Visual)")]
    public Image reticleImage;      // Arrastra aquí tu imagen del punto blanco
    public Color normalColor = Color.white;
    public Color hoverColor = Color.green;  // Color al mirar un objeto
    public Color dropColor = Color.cyan;    // Color al mirar dónde soltar
    public Color interactColor = Color.magenta;    // Color al mirar dónde soltar
    public Color validConnectionColor = Color.red; //Color para detectar punto de conexión
    public float resizeFactor = 1.5f;       // Cuánto crece el punto al detectar algo
    private float menuCloseCooldown = 0f;

    private GameObject heldObject;
    private Rigidbody heldRb;
    private Vector3 originalScale; // Para recordar el tamaño original del punto

    // Cada call site de TouchInput.ButtonDown necesita su propio campo de
    // dedupe (ver TouchInput.cs) para reaccionar exactamente una vez por toque.
    private int _lastTouchPressIdInteract = -10;
    private int _lastTouchPressIdGrab = -10;

    [Header("Parámetros del cable")]
    public float maxCableLength = 3f;
    public static bool cableTensionReached = false;
    public static Vector3 cableDirection;

    // Otro script puede fijar este color para sobreescribir el reticle un frame.
    // Null = sin override (comportamiento normal). Usado por InstructionManager_Demo
    // y P1_InstructionManager durante los pasos guiados de mirar/seleccionar.
    public static Color? ReticleOverride = null;

    // Disparado al soltar cualquier objeto sostenido. Usado por
    // InstructionManager_Demo como fallback para detectar que el alumno
    // soltó un cable durante el tutorial guiado.
    public static event System.Action OnRelease;

    [Header("Retroalimentación de conexión (UI)")]
    public TMP_Text connectionFeedbackText;   // TMP independiente para mensajes de error/aviso
    public float feedbackDuration = 2.5f;     // Segundos que el mensaje permanece visible
    private Coroutine feedbackCoroutine;




    void Start()
    {
        if (reticleImage != null)
            originalScale = reticleImage.transform.localScale;
    }


    void Update()
    {
        if (ConnectionMenuUI.Instance != null && ConnectionMenuUI.Instance.IsOpen) return;

        // Cooldown tras cerrar el menú para evitar reabrir inmediatamente
        if (menuCloseCooldown > 0f)
        {
            menuCloseCooldown -= Time.deltaTime;
            return;
        }
        UpdateReticle();

        if (TouchInput.ButtonDown(BotonInteractuar, ref _lastTouchPressIdInteract))
        {
            TryOpenFocusMode();
        }

        if (TouchInput.ButtonDown(BotonAgarrar, ref _lastTouchPressIdGrab))
        {
            if (heldObject == null) TryGrab();
            else DropAndPlace();
        }

        // ───── mover conectores de cable manualmente ─────
        if (heldObject != null)
        {
            CableEnd cableEnd = heldObject.GetComponent<CableEnd>();

            if (cableEnd != null)
            {
                Vector3 desiredPosition = holdPoint.position;

                Vector3 anchorPos = cableEnd.otherEnd.position;

                float dist = Vector3.Distance(anchorPos, desiredPosition);

                CableEnd cable = heldObject.GetComponent<CableEnd>();
                float maxLength = cable.maxLength;


                if (dist > maxCableLength)
                {
                    cableTensionReached = true;
                    Vector3 dir = (desiredPosition - anchorPos).normalized;
                    desiredPosition = anchorPos + dir * maxCableLength;
                    cableDirection = (heldObject.transform.position - cable.otherEnd.position).normalized;

                }
                else
                {
                    cableTensionReached = false;
                }

                heldObject.transform.position = desiredPosition;
                heldObject.transform.rotation = holdPoint.rotation;
            }
        }
    }

    //void TryOpenFocusMode()
    //{
    //    if (FocusModeManager.Instance.IsInFocusMode)
    //        return;
    //    //if (!InstructionManager.Instance.allowFocusMode)
    //    //    return;
    //    if (heldObject != null)
    //        return;
    //    Debug.Log("NO HE REGRESADO ");
    //    Ray ray = new Ray(transform.position, transform.forward);
    //    RaycastHit hit;

    //    if (Physics.Raycast(ray, out hit, grabRange))
    //    {
    //        // ¿Es un BER Tester?
    //        BERTesterController ber =
    //            hit.collider.GetComponentInParent<BERTesterController>();
    //        OpticalAttenuatorController atenuador = hit.collider.GetComponentInParent<OpticalAttenuatorController>();
    //        if (atenuador != null)
    //        {
    //            Debug.Log("YA ABRI");

    //            atenuador.OpenFocus();
    //        }
    //        if (ber != null)
    //        {
    //            Debug.Log("YA ABRI");

    //            ber.OpenFocus();
    //        }
    //    }
    //}
    void TryOpenFocusMode()
    {
        if (FocusModeManager.Instance == null || FocusModeManager.Instance.IsInFocusMode)
            return;
        //if (!InstructionManager.Instance.allowFocusMode)
        //    return;
        if (heldObject != null)
            return;

        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, grabRange))
        {
            // Cualquier equipo que implemente IFocusable: BERT, atenuador, OTDR
            // y lo que venga después. No hace falta volver a tocar este método.
            IFocusable focusable = hit.collider.GetComponentInParent<IFocusable>();

            if (focusable != null)
            {
                Debug.Log($"[Focus] Abriendo {((MonoBehaviour)focusable).gameObject.name}");
                focusable.OpenFocus();
            }
        }
    }

    void UpdateReticle()
    {
        if (reticleImage == null) return;

        if (ReticleOverride.HasValue)
        {
            SetReticleState(ReticleOverride.Value, true);
            return;
        }

        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        // ──────────────── ESTADO 1: OBJETO EN MANO ────────────────
        //if (heldObject != null)
        //{
        //    if (Physics.Raycast(ray, out hit, grabRange, placementLayer))
        //    {
        //        SetReticleState(dropColor, true);
        //    }
        //    else
        //    {
        //        SetReticleState(normalColor, false);
        //    }
        //}
        if (heldObject != null)
        {
            if (Physics.Raycast(ray, out hit, grabRange))
            {
                CableSocket socket = hit.collider.GetComponent<CableSocket>();
                CableEnd cableEnd = heldObject.GetComponent<CableEnd>();

                if (socket != null && cableEnd != null)
                {
                    if (IsValidConnection(cableEnd, socket))
                    {
                        SetReticleState(validConnectionColor, true);
                        return;
                    }
                    else
                    {
                        SetReticleState(dropColor, true); // inválido
                        return;
                    }
                }
                // ¿Apuntando a un equipo conectable? (NUEVO)
                if (cableEnd != null)
                {
                    ConnectableDevice device =
                        hit.collider.GetComponentInParent<ConnectableDevice>();
                    if (device != null && device.HasAvailablePorts())
                    {
                        SetReticleState(validConnectionColor, true);
                        return;
                    }
                }
                // fallback
                if (((1 << hit.collider.gameObject.layer) & placementLayer) != 0)
                {
                    SetReticleState(dropColor, true);
                }
                else
                {
                    SetReticleState(normalColor, false);
                }
            }
        }
        // ──────────────── ESTADO 2: MANOS VACÍAS ────────────────
        else
        {
            if (Physics.Raycast(ray, out hit, grabRange))
            {

                // ¿El objeto es focusable?
                IFocusable focusable =
                    hit.collider.GetComponentInParent<IFocusable>();

                if (focusable != null)
                {
                    //  Retícula especial de interacción
                    SetReticleState(interactColor, true);
                    return;
                }

                //  ¿Es agarrable?
                if (((1 << hit.collider.gameObject.layer) & interactableLayer) != 0)
                {
                    SetReticleState(hoverColor, true);
                }
                else
                {
                    SetReticleState(normalColor, false);
                }
            }
            else
            {
                SetReticleState(normalColor, false);
            }
        }
    }


    void SetReticleState(Color targetColor, bool isActive)
    {
        // Cambiar color suavemente (Lerp) o directo
        reticleImage.color = Color.Lerp(reticleImage.color, targetColor, Time.deltaTime * 10f);

        // Cambiar tamaño: Si está activo crece, si no, vuelve a tamaño original
        Vector3 targetScale = isActive ? originalScale * resizeFactor : originalScale;
        reticleImage.transform.localScale = Vector3.Lerp(reticleImage.transform.localScale, targetScale, Time.deltaTime * 10f);
    }

    // --- (El resto de funciones siguen igual que antes) ---

    void TryGrab()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, grabRange, interactableLayer))
        {
            Grab(hit.collider.gameObject);
            return;
        }

        // No golpeamos un objeto agarrable directamente (el extremo del cable puede
        // quedar "escondido" contra el socket una vez conectado). Si apuntamos a un
        // socket OCUPADO, lo desconectamos y tomamos el cable que estaba ahí.
        if (Physics.Raycast(ray, out hit, grabRange))
        {
            CableSocket socket = hit.collider.GetComponent<CableSocket>();
            if (socket != null && socket.occupied)
            {
                CableEnd cable = socket.connectedCable;
                socket.Disconnect();
                cable.Disconnect();
                Grab(cable.gameObject);
            }
        }
    }


    void Grab(GameObject obj)
    {
        heldObject = obj;
        heldRb = heldObject.GetComponent<Rigidbody>();

        CableEnd cableEnd = heldObject.GetComponent<CableEnd>();

        Collider col = heldObject.GetComponent<Collider>();

        if (heldRb != null)
        {
            heldRb.useGravity = false;
            heldRb.isKinematic = true;
        }

        if (col != null && cableEnd == null)
            col.enabled = false;

        heldObject.transform.position = holdPoint.position;
        heldObject.transform.rotation = holdPoint.rotation;

        // 🔹 SOLO parentar si NO es cable
        if (cableEnd == null)
            heldObject.transform.SetParent(holdPoint);
    }
    void DropAndPlace()
    {
        if (heldObject == null) return;

        CableEnd cableEnd = heldObject.GetComponent<CableEnd>();

        // ─── Caso especial: estamos sosteniendo un cable ───
        if (cableEnd != null)
        {
            Ray ray = new Ray(transform.position, transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, grabRange))
            {
                ConnectableDevice device =
                    hit.collider.GetComponentInParent<ConnectableDevice>();

                if (device != null)
                {
                    // Se muestra siempre el menú con TODOS los puertos del dispositivo
                    // (ocupados o no, válidos o no para este cable). La validación real
                    // ocurre al presionar un botón específico (ver OpenConnectionMenu),
                    // para que el alumno vea explícitamente qué intentó y por qué falló o no.
                    OpenConnectionMenu(device, cableEnd, device.ports);
                    return;
                }
            }

            // No apuntando a un dispositivo: drop normal del cable en el aire/mesa
            //ReleaseHeldCable(grabbable);
            //return;
            // No apuntando a un dispositivo: soltar cable donde apunta el ray
            Ray dropRay = new Ray(transform.position, transform.forward);
            RaycastHit dropHit;

            if (Physics.Raycast(dropRay, out dropHit, grabRange, placementLayer))
            {
                heldObject.transform.position = dropHit.point + (dropHit.normal * 0.05f);
            }

            ReleaseHeldCable();
            return;

        }

        // ─── Caso general: objeto no-cable, comportamiento original ───
        DropNonCableObject();
    }

    //void DropAndPlace()
    //{
    //    if (heldObject == null) return;

    //    Grabbable grabbable = heldObject.GetComponent<Grabbable>();

    //    Ray ray = new Ray(transform.position, transform.forward);
    //    RaycastHit hit;

    //    if (Physics.Raycast(ray, out hit, grabRange, placementLayer))
    //    {
    //        heldObject.transform.position = hit.point + (hit.normal * 0.1f);
    //        heldObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
    //    }

    //    Collider col = heldObject.GetComponent<Collider>();
    //    heldObject.transform.SetParent(null);
    //    if (col != null) col.enabled = true;

    //    //if (heldRb != null)
    //    //{
    //    //    heldRb.useGravity = true;
    //    //    heldRb.isKinematic = false;
    //    //    heldRb.linearVelocity = Vector3.zero;
    //    //}
    //    CableEnd cableEnd = heldObject.GetComponent<CableEnd>();
    //    if (heldRb != null && cableEnd == null)
    //    {
    //        heldRb.useGravity = true;
    //        heldRb.isKinematic = false;
    //        heldRb.linearVelocity = Vector3.zero;
    //    }

    //    // AVISO AL SISTEMA
    //    if (grabbable != null)
    //        OnRelease?.Invoke(grabbable.id);

    //    heldObject = null;
    //    heldRb = null;
    //}
    bool IsValidConnection(CableEnd cable, CableSocket socket)
    {
        // Usado por el retículo (feedback visual): combina ambas condiciones.
        return !socket.occupied && IsTopologyValid(cable, socket);
    }

    bool IsTopologyValid(CableEnd cable, CableSocket socket)
    {
        // El otro extremo de ESTE MISMO cable (no este) es el que define la topología.
        CableEnd otherEnd = cable.otherEnd != null ? cable.otherEnd.GetComponent<CableEnd>() : null;

        if (otherEnd == null || otherEnd.connectedSocket == null)
        {
            // Ningún extremo de este cable está conectado todavía:
            // cualquier socket libre es un punto de partida válido.
            return true;
        }

        // El otro extremo ya está conectado: este extremo SOLO puede ir al tipo
        // de socket que le corresponde según la topología real del enlace:
        //   TxOutput        ↔️ AtenuadorInput
        //   AtenuadorOutput ↔️ RxInput
        return IsValidPair(otherEnd.connectedSocket.socketType, socket.socketType);
    }

    bool IsValidPair(SocketType a, SocketType b)
    {
        return (a == SocketType.TxOutput && b == SocketType.AtenuadorInput) ||
               (a == SocketType.AtenuadorInput && b == SocketType.TxOutput) ||
               (a == SocketType.AtenuadorOutput && b == SocketType.RxInput) ||
               (a == SocketType.RxInput && b == SocketType.AtenuadorOutput);
    }
    void ConnectCableToSocket(CableEnd cable, CableSocket socket)
    {
        // Si este extremo ya estaba conectado a otro socket, liberarlo primero
        // para que no quede marcado como "ocupado" de forma huérfana.
        if (cable.connectedSocket != null && cable.connectedSocket != socket)
        {
            cable.connectedSocket.Disconnect();
        }

        cable.ConnectToSocket(socket);
        socket.Connect(cable);
        Debug.Log("Cable conectado a " + socket.socketType);
    }

    void OpenConnectionMenu(ConnectableDevice device, CableEnd cable,
                           List<PortInfo> allPorts)
    {
        ConnectionMenuUI.Instance.Show(
            device,
            cable,
            allPorts,
            onSelect: (selectedCable, selectedSocket) =>
            {
                Debug.Log($"[P3-DEBUG] Intentando conectar '{selectedCable.gameObject.name}' a socket '{selectedSocket.gameObject.name}' (tipo={selectedSocket.socketType}). " +
                          $"occupied={selectedSocket.occupied}, connectedCable={(selectedSocket.connectedCable != null ? selectedSocket.connectedCable.gameObject.name : "null")}. " +
                          $"selectedCable.connectedSocket={(selectedCable.connectedSocket != null ? selectedCable.connectedSocket.gameObject.name : "null")}, " +
                          $"otherEnd={(selectedCable.otherEnd != null ? selectedCable.otherEnd.name : "null")}, " +
                          $"otherEnd.connectedSocket={(selectedCable.otherEnd != null && selectedCable.otherEnd.GetComponent<CableEnd>() != null && selectedCable.otherEnd.GetComponent<CableEnd>().connectedSocket != null ? selectedCable.otherEnd.GetComponent<CableEnd>().connectedSocket.gameObject.name : "null")}");

                if (selectedSocket.occupied)
                {
                    ShowConnectionFeedback("Ese puerto ya está ocupado.");
                    return false;
                }

                if (!IsTopologyValid(selectedCable, selectedSocket))
                {
                    ShowConnectionFeedback("Esta conexión no es válida. \nRevisa a qué puerto está conectado el otro extremo del cable.");
                    return false;
                }

                ConnectCableToSocket(selectedCable, selectedSocket);
                ReleaseHeldCable();
                menuCloseCooldown = 0.3f;
                return true;
            },
            onCancel: () =>
            {
                // El alumno canceló; el cable sigue agarrado
                // (no liberamos heldObject)
                if (heldObject != null)
                {
                    heldObject.transform.position = holdPoint.position;
                    heldObject.transform.rotation = holdPoint.rotation;
                }
                menuCloseCooldown = 0.3f;
            }
        );
    }

    void ReleaseHeldCable()
    {
        OnRelease?.Invoke();

        heldObject = null;
        heldRb = null;
    }

    // ──────────────── RETROALIMENTACIÓN DE CONEXIÓN (UI) ────────────────

    void ShowConnectionFeedback(string message)
    {
        Debug.Log(message);

        if (connectionFeedbackText == null) return;

        connectionFeedbackText.text = message;

        if (feedbackCoroutine != null)
            StopCoroutine(feedbackCoroutine);

        feedbackCoroutine = StartCoroutine(ClearFeedbackAfterDelay());
    }

    IEnumerator ClearFeedbackAfterDelay()
    {
        yield return new WaitForSeconds(feedbackDuration);
        if (connectionFeedbackText != null)
            connectionFeedbackText.text = "";
    }

    void DropNonCableObject()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, grabRange, placementLayer))
        {
            heldObject.transform.position = hit.point + (hit.normal * 0.1f);
            heldObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
        }

        Collider col = heldObject.GetComponent<Collider>();
        heldObject.transform.SetParent(null);
        if (col != null) col.enabled = true;

        if (heldRb != null)
        {
            heldRb.useGravity = true;
            heldRb.isKinematic = false;
            heldRb.linearVelocity = Vector3.zero;
        }

        OnRelease?.Invoke();

        heldObject = null;
        heldRb = null;
    }
}