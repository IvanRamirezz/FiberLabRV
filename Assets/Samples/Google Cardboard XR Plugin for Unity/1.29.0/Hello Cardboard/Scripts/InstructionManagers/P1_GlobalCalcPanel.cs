using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class P1_GlobalCalcPanel : MonoBehaviour
{
    // ══════════════════════════════════════════════════════════════════
    // REFERENCIAS
    // ══════════════════════════════════════════════════════════════════
    public static P1_GlobalCalcPanel Instance;

    [Header("UI")]
    public GameObject menuPanel;
    public TMP_Text labelPregunta;   // "La fibra #N del cable..."
    public TMP_Text labelHint;       // "Búfer = ⌈N÷12⌉  |  Pos = N mod 12"
    public Transform buttonContainer; // VerticalLayoutGroup
    public GameObject buttonPrefab;    // mismo prefab que ConnectionMenuUI

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

    // ══════════════════════════════════════════════════════════════════
    // ESTADO INTERNO
    // ══════════════════════════════════════════════════════════════════

    static readonly string[] COLOR_NAMES = {
        "Azul","Naranja","Verde","Café","Gris",
        "Blanco","Rojo","Negro","Amarillo","Violeta","Rosa","Aguamarina"
    };

    // Fase de la pregunta: primero elige búfer, luego color
    enum CalcPhase { PickBuffer, PickColor }
    CalcPhase currentPhase;

    int correctBuffer;
    int correctFiber;
    int globalFiberNumber;
    int chosenBuffer; // guardado al confirmar la fase de búfer

    List<Button> currentButtons = new List<Button>();
    int selectedIndex = 0;
    float lastInputTime;

    public bool IsOpen { get; private set; }

    // ══════════════════════════════════════════════════════════════════
    // AWAKE
    // ══════════════════════════════════════════════════════════════════

    void Awake()
    {

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
    // SHOW — llamado desde InstructionManager
    // ══════════════════════════════════════════════════════════════════

    public void Show(int globalN, int correctBuffer, int correctFiber)
    {
        this.globalFiberNumber = globalN;
        this.correctBuffer = correctBuffer;
        this.correctFiber = correctFiber;

        labelHint.text = "Búfer = ⌈N ÷ 12⌉   |   Color = N mod 12";

        PositionInFrontOfPlayer();
        menuPanel.SetActive(true);
        IsOpen = true;

        if (motionController != null)
            motionController.enabled = false;

        ShowBufferPhase();
    }

    // ══════════════════════════════════════════════════════════════════
    // FASE 1: elegir búfer
    // ══════════════════════════════════════════════════════════════════

    void ShowBufferPhase()
    {
        currentPhase = CalcPhase.PickBuffer;

        labelPregunta.text =
            $"Fibra global número: {globalFiberNumber}\n" +
            "¿A qué Cinta Milar pertenece?";

        ClearButtons();

        for (int i = 1; i <= 5; i++)
        {
            int bufIdx = i; // captura para lambda
            GameObject btnGO = Instantiate(buttonPrefab, buttonContainer);
            btnGO.GetComponentInChildren<TMP_Text>().text = $"Cinta Milar {bufIdx}";

            Button btn = btnGO.GetComponent<Button>();
            btn.onClick.AddListener(() => OnBufferChosen(bufIdx));
            currentButtons.Add(btn);
        }

        selectedIndex = 0;
        HighlightButton(0);
    }

    void OnBufferChosen(int bufIdx)
    {
        chosenBuffer = bufIdx;
        ShowColorPhase();
    }

    // ══════════════════════════════════════════════════════════════════
    // FASE 2: elegir color
    // ══════════════════════════════════════════════════════════════════

    void ShowColorPhase()
    {
        currentPhase = CalcPhase.PickColor;

        labelPregunta.text =
            $"Fibra global número: {globalFiberNumber}\n" +
            $"Cinta Milar {chosenBuffer} — ¿Qué color tiene?";

        ClearButtons();

        for (int i = 0; i < COLOR_NAMES.Length; i++)
        {
            int fibIdx = i + 1; // captura para lambda
            string color = COLOR_NAMES[i];

            GameObject btnGO = Instantiate(buttonPrefab, buttonContainer);
            btnGO.GetComponentInChildren<TMP_Text>().text = $"{fibIdx}. {color}";

            Button btn = btnGO.GetComponent<Button>();
            btn.onClick.AddListener(() => OnColorChosen(fibIdx));
            currentButtons.Add(btn);
        }

        selectedIndex = 0;
        HighlightButton(0);
    }

    void OnColorChosen(int fibIdx)
    {
        Close();
        P1_InstructionManager.Instance?.OnGlobalAnswerSubmitted(chosenBuffer, fibIdx);
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
        IsOpen = false;
        menuPanel.SetActive(false);
        ClearButtons();

        if (motionController != null)
            motionController.enabled = true;
    }
}