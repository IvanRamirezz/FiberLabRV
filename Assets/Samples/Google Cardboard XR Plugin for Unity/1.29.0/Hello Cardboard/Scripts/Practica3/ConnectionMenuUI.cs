using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Collections.Generic;

public class ConnectionMenuUI : MonoBehaviour
{
    public static ConnectionMenuUI Instance;
    [Header("Posición (World Space)")]
    public float distanciaAlJugador = 0.2f;
    public float alturaOffset = 0.05f;
    [Header("UI References (Canvas Screen Space Overlay)")]
    public GameObject menuPanel;
    public Transform buttonContainer;
    public GameObject buttonPrefab;
    public TMP_Text deviceNameText;
    public Image deviceImage;
    [Header("Input")]
    public string navigateAxis = "Vertical";
    public string submitButton = "Fire1";
    public string cancelButton = "Fire2";
    public float inputCooldown = 0.25f;
    public float navigationThreshold = 0.5f;
    [Header("Referencias")]
    public MotionObjectController motionController;

    CableEnd pendingCable;
    Action<CableEnd, CableSocket> onPortSelected;
    Action onCancelled;

    List<Button> currentButtons = new List<Button>();
    int selectedIndex = 0;
    float lastInputTime;

    public bool IsOpen { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        menuPanel.SetActive(false);
        IsOpen = false;
    }

    void Update()
    {

        if (!IsOpen) return;
        
        HandleNavigation();
        HandleButtons();
    }

    void HandleNavigation()
    {
        if (Time.time - lastInputTime < inputCooldown) return;

        float v = Input.GetAxis(navigateAxis);

        if (v > navigationThreshold)
        {
            selectedIndex--;
            if (selectedIndex < 0) selectedIndex = currentButtons.Count - 1;
            HighlightButton(selectedIndex);
            lastInputTime = Time.time;
        }
        else if (v < -navigationThreshold)
        {
            selectedIndex++;
            if (selectedIndex >= currentButtons.Count) selectedIndex = 0;
            HighlightButton(selectedIndex);
            lastInputTime = Time.time;
        }
    }

    void HandleButtons()
    {
        if (Input.GetButtonDown(submitButton))
        {
            if (selectedIndex >= 0 && selectedIndex < currentButtons.Count)
            {
                currentButtons[selectedIndex].onClick.Invoke();
            }
        }

        if (Input.GetButtonDown(cancelButton))
        {
            HandleCancel();
        }
    }

    public void Show(ConnectableDevice device, CableEnd cable,
                     List<PortInfo> availablePorts,
                     Action<CableEnd, CableSocket> onSelect,
                     Action onCancel)
    {
        pendingCable = cable;
        onPortSelected = onSelect;
        onCancelled = onCancel;

        if (deviceNameText != null)
            deviceNameText.text = device.deviceName;
        // Imagen del dispositivo
        if (deviceImage != null)
        {
            if (device.deviceImage != null)
            {
                deviceImage.sprite = device.deviceImage;
                deviceImage.enabled = true;
            }
            else
            {
                deviceImage.enabled = false;
            }
        }
        // Limpiar botones anteriores
        // Limpiar botones anteriores (solo los instanciados dinámicamente)
        for (int i = buttonContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(buttonContainer.GetChild(i).gameObject);
        }
        currentButtons.Clear();

        // Crear botón por puerto
        foreach (var port in availablePorts)
        {
            GameObject btnGO = Instantiate(buttonPrefab, buttonContainer);
            TMP_Text label = btnGO.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = port.displayName;

            CableSocket targetSocket = port.socket;
            Button btn = btnGO.GetComponent<Button>();
            btn.onClick.AddListener(() => HandlePortSelection(targetSocket));
            currentButtons.Add(btn);
        }



        // Botón cancelar
        GameObject cancelGO = Instantiate(buttonPrefab, buttonContainer);
        TMP_Text cancelLabel = cancelGO.GetComponentInChildren<TMP_Text>();
        if (cancelLabel != null) cancelLabel.text = "Cancelar";
        Button cancelBtn = cancelGO.GetComponent<Button>();
        cancelBtn.onClick.AddListener(HandleCancel);
        currentButtons.Add(cancelBtn);

        // Seleccionar primer botón
        selectedIndex = 0;
        HighlightButton(0);
        // Posicionar frente al jugador
        //Camera cam = Camera.main;
        //if (cam != null)
        //{
        //    Vector3 forward = cam.transform.forward;
        //    forward.y = 0f;
        //    forward.Normalize();

        //    // Posición: adelante del jugador, ligeramente arriba
        //    transform.position = cam.transform.position
        //                         + forward * distanciaAlJugador
        //                         + Vector3.up * alturaOffset;

        //    // Rotación: mirando hacia el jugador
        //    Vector3 lookDir = transform.position - cam.transform.position;
        //    lookDir.y = 0f;
        //    transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
        //}

        Camera cam = Camera.main;
        if (cam != null)
        {
            //Vector3 forward = cam.transform.forward;
            //forward.y = 0f - 0.1f;
            //forward.Normalize();

            //// Posición: adelante del jugador, a la misma altura de los ojos
            //Vector3 menuPos = cam.transform.position
            //                  + forward * distanciaAlJugador;
            Vector3 forwardFlat = new Vector3(cam.transform.forward.x, 0f, cam.transform.forward.z).normalized;

            // Posicionar el raíz del menú (mismo patrón que FocusModeManager)
            transform.position = cam.transform.position + forwardFlat * distanciaAlJugador;
            transform.rotation = Quaternion.LookRotation(forwardFlat, Vector3.up);
            Canvas canvas = menuPanel.GetComponentInParent<Canvas>();
            //if (canvas != null)
            //{
            //    canvas.transform.position = menuPos;

            //    // Rotación puramente horizontal (sin inclinación en X)
            //    Vector3 lookDir = forward;
            //    canvas.transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
            //}
        }

        menuPanel.SetActive(true);
        if (motionController != null)
            motionController.enabled = false;
        IsOpen = true;
        Debug.Log("[ConnectionMenuUI] Botones creados: " + currentButtons.Count +
          ", selectedIndex: " + selectedIndex + ", IsOpen: " + IsOpen);
    }

    void HighlightButton(int index)
    {
        for (int i = 0; i < currentButtons.Count; i++)
        {
            Image bg = currentButtons[i].GetComponent<Image>();
            if (bg != null)
            {
                bg.color = (i == index) ? Color.yellow : Color.white;
            }

            //float scale = (i == index) ? 1.1f : 1.0f;
            //currentButtons[i].transform.localScale = Vector3.one * scale;
        }

        if (index >= 0 && index < currentButtons.Count)
        {
            EventSystem.current.SetSelectedGameObject(
                currentButtons[index].gameObject);
        }
    }

    void HandlePortSelection(CableSocket socket)
    {
        var cable = pendingCable;
        var callback = onPortSelected;
        Close();
        callback?.Invoke(cable, socket);
    }

    void HandleCancel()
    {
        Debug.Log("[ConnectionMenuUI] HandleCancel ejecutado");
        var callback = onCancelled;
        Close();
        callback?.Invoke();
    }

    public void CancelFromInput()
    {
        HandleCancel();
    }

    void Close()
    {
        Debug.Log("Close ejecutado");
        IsOpen = false;
        menuPanel.SetActive(false);
        currentButtons.Clear();
        pendingCable = null;
        onPortSelected = null;
        onCancelled = null;
        if (motionController != null)
            motionController.enabled = true;
    }
}