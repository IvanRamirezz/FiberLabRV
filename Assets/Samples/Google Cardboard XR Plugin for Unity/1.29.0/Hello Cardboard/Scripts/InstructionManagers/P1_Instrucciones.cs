using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

public class P1_InstructionManager : MonoBehaviour
{
    public static P1_InstructionManager Instance;

    // ══════════════════════════════════════════════════════════════════
    // REFERENCIAS
    // ══════════════════════════════════════════════════════════════════

    //[Header("Búferes")]
    //public P1_BufferMenu[] bufferMenus; // 5 elementos, asignar en inspector

    [Header("UI — Instrucciones")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI feedbackText;    // texto temporal de correcto/incorrecto

    [Header("UI — Tabla TIA-598-C")]
    public GameObject panelTabla;

    [Header("UI — Cálculo Global")]
    public P1_GlobalCalcPanel panelCalculo; // script del panel de Fase 6

    [Header("Checkout")]
    public Transform playerTransform;
    public Transform checkoutPoint;
    public float checkoutRadius = 1.5f;
    public string escenaCuestionario = "Cuestionario_P1";

    // ══════════════════════════════════════════════════════════════════
    // CÓDIGO DE COLORES TIA-598-C
    // ══════════════════════════════════════════════════════════════════

    public static readonly string[] COLOR_NAMES = {
        "Azul","Naranja","Verde","Café","Gris",
        "Blanco","Rojo","Negro","Amarillo","Violeta","Rosa","Aguamarina"
    };

    // ══════════════════════════════════════════════════════════════════
    // ESTADO INTERNO
    // ══════════════════════════════════════════════════════════════════

    int currentStep = 0;
    bool waitingForFiberSelection = false;
    bool waitingForBufferSelection = false;
    bool waitingForCalcAnswer = false;
    bool selectionCorrect = false;

    int targetBuffer = 0;
    int targetFiber = 0;

    // Puntuación acumulada
    int correctPhase4 = 0;
    int correctPhase5 = 0;
    int correctPhase6 = 0;

    bool checkoutReached = false;

    // ══════════════════════════════════════════════════════════════════
    // AWAKE / START
    // ══════════════════════════════════════════════════════════════════

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        panelTabla.SetActive(false);
        panelCalculo.gameObject.SetActive(false);
        feedbackText.text = "";
        SetBufferMenusActive(false);

        StartCoroutine(RunStep1_Exploracion());
    }

    // ══════════════════════════════════════════════════════════════════
    // UPDATE — checkout polling
    // ══════════════════════════════════════════════════════════════════

    void Update()
    {
        if (currentStep == 6 && !checkoutReached)
        {
            float dist = Vector3.Distance(playerTransform.position, checkoutPoint.position);
            if (dist <= checkoutRadius)
            {
                checkoutReached = true;
                StartCoroutine(RunCheckout());
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // PASO 1 — Exploración automática del cable
    // ══════════════════════════════════════════════════════════════════

    IEnumerator RunStep1_Exploracion()
    {
        currentStep = 1;
        SetBufferMenusActive(false);

        instructionText.text =
            "Bienvenido a la Práctica 1.\n" +
            "Observa el cable de fibra óptica frente a ti.\n" +
            "Está compuesto por 5 cintas millar, cada una con 12 fibras.";
        yield return new WaitForSeconds(5f);

        // Highlight secuencial de cada búfer
        for (int i = 0; i < 5; i++)
        {
            P1_BufferMenu.Instance.SetHighlight(i,true);
            instructionText.text =
                $"Cinta Milar {i + 1}:\n" +
                $"Contiene 12 fibras que siguen el código de colores TIA/EIA-598-C.\n" +
                $"Cada fibra tiene un color único dentro del búfer.";
            yield return new WaitForSeconds(3.5f);
            P1_BufferMenu.Instance.SetHighlight(i, false);
            yield return new WaitForSeconds(0.3f);
        }

        instructionText.text =
            "Los 12 colores del estándar, en orden, son:\n" +
            "Azul · Naranja · Verde · Café · Gris · Blanco\n" +
            "Rojo · Negro · Amarillo · Violeta · Rosa · Aguamarina";
        yield return new WaitForSeconds(5f);

        StartCoroutine(RunStep2_IdentificacionBuffers());
    }

    // ══════════════════════════════════════════════════════════════════
    // PASO 2 — Identificación de búferes (×5)
    // ══════════════════════════════════════════════════════════════════

    IEnumerator RunStep2_IdentificacionBuffers()
    {
        currentStep = 2;
        SetBufferMenusActive(true);

        instructionText.text =
            "Ahora identificarás cada Cinta Milar.\n" +
            "Apunta a la que te indique el sistema y presiona el botón.";
        yield return new WaitForSeconds(3f);

        int[] order = ShuffledRange(0, 4); // índices 0–4

        foreach (int idx in order)
        {
            int bufferNum = idx + 1;
            instructionText.text = $"Selecciona la Cinta Milar número {bufferNum}.";

            waitingForBufferSelection = true;
            targetBuffer = bufferNum;
            yield return new WaitUntil(() => !waitingForBufferSelection);

            yield return new WaitForSeconds(0.8f);
        }

        instructionText.text = "¡Bien! Reconoces todas las cintas millar.";
        yield return new WaitForSeconds(2f);

        // En esta fase el menú no se usa — solo selección directa del búfer
        SetBufferMenusActive(false);

        StartCoroutine(RunStep3_IntroColores());
    }

    // ══════════════════════════════════════════════════════════════════
    // PASO 3 — Intro al código de colores con tabla (×12, búfer 1)
    // ══════════════════════════════════════════════════════════════════

    IEnumerator RunStep3_IntroColores()
    {
        currentStep = 3;
        panelTabla.SetActive(true);
        SetBufferMenusActive(true);

        instructionText.text =
            "Ahora aprenderás el código de colores.\n" +
            "Usaremos la Cinta Milar 1 como referencia.\n" +
            "La tabla de colores está visible. Consulta cada fibra.";
        yield return new WaitForSeconds(4f);

        for (int fib = 1; fib <= 12; fib++)
        {
            string colorName = COLOR_NAMES[fib - 1];
            instructionText.text =
                $"Fibra {fib} de la Cinta Milar 1: {colorName}\n" +
                $"Abre el menú de la Cinta Milar 1 y selecciona '{colorName}'.";

            waitingForFiberSelection = true;
            targetBuffer = 1;
            targetFiber = fib;
            yield return new WaitUntil(() => !waitingForFiberSelection);

            // Sustituye la llamada problemática en RunStep3_IntroColores:
            if (selectionCorrect)
                ShowFeedback(true, $"Correcto — Fibra {fib}: {colorName}");
            // Si es incorrecto, OnFiberSelected ya muestra el feedback,
            // así que aquí no hace falta hacer nada más.
            yield return new WaitForSeconds(0.8f);
        }

        panelTabla.SetActive(false);
        instructionText.text =
            "Has recorrido los 12 colores del estándar TIA/EIA-598-C.\n" +
            "Ahora practicarás con todos los búferes.";
        yield return new WaitForSeconds(3f);

        StartCoroutine(RunStep4_PracticaConTabla());
    }

    // ══════════════════════════════════════════════════════════════════
    // PASO 4 — Práctica guiada con tabla (×4)
    // ══════════════════════════════════════════════════════════════════

    IEnumerator RunStep4_PracticaConTabla()
    {
        currentStep = 4;
        panelTabla.SetActive(true);
        SetBufferMenusActive(true);
        correctPhase4 = 0;

        instructionText.text =
            "Práctica guiada.\n" +
            "Busca la fibra que te pida el sistema en cualquier búfer.\n" +
            "La tabla de colores sigue disponible.";
        yield return new WaitForSeconds(3f);

        var questions = GenerateRandomQuestions(4);
        foreach (var (buf, fib) in questions)
        {
            string color = COLOR_NAMES[fib - 1];
            instructionText.text =
                $"Encuentra el hilo de color {color}\n" +
                $"dentro de la Cinta Milar {buf}.";

            waitingForFiberSelection = true;
            targetBuffer = buf;
            targetFiber = fib;
            yield return new WaitUntil(() => !waitingForFiberSelection);

            if (selectionCorrect) correctPhase4++;
            yield return new WaitForSeconds(0.8f);
        }

        panelTabla.SetActive(false);
        instructionText.text =
            $"Fase completada: {correctPhase4}/4 respuestas correctas.";
        yield return new WaitForSeconds(2.5f);

        StartCoroutine(RunStep5_PruebaSinTabla());
    }

    // ══════════════════════════════════════════════════════════════════
    // PASO 5 — Prueba sin tabla (×5)
    // ══════════════════════════════════════════════════════════════════

    IEnumerator RunStep5_PruebaSinTabla()
    {
        currentStep = 5;
        panelTabla.SetActive(false);
        SetBufferMenusActive(true);
        correctPhase5 = 0;

        instructionText.text =
            "Prueba sin tabla de referencia.\n" +
            "Identifica las fibras por memoria.\n" +
            "¡Confía en lo que aprendiste!";
        yield return new WaitForSeconds(3.5f);

        var questions = GenerateRandomQuestions(5);
        foreach (var (buf, fib) in questions)
        {
            string color = COLOR_NAMES[fib - 1];
            instructionText.text =
                $"Encuentra el hilo de color {color}\n" +
                $"dentro de la Cinta Milar {buf}.";

            waitingForFiberSelection = true;
            targetBuffer = buf;
            targetFiber = fib;
            yield return new WaitUntil(() => !waitingForFiberSelection);

            if (selectionCorrect) correctPhase5++;
            yield return new WaitForSeconds(0.8f);
        }

        SetBufferMenusActive(false);
        instructionText.text =
            $"Muy bien — {correctPhase5}/5 respuestas correctas.\n" +
            "Ahora el último ejercicio: cálculo de posición global.";
        yield return new WaitForSeconds(3f);

        StartCoroutine(RunStep6_CalculoGlobal());
    }

    // ══════════════════════════════════════════════════════════════════
    // PASO 6 — Cálculo de posición global (×4)
    // ══════════════════════════════════════════════════════════════════

    IEnumerator RunStep6_CalculoGlobal()
    {
        currentStep = 6;
        correctPhase6 = 0;

        instructionText.text =
            "Cálculo de posición global.\n" +
            "Recuerda: Fibra global N → Búfer = ⌈N/12⌉, Color = posición dentro del búfer.";
        yield return new WaitForSeconds(5f);

        int[] globals = ShuffledRange(1, 60);

        for (int i = 0; i < 4; i++)
        {
            int globalN = globals[i];
            int bufIdx = ((globalN - 1) / 12) + 1;
            int fibIdx = ((globalN - 1) % 12) + 1;
            string color = COLOR_NAMES[fibIdx - 1];

            instructionText.text =
                $"La fibra número {globalN} del cable...\n" +
                $"¿A qué Cinta Milar pertenece y qué color tiene?";
            yield return new WaitForSeconds(2f);

            waitingForCalcAnswer = true;
            panelCalculo.Show(globalN, correctBuffer: bufIdx, correctFiber: fibIdx);
            yield return new WaitUntil(() => !waitingForCalcAnswer);

            if (selectionCorrect) correctPhase6++;
            yield return new WaitForSeconds(0.8f);
        }

        panelCalculo.gameObject.SetActive(false);

        int total = correctPhase4 + correctPhase5 + correctPhase6;
        instructionText.text =
            $"Práctica completada.\n" +
            $"Resultados: Guiada {correctPhase4}/4 · Sin tabla {correctPhase5}/5 · Global {correctPhase6}/4\n" +
            "Dirígete al punto de salida para continuar.";

        // Update() detecta llegada al checkoutPoint
    }

    // ══════════════════════════════════════════════════════════════════
    // CHECKOUT
    // ══════════════════════════════════════════════════════════════════

    IEnumerator RunCheckout()
    {
        instructionText.text = "Cargando cuestionario final...";
        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene(escenaCuestionario);
    }

    // ══════════════════════════════════════════════════════════════════
    // HANDLERS — llamados desde P1_BufferMenu y P1_GlobalCalcPanel
    // ══════════════════════════════════════════════════════════════════

    /// <summary>Llamado cuando el alumno selecciona un hilo del menú de búfer.</summary>
    public void OnFiberSelected(int bufferIndex, int fiberIndex)
    {
        Debug.Log($"[IM] OnFiberSelected llamado — buf:{bufferIndex} fib:{fiberIndex} step:{currentStep} waitingBuffer:{waitingForBufferSelection} waitingFiber:{waitingForFiberSelection}");

        // Fase 2: solo se evalúa el búfer, no la fibra
        if (currentStep == 2 && waitingForBufferSelection)
        {
            bool correct = bufferIndex == targetBuffer;
            selectionCorrect = correct;

            if (correct)
            {
                ShowFeedback(true, $"¡Correcto! Esa es la Cinta Milar {targetBuffer}.");
                waitingForBufferSelection = false;
            }
            else
            {
                ShowFeedback(false, $"Esa es la Cinta Milar {bufferIndex}. Busca la número {targetBuffer}.");
            }
            return;
        }

        // Fases 3, 4 y 5: búfer + fibra
        if (waitingForFiberSelection)
        {
            bool correct = (bufferIndex == targetBuffer && fiberIndex == targetFiber);
            selectionCorrect = correct;

            if (correct)
            {
                string color = COLOR_NAMES[fiberIndex - 1];
                ShowFeedback(true, $"¡Correcto! Fibra {fiberIndex}: {color}.");
                waitingForFiberSelection = false;
            }
            else
            {
                string colorElegido = COLOR_NAMES[fiberIndex - 1];
                ShowFeedback(false,
                    $"Incorrecto — elegiste {colorElegido} en Cinta Milar {bufferIndex}.\n" +
                    "Intenta de nuevo.");
            }
        }
    }

    /// <summary>Llamado desde P1_GlobalCalcPanel cuando el alumno confirma su respuesta.</summary>
    public void OnGlobalAnswerSubmitted(int bufferAnswer, int fiberAnswer)
    {
        if (!waitingForCalcAnswer) return;

        bool correct = (bufferAnswer == targetBuffer && fiberAnswer == targetFiber);
        selectionCorrect = correct;

        string colorCorrect = COLOR_NAMES[targetFiber - 1];
        string colorElegido = COLOR_NAMES[fiberAnswer - 1];

        if (correct)
            ShowFeedback(true,
                $"¡Correcto! Cinta Milar {targetBuffer}, fibra {colorCorrect}.");
        else
            ShowFeedback(false,
                $"La respuesta correcta era Cinta Milar {targetBuffer}, color {colorCorrect}.");

        waitingForCalcAnswer = false;
    }

    // ══════════════════════════════════════════════════════════════════
    // UTILIDADES
    // ══════════════════════════════════════════════════════════════════

    void SetBufferMenusActive(bool active)
    {
        if (P1_BufferMenu.Instance != null)
            P1_BufferMenu.Instance.SetInteractable(active);
    }

    void ShowFeedback(bool correct, string message)
    {
        feedbackText.color = correct ? Color.green : new Color(1f, 0.4f, 0f);
        feedbackText.text = message;
        StartCoroutine(ClearFeedback(2.5f));
    }

    IEnumerator ClearFeedback(float delay)
    {
        yield return new WaitForSeconds(delay);
        feedbackText.text = "";
    }

    int[] ShuffledRange(int min, int max)
    {
        var list = Enumerable.Range(min, max - min + 1).ToList();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list.ToArray();
    }

    (int buf, int fib)[] GenerateRandomQuestions(int count)
    {
        var pool = new List<(int, int)>();
        for (int b = 1; b <= 5; b++)
            for (int f = 1; f <= 12; f++)
                pool.Add((b, f));

        return pool.OrderBy(_ => Random.Range(0f, 1f))
                   .Take(count)
                   .ToArray();
    }
}