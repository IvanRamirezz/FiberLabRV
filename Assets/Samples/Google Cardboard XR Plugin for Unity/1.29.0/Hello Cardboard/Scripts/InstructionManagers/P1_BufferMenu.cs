using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class P1_BufferMenu : MonoBehaviour
{
    public static P1_BufferMenu Instance;

    // ══════════════════════════════════════════════════════════════════
    // REFERENCIAS
    // ══════════════════════════════════════════════════════════════════

    [Header("UI")]
    public GameObject menuPanel;
    public TMP_Text labelCintaMilar;   // "Cinta Milar 3 — Selecciona el color:"
    public Transform buttonContainer;   // VerticalLayoutGroup
    public GameObject buttonPrefab;      // mismo prefab que ConnectionMenuUI

    [Header("Input — igual que ConnectionMenuUI")]
    public string navigateAxis = "Vertical";
    public string submitButton = "Fire1";
    public string cancelButton = "Fire2";
    public float inputCooldown = 0.25f;
    public float navigationThreshold = 0.5f;

    [Header("Posición")]
    public float distanciaAlJugador = 0.2f;
    public float alturaOffset = 0.05f;

    [Header("Referencias")]
    public MotionObjectController motionController;
    // Campo privado:
    float inputCooldownTimer = 0f;
    // ══════════════════════════════════════════════════════════════════
    // ESTADO INTERNO
    // ══════════════════════════════════════════════════════════════════

    static readonly string[] COLOR_NAMES = {
        "Azul","Naranja","Verde","Café","Gris",
        "Blanco","Rojo","Negro","Amarillo","Violeta","Rosa","Aguamarina"
    };

    List<Button> currentButtons = new List<Button>();
    int selectedIndex = 0;
    float lastInputTime;
    int currentBufferIndex = 0;

    public bool IsOpen { get; private set; }

    // ══════════════════════════════════════════════════════════════════
    // AWAKE
    // ══════════════════════════════════════════════════════════════════

    void Awake()
    {
        Debug.Log($"[BufferMenu] Awake en GO: {gameObject.name}, Instance previa: {(Instance != null ? Instance.gameObject.name : "null")}");

        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        menuPanel.SetActive(false);
        IsOpen = false;
    }

    // ══════════════════════════════════════════════════════════════════
    // UPDATE
    // ══════════════════════════════════════════════════════════════════

    void Update()
    {
        if (!IsOpen) return;
        // Cooldown al abrir para evitar procesar input del mismo frame
        if (inputCooldownTimer > 0f)
        {
            inputCooldownTimer -= Time.deltaTime;
            return;
        }

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
                currentButtons[selectedIndex].onClick.Invoke();
        }

        if (Input.GetButtonDown(cancelButton))
            Close();
    }

    // ══════════════════════════════════════════════════════════════════
    // SHOW — llamado desde HandInteraction al apuntar a una Cinta Milar
    // ══════════════════════════════════════════════════════════════════

    //public void Show(int bufferIndex)
    //{
    //    currentBufferIndex = bufferIndex;
    //    labelCintaMilar.text = $"Cinta Milar {bufferIndex} — Selecciona el color:";

    //    ClearButtons();

    //    for (int i = 0; i < COLOR_NAMES.Length; i++)
    //    {
    //        int fiberIndex = i + 1;       // captura para lambda
    //        string colorName = COLOR_NAMES[i];

    //        GameObject btnGO = Instantiate(buttonPrefab, buttonContainer);
    //        btnGO.GetComponentInChildren<TMP_Text>().text = $"{fiberIndex}. {colorName}";

    //        Button btn = btnGO.GetComponent<Button>();
    //        btn.onClick.AddListener(() =>
    //        {
    //            P1_InstructionManager.Instance?.OnFiberSelected(currentBufferIndex, fiberIndex);
    //            Close();
    //        });
    //        currentButtons.Add(btn);
    //    }

    //    selectedIndex = 0;
    //    HighlightButton(0);
    //    PositionInFrontOfPlayer();

    //    menuPanel.SetActive(true);
    //    IsOpen = true;

    //    if (motionController != null)
    //        motionController.enabled = false;
    //}
    public void Show(int bufferIndex)
    {
        Debug.Log($"[BufferMenu] Show() llamado para búfer {bufferIndex}");

        currentBufferIndex = bufferIndex;
        labelCintaMilar.text = $"Cinta Milar {bufferIndex} — Selecciona el color:";
        Debug.Log("[BufferMenu] Label asignado");

        ClearButtons();
        Debug.Log("[BufferMenu] Botones limpiados");
        Debug.Log($"[BufferMenu] COLOR_NAMES.Length = {COLOR_NAMES.Length}");

        for (int i = 0; i < COLOR_NAMES.Length; i++)
        {
            int fiberIndex = i + 1;       // captura para lambda
            string colorName = COLOR_NAMES[i];

            GameObject btnGO = Instantiate(buttonPrefab, buttonContainer);
            btnGO.GetComponentInChildren<TMP_Text>().text = $"{fiberIndex}. {colorName}";

            Button btn = btnGO.GetComponent<Button>();
            Debug.Log($"[BufferMenu] btn = {btn}, GO = {btnGO.name}");

            btn.onClick.AddListener(() =>
            {
                Debug.Log($"[BufferMenu] onClick disparado — buf:{currentBufferIndex} fib:{fiberIndex}");

                P1_InstructionManager.Instance?.OnFiberSelected(currentBufferIndex, fiberIndex);
                Close();
            });
            currentButtons.Add(btn);
        }
        Debug.Log($"[BufferMenu] Botones creados: {currentButtons.Count}");
        // Botón cancelar
        GameObject cancelGO = Instantiate(buttonPrefab, buttonContainer);
        TMP_Text cancelLabel = cancelGO.GetComponentInChildren<TMP_Text>();
        if (cancelLabel != null) cancelLabel.text = "Salir";
        Button cancelBtn = cancelGO.GetComponent<Button>();
        cancelBtn.onClick.AddListener(Close);
        currentButtons.Add(cancelBtn);

        selectedIndex = 0;
        HighlightButton(0);
        PositionInFrontOfPlayer();
        Debug.Log("[BufferMenu] Posición asignada");

        menuPanel.SetActive(true);
        Debug.Log("[BufferMenu] Panel activado");
        Debug.Log($"[BufferMenu] Panel activeInHierarchy = {menuPanel.activeInHierarchy}, activeSelf = {menuPanel.activeSelf}");

        IsOpen = true;
        inputCooldownTimer = 0.3f;
        Debug.Log($"[BufferMenu] IsOpen = {IsOpen}");
        if (motionController != null)
        {
            motionController.enabled = false; Debug.LogWarning("[BufferMenu] motionController desactivado");

        }
        else
            Debug.LogWarning("[BufferMenu] motionController es null");
    }
    // ══════════════════════════════════════════════════════════════════
    // MÉTODOS LLAMADOS DESDE InstructionManager / HandInteraction
    // ══════════════════════════════════════════════════════════════════

    /// <summary>Activa o desactiva la posibilidad de abrir el menú.</summary>
    public void SetInteractable(bool active)
    {
        enabled = active;
        if (!active) Close();
    }

    /// <summary>Highlight de la Cinta Milar 3D — delega en Highlightable del GO correspondiente.</summary>
    public void SetHighlight(int bufferIndex, bool on)
    {
        // Busca el Highlightable en la escena por bufferIndex
        foreach (var h in FindObjectsOfType<P1_BufferIdentity>())
        {
            if (h.bufferIndex == bufferIndex)
                h.GetComponent<Highlightable>()?.Highlight(on);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // UTILIDADES
    // ══════════════════════════════════════════════════════════════════

    void ClearButtons()
    {
        for (int i = buttonContainer.childCount - 1; i >= 0; i--)
            Destroy(buttonContainer.GetChild(i).gameObject);
        currentButtons.Clear();
    }

    void HighlightButton(int index)
    {
        for (int i = 0; i < currentButtons.Count; i++)
        {
            Image bg = currentButtons[i].GetComponent<Image>();
            if (bg != null)
                bg.color = (i == index) ? Color.yellow : Color.white;
        }

        if (index >= 0 && index < currentButtons.Count)
            EventSystem.current.SetSelectedGameObject(
                currentButtons[index].gameObject);
    }

    void PositionInFrontOfPlayer()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 forwardFlat = new Vector3(
            cam.transform.forward.x, 0f, cam.transform.forward.z).normalized;

        transform.position = cam.transform.position
                             + forwardFlat * distanciaAlJugador
                             + Vector3.up * alturaOffset;

        transform.rotation = Quaternion.LookRotation(forwardFlat, Vector3.up);
    }

    void Close()
    {
        Debug.Log("[BufferMenu] Close() llamado desde: " + System.Environment.StackTrace);

        IsOpen = false;
        menuPanel.SetActive(false);
        ClearButtons();

        if (motionController != null)
            motionController.enabled = true;
    }
}