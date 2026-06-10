using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // IMPORTANTE: Necesario para controlar la Imagen

public class HandInteraction : MonoBehaviour
{
    [Header("Configuración de Interacción")]
    public Transform holdPoint;
    public float grabRange = 3.0f;
    public LayerMask interactableLayer;
    public LayerMask placementLayer;
    public string BotonAgarrar = "Submit";
    public string BotonInteractuar = "Jump";
    P1_BufferIdentity currentGazedBuffer = null;

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

    [Header("Parámetros del cable")]
    public float maxCableLength = 3f;
    public static bool cableTensionReached = false;
    public static Vector3 cableDirection;

    public static event Action<GrabbableID> OnGrab;
    public static event Action<GrabbableID> OnRelease;



    void Start()
    {
        if (reticleImage != null)
            originalScale = reticleImage.transform.localScale;
    }


    void Update()
    {
        Debug.Log($"[HandInteraction] Instance = {P1_BufferMenu.Instance}, IsOpen = {(P1_BufferMenu.Instance != null ? P1_BufferMenu.Instance.IsOpen.ToString() : "N/A")}");
        if (P1_BufferMenu.Instance != null && P1_BufferMenu.Instance.IsOpen) return;
        //if (P1_BufferMenu.Instance != null && P1_BufferMenu.Instance.IsOpen) return;
        if (P1_GlobalCalcPanel.Instance != null && P1_GlobalCalcPanel.Instance.IsOpen) return;
        if (ConnectionMenuUI.Instance != null && ConnectionMenuUI.Instance.IsOpen) return;

        // Cooldown tras cerrar el menú para evitar reabrir inmediatamente
        if (menuCloseCooldown > 0f)
        {
            menuCloseCooldown -= Time.deltaTime;
            return;
        }
        UpdateReticle();

        if (Input.GetButtonDown(BotonInteractuar))
        {
            if (currentGazedBuffer != null &&
                P1_BufferMenu.Instance != null &&
                P1_BufferMenu.Instance.enabled)
            {
                P1_BufferMenu.Instance.Show(currentGazedBuffer.bufferIndex);
                return;
            }
            TryOpenFocusMode();
        }

        if (Input.GetButtonDown(BotonAgarrar))
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

    void TryOpenFocusMode()
    {
        if (FocusModeManager.Instance.IsInFocusMode)
            return;
        //if (!InstructionManager.Instance.allowFocusMode)
        //    return;
        if (heldObject != null)
            return;
        Debug.Log("NO HE REGRESADO ");
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, grabRange))
        {
            // ¿Es un BER Tester?
            BERTesterController ber =
                hit.collider.GetComponentInParent<BERTesterController>();
            OpticalAttenuatorController atenuador = hit.collider.GetComponentInParent<OpticalAttenuatorController>();
            if (atenuador != null)
            {
                Debug.Log("YA ABRI");

                atenuador.OpenFocus();
            }
            if (ber != null)
            {
                Debug.Log("YA ABRI");

                ber.OpenFocus();
            }
        }
    }


    void UpdateReticle()
    {
        if (reticleImage == null) return;

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
                P1_BufferIdentity bufferIdentity = hit.collider.GetComponentInParent<P1_BufferIdentity>();
                if (bufferIdentity != null && P1_BufferMenu.Instance != null && P1_BufferMenu.Instance.enabled)
                {
                    if (currentGazedBuffer != bufferIdentity)
                    {
                        currentGazedBuffer = bufferIdentity;
                    }
                    SetReticleState(interactColor, true);
                    return;
                }
                else if (currentGazedBuffer != null)
                {
                    currentGazedBuffer = null;
                }
            }
            else
            {
                if (currentGazedBuffer != null)
                {
                    currentGazedBuffer = null;
                }
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
        }
    }

 
    void Grab(GameObject obj)
    {
        heldObject = obj;
        heldRb = heldObject.GetComponent<Rigidbody>();

        Grabbable grabbable = heldObject.GetComponent<Grabbable>();
        if (grabbable == null) return;

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

        OnGrab?.Invoke(grabbable.id);
    }
    void DropAndPlace()
    {
        if (heldObject == null) return;

        Grabbable grabbable = heldObject.GetComponent<Grabbable>();
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
                    var available = device.GetAvailablePorts();

                    if (available.Count == 0)
                    {
                        Debug.Log("Todos los puertos de " + device.deviceName +
                                  " están ocupados.");
                        // Alumno no puede conectar aquí; el cable se queda agarrado
                        return;
                    }
                    else if (available.Count == 1)
                    {
                        // Un solo puerto disponible: conectar directo sin menú
                        ConnectCableToSocket(cableEnd, available[0].socket);
                        ReleaseHeldCable(grabbable);
                        return;
                    }
                    else
                    {
                        // Múltiples puertos: mostrar menú
                        OpenConnectionMenu(device, cableEnd, available, grabbable);
                        return;
                    }
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

            ReleaseHeldCable(grabbable);
            return;

        }

        // ─── Caso general: objeto no-cable, comportamiento original ───
        DropNonCableObject(grabbable);
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
        // Ejemplo: evitar conectar dos inputs
        if (cable.connectedSocket != null)
        {
            return socket.socketType != cable.connectedSocket.socketType;
        }

        return true;
    }
    void ConnectCableToSocket(CableEnd cable, CableSocket socket)
    {
        cable.ConnectToSocket(socket);
        socket.Connect(cable);
        Debug.Log("Cable conectado a " + socket.socketType);
    }

    void OpenConnectionMenu(ConnectableDevice device, CableEnd cable,
                           List<PortInfo> availablePorts, Grabbable grabbable)
    {
        ConnectionMenuUI.Instance.Show(
            device,
            cable,
            availablePorts,
            onSelect: (selectedCable, selectedSocket) =>
            {
                ConnectCableToSocket(selectedCable, selectedSocket);
                ReleaseHeldCable(grabbable);
                menuCloseCooldown = 0.3f;

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

    void ReleaseHeldCable(Grabbable grabbable)
    {
        if (grabbable != null)
            OnRelease?.Invoke(grabbable.id);

        heldObject = null;
        heldRb = null;
    }

    void DropNonCableObject(Grabbable grabbable)
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

        if (grabbable != null)
            OnRelease?.Invoke(grabbable.id);

        heldObject = null;
        heldRb = null;
    }
}