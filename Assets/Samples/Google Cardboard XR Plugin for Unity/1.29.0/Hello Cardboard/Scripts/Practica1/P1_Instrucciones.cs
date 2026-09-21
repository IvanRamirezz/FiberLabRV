using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;
using TMPro;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

public class P1_InstructionManager : MonoBehaviour
{
    public static P1_InstructionManager Instance;

    int _lastTouchPressId = -10;

    // ══════════════════════════════════════════════════════════════════
    // REFERENCIAS
    // ══════════════════════════════════════════════════════════════════

    [Header("UI — Instrucciones")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI feedbackText;

    [Header("Paso 1 — Partes del cable")]
    public Highlightable   vainaHighlight;
    public Highlightable   armaduraHighlight;
    public Highlightable[] cintaMilarHighlights;
    public Highlightable   nucleoHighlight;
    public Highlightable[] extrasToGray;

    [Header("Paso 3 — Intro búferes")]
    public GameObject panelTabla;

    [Header("Paso 5 — Quiz fibras")]
    public GameObject panelRespuestas;   // Panel con los 4 botones de color
    public Button[]   botonesColor;      // Exactamente 4 botones dentro del panel

    [Header("Paso 7 — Checkout")]
    public Transform     exitPoint;
    public float         exitRadius    = 2f;
    public GameObject    exitIndicator;
    public Highlightable pcHighlight;
    public string     escenaCuestionario = "Feedback";
    public SupabaseConfig supabaseConfig;

    [Header("Mesa 1 — Inicio de práctica")]
    public Transform   mesa1Point;
    public float       mesa1Radius = 2f;
    public Transform   playerTransform;
    public Highlightable mesa1Highlight;

    // ══════════════════════════════════════════════════════════════════
    // CÓDIGO DE COLORES TIA/EIA-598-C
    // ══════════════════════════════════════════════════════════════════

    // ══════════════════════════════════════════════════════════════════
    // ESTADO INTERNO
    // ══════════════════════════════════════════════════════════════════

    int  currentStep = 0;

    P1_DatosRepository repository;
    P1_Logica logica;

    // Scores por paso (usados en el resumen del Paso 7)
    int scoreStep2 = 0;
    int scoreStep5 = 0;
    int scoreStep6 = 0;

    // Paso 2 — Identificación de partes del cable
    bool          waitingForCablePartSelection = false;
    CablePartType targetCablePart;
    P1_CablePart  _lastHoveredPart;

    // ══════════════════════════════════════════════════════════════════
    // AWAKE / START
    // ══════════════════════════════════════════════════════════════════

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        repository = new P1_DatosRepository(supabaseConfig);
        logica = new P1_Logica();
    }

    void Start()
    {
        feedbackText.text = "";
        if (panelTabla     != null) panelTabla.SetActive(false);
        if (panelRespuestas != null) panelRespuestas.SetActive(false);
        StartCoroutine(RunStep0_AcercateMesa());
    }

    // ══════════════════════════════════════════════════════════════════
    // UPDATE
    // ══════════════════════════════════════════════════════════════════

    void Update()
    {
        if (currentStep == 2 && waitingForCablePartSelection)
            HandleCablePartSelection();
    }

    void HandleCablePartSelection()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        P1_CablePart hovered = null;
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, 15f))
            hovered = hit.collider.GetComponentInParent<P1_CablePart>();

        if (hovered != _lastHoveredPart)
        {
            _lastHoveredPart?.SetHovered(false);
            hovered?.SetHovered(true);
            _lastHoveredPart = hovered;
        }

        // Reticle: verde cuando apunta a una parte del cable
        HandInteraction.ReticleOverride = hovered != null
            ? Color.green
            : (Color?)null;

        if (hovered != null && TouchInput.ButtonDown("Fire1", ref _lastTouchPressId))
        {
            bool correct = (hovered.partType == targetCablePart);
            if (correct)
            {
                hovered.SetHovered(false);
                _lastHoveredPart = null;
                HandInteraction.ReticleOverride = null;
                ShowFeedback(true, $"¡Correcto! Esa es la {GetNombreParte(hovered.partType)}.");
                scoreStep2++;
                waitingForCablePartSelection = false;
            }
            else
            {
                ShowFeedback(false,
                    $"Eso es {GetNombreParte(hovered.partType)}. Busca la {GetNombreParte(targetCablePart)}.");
            }
        }
    }

    static string GetNombreParte(CablePartType tipo) => tipo switch
    {
        CablePartType.Vaina      => "la Vaina",
        CablePartType.Armadura   => "la Armadura",
        CablePartType.CintaMilar => "la Cinta Milar",
        CablePartType.Nucleo     => "el Núcleo",
        _                        => tipo.ToString()
    };

    // ══════════════════════════════════════════════════════════════════
    // INPUT HELPER
    // ══════════════════════════════════════════════════════════════════

    // Espera hasta que el jugador pulse el botón del control o haga clic/tap
    IEnumerator WaitForConfirm()
    {
        yield return new WaitUntil(() =>
            TouchInput.ButtonDown("Fire1", ref _lastTouchPressId));
        // Skip one frame so GetButtonDown resets before the next WaitForConfirm check
        yield return null;
    }

    // ══════════════════════════════════════════════════════════════════
    // PASO 0 — Acercarse a la mesa e iniciar práctica
    // ══════════════════════════════════════════════════════════════════

    IEnumerator RunStep0_AcercateMesa()
    {
        currentStep = 0;
        instructionText.text =
            "Dirígete hacia la <b>Mesa 1</b> para comenzar la práctica.\n\n" +
            "<size=70%>Observa alrededor de tu entorno — la mesa iluminada en amarillo\n" +
            "indica el lugar. Mira hacia arriba para leer las instrucciones.</size>";

        if (mesa1Highlight != null) mesa1Highlight.Highlight(true);

        yield return new WaitUntil(() =>
            mesa1Point != null &&
            Vector3.Distance(playerTransform.position, mesa1Point.position) <= mesa1Radius
        );

        if (mesa1Highlight != null) mesa1Highlight.Highlight(false);

        Time.timeScale = 0f;
        instructionText.text = "Pulsa el botón start para iniciar la práctica.";

        yield return StartCoroutine(WaitForConfirm());

        instructionText.text = "";
        Time.timeScale = 1f;

        StartCoroutine(RunStep1_Exploracion());
    }

    // ══════════════════════════════════════════════════════════════════
    // PASO 1 — Exploración guiada por partes
    // ══════════════════════════════════════════════════════════════════

    bool IsLookingAtCable()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, 15f))
            return hit.collider.GetComponentInParent<P1_CablePart>() != null;
        return false;
    }

    IEnumerator RunStep1_Exploracion()
    {
        currentStep = 1;

        instructionText.text =
            "Vamos a comenzar explicando las partes de la fibra óptica.\n" +
            "Observa el cable para continuar.";
        yield return new WaitUntil(() => IsLookingAtCable());
        yield return new WaitForSeconds(0.5f);

        instructionText.text =
            "A continuación se mostrarán las partes de la fibra óptica pintadas \nde amarillo." +
            "Lee con atención, ya que se te hará una prueba.\n\n" +
            "<size=70%>Pulsa el botón start del control para comenzar.</size>";
        yield return StartCoroutine(WaitForConfirm());

        yield return StartCoroutine(MostrarParte(
            "VAINA\nCubierta exterior del cable. \nCapa protectora que resiste golpes, humedad y rayos UV.\"",
            vainaHighlight));

        yield return StartCoroutine(MostrarParte(
            "ARMADURA\nMiembro de fuerza (FRP). \nAbsorbe las tensiones mecánicas durante la instalación, \nevitando que las fibras se estiren o rompan.",
            armaduraHighlight));

        yield return StartCoroutine(MostrarParte(
            "BÚFERES O CINTAS MILARES\nTubos que agrupan y protegen las fibras individuales. \nEste cable tiene 5 búferes.",
            cintaMilarHighlights));

        yield return StartCoroutine(MostrarParte(
            "NÚCLEO\nElemento central de cada fibra. \nElemento de soporte que mantiene la geometría del cable \ny sobre el que se enrollan los búferes.",
            nucleoHighlight));

        StartCoroutine(RunStep2_IdentificacionPartes());
    }

    IEnumerator MostrarParte(string descripcion, params Highlightable[] toHighlight)
    {
        foreach (var h in AllCableHighlights())
            if (h != null && System.Array.IndexOf(toHighlight, h) < 0) h.GrayOut(true);

        foreach (var h in toHighlight)
            if (h != null) h.Highlight(true);

        instructionText.text = descripcion + "\n\n<size=70%>Pulsa el botón start del control para continuar.</size>";

        yield return StartCoroutine(WaitForConfirm());

        foreach (var h in toHighlight)
            if (h != null) h.Highlight(false);

        foreach (var h in AllCableHighlights())
            if (h != null) h.GrayOut(false);
    }

    IEnumerable<Highlightable> AllCableHighlights()
    {
        yield return vainaHighlight;
        yield return armaduraHighlight;
        if (cintaMilarHighlights != null)
            foreach (var h in cintaMilarHighlights) yield return h;
        yield return nucleoHighlight;
        if (extrasToGray != null)
            foreach (var h in extrasToGray) yield return h;
    }

    // ══════════════════════════════════════════════════════════════════
    // PASO 2 — Identificación de partes del cable (gaze + Fire1)
    // ══════════════════════════════════════════════════════════════════

    IEnumerator RunStep2_IdentificacionPartes()
    {
        currentStep = 2;

        var orden = new[] {
            CablePartType.Vaina, CablePartType.Armadura,
            CablePartType.CintaMilar, CablePartType.Nucleo
        }.OrderBy(_ => Random.Range(0f, 1f)).ToArray();

        instructionText.text =
            "Ahora identifica cada parte del cable cuando el sistema te la pida.\n" +
            "Mira la parte indicada y pulsa el botón del control.";
        yield return new WaitForSeconds(3f);

        foreach (var tipo in orden)
        {
            targetCablePart = tipo;
            instructionText.text = $"Mira y selecciona {GetNombreParte(tipo)} del cable.";
            waitingForCablePartSelection = true;
            yield return new WaitUntil(() => !waitingForCablePartSelection);
            yield return new WaitForSeconds(0.8f);
        }

        instructionText.text = "¡Muy bien! Identificaste todas las partes del cable. \n" +
            "Ahora aprenderás cómo se organizan las fibras dentro de él.";
        yield return new WaitForSeconds(2f);
        StartCoroutine(RunStep3_IntroBuffers());
    }

    // ══════════════════════════════════════════════════════════════════
    // PASO 3 — Introducción a los búferes y tabla de colores
    // ══════════════════════════════════════════════════════════════════

    IEnumerator RunStep3_IntroBuffers()
    {
        currentStep = 3;

        // — Intro fase 2 —
        instructionText.text =
            "Has pasado a la Fase 2.\n\n" +
            "<size=70%>Pulsa el botón start del control para continuar.</size>";
        yield return StartCoroutine(WaitForConfirm());

        instructionText.text =
            "A continuación se te explicará sobre los búferes \ndel cable de fibra óptica." +
            "Por favor presta atención en las partes \nque se pintan de amarillo.\n\n" +
            "<size=70%>Pulsa el botón start del control para continuar.</size>";
        yield return StartCoroutine(WaitForConfirm());

        // — Mensaje 1: los 5 búferes encendidos, resto en gris —
        instructionText.text =
            "El cable de fibra óptica está compuesto por múltiples búferes.\n" +
            "En este caso tienes 5 búferes (Cintas Milares).\n\n" +
            "<size=70%>Pulsa el botón start del control para continuar.</size>";
        foreach (var h in AllCableHighlights()) if (h != null) h.GrayOut(true);
        SetAllBuffersHighlight(true);

        yield return StartCoroutine(WaitForConfirm());
        SetAllBuffersHighlight(false);
        foreach (var h in AllCableHighlights()) if (h != null) h.GrayOut(false);

        // — Mensaje 2: los 5 búferes siguen encendidos, resto en gris —
        instructionText.text =
            "Cada búfer contiene 12 fibras individuales.\n" +
            "En cables más grandes puede haber 12, 24 o más búferes.\n\n" +
            "<size=70%>Pulsa el botón del control para continuar.</size>";
        foreach (var h in AllCableHighlights()) if (h != null) h.GrayOut(true);
        SetAllBuffersHighlight(true);

        yield return StartCoroutine(WaitForConfirm());
        SetAllBuffersHighlight(false);
        foreach (var h in AllCableHighlights()) if (h != null) h.GrayOut(false);

        // — Mensaje 3: aparece la tabla de colores —
        instructionText.text =
            "Para identificar cada fibra existe el estándar TIA/EIA-598-C,\n" +
            "que asigna un color único a cada posición.\n\n" +
            "<size=70%>Pulsa el botón start del control para continuar.</size>";
        if (panelTabla != null) panelTabla.SetActive(true);

        yield return StartCoroutine(WaitForConfirm());

        // — Mensaje 4: tabla permanece, pausa larga —
        instructionText.text =
            "Tómate un momento para observar la tabla.\n" +
            "Cada color corresponde a una posición específica dentro del búfer.\n\n" +
            "<size=70%>Pulsa el botón del control para continuar.</size>";

        yield return StartCoroutine(WaitForConfirm());
        // panelTabla permanece visible el resto de la práctica

        StartCoroutine(RunStep4_ExploracionFibras());
    }

    // ══════════════════════════════════════════════════════════════════
    // UTILIDADES
    // ══════════════════════════════════════════════════════════════════

    void SetBufferHighlight(int bufferIndex, bool on)
    {
        foreach (var id in FindObjectsByType<P1_BufferIdentity>(FindObjectsSortMode.None))
            if (id.bufferIndex == bufferIndex)
                id.GetComponent<Highlightable>()?.Highlight(on);
    }

    void SetAllBuffersHighlight(bool on)
    {
        foreach (var id in FindObjectsByType<P1_BufferIdentity>(FindObjectsSortMode.None))
            id.GetComponent<Highlightable>()?.Highlight(on);
    }

    void SetFiberHighlight(int bufferIndex, int fiberPosition, bool on)
    {
        foreach (var f in FindObjectsByType<P1_FiberIdentity>(FindObjectsSortMode.None))
            if (f.bufferIndex == bufferIndex && f.fiberPosition == fiberPosition)
                f.highlightable?.Highlight(on);
    }

    // ══════════════════════════════════════════════════════════════════
    // PASO 4 — Exploración de fibras del Búfer 1
    // ══════════════════════════════════════════════════════════════════

    IEnumerator RunStep4_ExploracionFibras()
    {
        currentStep = 4;

        instructionText.text =
            "Ahora observa las fibras del Búfer 1.\n" +
            "El sistema las iluminará una por una con su color y posición \nsegún TIA/EIA-598-C.\n\n" +
            "<size=70%>Pulsa el botón start del control para continuar.</size>";
        yield return StartCoroutine(WaitForConfirm());

        for (int i = 0; i < 12; i++)
        {
            int    pos   = i + 1;
            string color = P1_Logica.COLOR_NAMES[i];

            // Gris en todo — partes del cable y demás fibras
            foreach (var h in AllCableHighlights()) if (h != null) h.GrayOut(true);
            foreach (var f in FindObjectsByType<P1_FiberIdentity>(FindObjectsSortMode.None))
                if (f.highlightable != null) f.highlightable.GrayOut(true);

            // Ilumina solo la fibra actual (sobreescribe su gris)
            SetFiberHighlight(0, pos, true);

            instructionText.text =
                $"Fibra #{pos} — {color}\n" +
                $"Posición {pos} dentro del Búfer 1.\n\n" +
                "<size=70%>Pulsa el botón del control para continuar.</size>";

            yield return StartCoroutine(WaitForConfirm());

            SetFiberHighlight(0, pos, false);
            foreach (var f in FindObjectsByType<P1_FiberIdentity>(FindObjectsSortMode.None))
                if (f.highlightable != null) f.highlightable.GrayOut(false);
            foreach (var h in AllCableHighlights()) if (h != null) h.GrayOut(false);
        }

        instructionText.text =
            "Has visto las 12 fibras del Búfer 1 y sus colores.\n" +
            "Este mismo patrón se repite en cada búfer del cable.\n\n" +
            "<size=70%>Pulsa el botón del control para continuar.</size>";
        yield return StartCoroutine(WaitForConfirm());

        StartCoroutine(RunStep5_QuizFibras());
    }

    // ══════════════════════════════════════════════════════════════════
    // PASO 5 — Quiz: encuentra la fibra #X del Búfer Y
    // ══════════════════════════════════════════════════════════════════

    static readonly Color BTN_NORMAL  = new Color(0.20f, 0.40f, 0.70f, 1f);
    static readonly Color BTN_HOVERED = new Color(1.00f, 0.85f, 0.00f, 1f);

    IEnumerator RunStep5_QuizFibras()
    {
        currentStep = 5;
        if (panelRespuestas != null) panelRespuestas.SetActive(false);

        instructionText.text =
            "Has pasado a la Fase 3.\n\n" +
            "<size=70%>Pulsa el botón del control para continuar.</size>";
        yield return StartCoroutine(WaitForConfirm());

        instructionText.text =
            "Ahora pondrás a prueba lo aprendido.\n" +
            "Mira el color correcto en el panel y pulsa el botón del control.\n\n" +
            "<size=70%>Pulsa el botón del control para comenzar.</size>";
        yield return StartCoroutine(WaitForConfirm());

        const int totalPreguntas = 4;
        int correctas = 0;
        var preguntas = logica.GenerarPreguntasQuiz(totalPreguntas);

        foreach (var (bufferIdx, fibraPos) in preguntas)
        {
            string colorCorrecto = P1_Logica.COLOR_NAMES[fibraPos - 1];
            int    bufferNum     = bufferIdx + 1;

            foreach (var h in AllCableHighlights()) if (h != null) h.GrayOut(true);
            SetBufferHighlight(bufferIdx, true);

            string[] opciones = logica.GenerarOpcionesColor(colorCorrecto);
            for (int i = 0; i < botonesColor.Length; i++)
                botonesColor[i].GetComponentInChildren<TextMeshProUGUI>().text = opciones[i];

            PosicionarPanelFrenteAlJugador();
            if (panelRespuestas != null) panelRespuestas.SetActive(true);

            instructionText.text =
                $"Encuentra la fibra #{fibraPos} del Búfer {bufferNum}.\n" +
                "Mira el color correcto y pulsa el botón del control.";

            string respuesta = null;
            yield return StartCoroutine(WaitForQuizSelection(opciones, r => respuesta = r));

            if (panelRespuestas != null) panelRespuestas.SetActive(false);
            SetBufferHighlight(bufferIdx, false);
            foreach (var h in AllCableHighlights()) if (h != null) h.GrayOut(false);

            bool acierto = logica.EvaluarRespuesta(respuesta, colorCorrecto);
            if (acierto) correctas++;
            ShowFeedback(acierto,
                acierto
                    ? $"¡Correcto! Fibra #{fibraPos} = {colorCorrecto}."
                    : $"Incorrecto. Fibra #{fibraPos} = {colorCorrecto}, no {respuesta}.");

            yield return new WaitForSeconds(1.8f);
        }

        scoreStep5 = correctas;

        instructionText.text =
            $"Quiz completado: {correctas}/{totalPreguntas} respuestas correctas.\n\n" +
            "<size=70%>Pulsa el botón del control para continuar.</size>";
        yield return StartCoroutine(WaitForConfirm());

        StartCoroutine(RunStep6_QuizGlobal());
    }

    // ══════════════════════════════════════════════════════════════════
    // PASO 6 — Cálculo de posición global
    // ══════════════════════════════════════════════════════════════════

    IEnumerator RunStep6_QuizGlobal()
    {
        currentStep = 6;
        if (panelRespuestas != null) panelRespuestas.SetActive(false);

        // ── Intro 1 ────────────────────────────────────────────────
        instructionText.text =
            "En campo no siempre sabes en qué búfer está una fibra.\n" +
            "Solo tienes el número global.\n" +
            "Este cable tiene <b>60 fibras</b> en total, numeradas del 1 al 60.\n\n" +
            "<size=70%>Pulsa el botón del control para continuar.</size>";
        yield return StartCoroutine(WaitForConfirm());

        // ── Intro 2: fórmula ───────────────────────────────────────
        instructionText.text =
            "Para encontrar cualquier fibra usas esta fórmula:\n\n" +
            "<color=#FFD700><b>Paso 1 — ¿En qué búfer está?</b></color>\n" +
            "Búfer  =  N  ÷  12  (redondea hacia arriba)\n\n" +
            "<color=#FFD700><b>Paso 2 — ¿En qué posición dentro del búfer?</b></color>\n" +
            "Posición  =  N  −  ( Búfer − 1 ) × 12\n\n" +
            "<size=65%>Pulsa el botón del control para continuar.</size>";
        yield return StartCoroutine(WaitForConfirm());

        // ── Ejemplo 1: Fibra #14 → Búfer 2 (idx 1), pos 2 = Naranja
        foreach (var h in AllCableHighlights()) if (h != null) h.GrayOut(true);
        SetBufferHighlight(1, true);
        SetFiberHighlight(1, 2, true);

        instructionText.text =
            "<b>Ejemplo: Fibra global #14</b>\n\n" +
            "<color=#FFD700>Paso 1:</color>  14 ÷ 12 = 1.16  →  redondea a  <b>2</b>  →  Cinta Milar 2\n\n" +
            "<color=#FFD700>Paso 2:</color>  14 − ( 2 − 1 ) × 12  =  14 − 12  =  <b>2</b>  →  <b>Naranja</b>\n\n" +
            "<size=65%>Pulsa el botón del control para continuar.</size>";
        yield return StartCoroutine(WaitForConfirm());

        SetFiberHighlight(1, 2, false);
        SetBufferHighlight(1, false);
        foreach (var h in AllCableHighlights()) if (h != null) h.GrayOut(false);

        // ── Ejemplo 2: Fibra #37 → Búfer 4 (idx 3), pos 1 = Azul
        foreach (var h in AllCableHighlights()) if (h != null) h.GrayOut(true);
        SetBufferHighlight(3, true);
        SetFiberHighlight(3, 1, true);

        instructionText.text =
            "<b>Ejemplo: Fibra global #37</b>\n\n" +
            "<color=#FFD700>Paso 1:</color>  37 ÷ 12 = 3.08  →  redondea a  <b>4</b>  →  Cinta Milar 4\n\n" +
            "<color=#FFD700>Paso 2:</color>  37 − ( 4 − 1 ) × 12  =  37 − 36  =  <b>1</b>  →  <b>Azul</b>\n\n" +
            "<size=70%>Pulsa el botón del control para continuar.</size>";
        yield return StartCoroutine(WaitForConfirm());

        SetFiberHighlight(3, 1, false);
        SetBufferHighlight(3, false);
        foreach (var h in AllCableHighlights()) if (h != null) h.GrayOut(false);

        // ── Inicio ejercicios ──────────────────────────────────────
        instructionText.text =
            "Ahora es tu turno.\n" +
            "Se te dará el número global de una fibra.\n" +
            "Usa la fórmula para encontrar el búfer y el color.\n\n" +
            "<size=70%>Pulsa el botón del control para comenzar.</size>";
        yield return StartCoroutine(WaitForConfirm());

        const string FORMULA = "Búfer = N ÷ 12 (redondea arriba)   |   Posición = N − (Búfer−1) × 12";
        const int totalEj = 3;
        const int maxPuntosStep6 = totalEj * 2; // acBuf + acColor por ejercicio
        int puntos = 0;
        int[] numerosGlobales = logica.GenerarNumerosGlobales(totalEj);

        foreach (int N in numerosGlobales)
        {
            int correctBuf = logica.CalcularBuffer(N);    // 1-based (1-5)
            int correctPos = logica.CalcularPosicion(N);   // 1-based (1-12)
            int correctBufIdx = correctBuf - 1;           // 0-based
            string colorCorrecto = P1_Logica.COLOR_NAMES[correctPos - 1];

            // ── Fase 1: Búfer ─────────────────────────────────────
            SetAllBuffersHighlight(true);

            string[] opsBuf = logica.GenerarOpcionesBufer(correctBuf);
            for (int i = 0; i < botonesColor.Length; i++)
                botonesColor[i].GetComponentInChildren<TextMeshProUGUI>().text = opsBuf[i];

            PosicionarPanelFrenteAlJugador();
            if (panelRespuestas != null) panelRespuestas.SetActive(true);

            instructionText.text =
                $"<size=75%>{FORMULA}</size>\n\n" +
                $"Fibra global <b>#{N}</b>\n" +
                "¿A qué Cinta Milar pertenece?\n" +
                "<size=70%>Mira la opción y pulsa el control.</size>";

            string respBuf = null;
            yield return StartCoroutine(WaitForQuizSelection(opsBuf, r => respBuf = r));

            if (panelRespuestas != null) panelRespuestas.SetActive(false);
            SetAllBuffersHighlight(false);

            bool acBuf = logica.EvaluarRespuesta(respBuf, $"Cinta Milar {correctBuf}");
            if (acBuf) puntos++;
            ShowFeedback(acBuf,
                acBuf
                    ? $"¡Correcto! Es la Cinta Milar {correctBuf}."
                    : $"El búfer correcto es la Cinta Milar {correctBuf}.");

            yield return new WaitForSeconds(1.5f);

            // ── Fase 2: Color ─────────────────────────────────────
            foreach (var h in AllCableHighlights()) if (h != null) h.GrayOut(true);
            SetBufferHighlight(correctBufIdx, true);

            string[] opsColor = logica.GenerarOpcionesColor(colorCorrecto);
            for (int i = 0; i < botonesColor.Length; i++)
                botonesColor[i].GetComponentInChildren<TextMeshProUGUI>().text = opsColor[i];

            PosicionarPanelFrenteAlJugador();
            if (panelRespuestas != null) panelRespuestas.SetActive(true);

            instructionText.text =
                $"<size=75%>{FORMULA}</size>\n\n" +
                $"Fibra global <b>#{N}</b> — Cinta Milar {correctBuf}\n" +
                "¿Cuál es su color?\n" +
                "<size=70%>Mira el color y pulsa el control.</size>";

            string respColor = null;
            yield return StartCoroutine(WaitForQuizSelection(opsColor, r => respColor = r));

            if (panelRespuestas != null) panelRespuestas.SetActive(false);
            SetBufferHighlight(correctBufIdx, false);
            foreach (var h in AllCableHighlights()) if (h != null) h.GrayOut(false);

            bool acColor = logica.EvaluarRespuesta(respColor, colorCorrecto);
            if (acColor) puntos++;
            ShowFeedback(acColor,
                acColor
                    ? $"¡Correcto! Fibra #{N} = {colorCorrecto}."
                    : $"El color correcto era {colorCorrecto}.");

            yield return new WaitForSeconds(1.8f);
        }

        scoreStep6 = puntos;

        instructionText.text =
            $"Resultado Paso 6: <b>{puntos}/{maxPuntosStep6}</b> correctas.\n\n" +
            "<size=70%>Pulsa el botón del control para continuar.</size>";
        yield return StartCoroutine(WaitForConfirm());

        StartCoroutine(RunStep7_Checkout());
    }

    // ══════════════════════════════════════════════════════════════════
    // PASO 7 — Checkout y resumen final
    // ══════════════════════════════════════════════════════════════════

    IEnumerator RunStep7_Checkout()
    {
        currentStep = 7;

        int total = scoreStep2 + scoreStep5 + scoreStep6;
        float promedio = logica.CalcularCalificacionFinal(scoreStep2, scoreStep5, scoreStep6);

        EnviarResultado(promedio);

        // ── Resumen de resultados ──────────────────────────────────
        instructionText.text =
            "¡Práctica 1 completada!\n\n" +
            $"Identificación de partes (Paso 2):   <b>{scoreStep2}/4</b> correctas\n" +
            $"Identificación de fibras (Paso 5):   <b>{scoreStep5}/4</b> correctas\n" +
            $"Cálculo de posición global (Paso 6): <b>{scoreStep6}/6</b> correctas\n\n" +
            $"Puntuación total: <b>{total}/14</b>\n\n" +
            $"Calificación final: <b>{promedio:F1} / 10</b>\n\n" +
            "<size=70%>Pulsa el botón del control para continuar.</size>";
        yield return StartCoroutine(WaitForConfirm());

        // ── Indicación de salida ───────────────────────────────────
        instructionText.text =
            "Dirígete hacia la <b>PC</b> para finalizar la práctica.\n\n" +
            "<size=70%>Observa alrededor de tu entorno — la PC iluminada en amarillo\n" +
            "indica el lugar de salida. Mira hacia arriba para leer las instrucciones.</size>";

        if (exitIndicator    != null) exitIndicator.SetActive(true);
        if (pcHighlight      != null) pcHighlight.Highlight(true);

        // Espera proximidad al punto de salida
        yield return new WaitUntil(() =>
            exitPoint != null &&
            playerTransform != null &&
            Vector3.Distance(playerTransform.position, exitPoint.position) <= exitRadius
        );

        if (exitIndicator != null) exitIndicator.SetActive(false);
        if (pcHighlight   != null) pcHighlight.Highlight(false);

        instructionText.text =
            "¡Bien hecho!\n" +
            "Ahora responde el cuestionario\n" +
            "para completar tu evaluación.";

        yield return new WaitForSeconds(2.5f);

        // Detener Cardboard/XR antes de cargar la escena 2D
        var xrMgr = XRGeneralSettings.Instance?.Manager;
        if (xrMgr != null && xrMgr.isInitializationComplete)
        {
            xrMgr.StopSubsystems();
            xrMgr.DeinitializeLoader();
        }
        yield return null; // un frame para que XR termine de cerrarse

        SceneManager.LoadScene(escenaCuestionario);
    }

    // Envía la calificación de la práctica (obtenida en el entorno RV, sin
    // cuestionario) a Supabase. respuestas_json lleva un resumen por paso
    // en vez de pares pregunta/respuesta.
    void EnviarResultado(float calificacion)
    {
        string accessToken = PlayerPrefs.GetString("sb_access_token", "");
        int alumnoId = PlayerPrefs.GetInt("alumno_id", 0);
        int practicaId = PlayerPrefs.GetInt("practica_id", 0);

        if (supabaseConfig == null || string.IsNullOrEmpty(accessToken) || alumnoId == 0 || practicaId == 0)
        {
            Debug.LogWarning("P1_InstructionManager: no se pudo enviar el resultado (config o sesión incompletos).");
            return;
        }

        StartCoroutine(repository.EnviarResultado(
            accessToken, alumnoId, practicaId, calificacion,
            logica.ConstruirRespuestasJson(scoreStep2, scoreStep5, scoreStep6),
            OnResultadoEnviado));
    }

    void OnResultadoEnviado(bool ok, long codigoHttp, string cuerpo)
    {
        if (ok)
            Debug.Log("P1_InstructionManager: resultado enviado a Supabase correctamente.");
        else
            Debug.LogWarning($"P1_InstructionManager: {MensajesHttp.ErrorEnvio(codigoHttp, cuerpo)}");
    }

    // Gaze-selection para World Space: proyecta las esquinas del botón a pantalla
    // y comprueba si el punto de mira (centro en VR, ratón en editor) está dentro.
    IEnumerator WaitForQuizSelection(string[] opciones, System.Action<string> onSelected)
    {
        int hovered = -1;
        foreach (var b in botonesColor)
            b.GetComponent<Image>().color = BTN_NORMAL;

        while (true)
        {
            Camera cam = Camera.main;
            int newHovered = -1;

            if (cam != null)
            {
                for (int i = 0; i < botonesColor.Length; i++)
                {
                    if (IsGazeOnButton(botonesColor[i].GetComponent<RectTransform>(), cam))
                    {
                        newHovered = i;
                        break;
                    }
                }
            }

            if (newHovered != hovered)
            {
                if (hovered >= 0)    botonesColor[hovered].GetComponent<Image>().color    = BTN_NORMAL;
                if (newHovered >= 0) botonesColor[newHovered].GetComponent<Image>().color = BTN_HOVERED;
                hovered = newHovered;
            }

            if (hovered >= 0 && TouchInput.ButtonDown("Fire1", ref _lastTouchPressId))
            {
                botonesColor[hovered].GetComponent<Image>().color = BTN_NORMAL;
                onSelected(opciones[hovered]);
                yield return null;
                yield break;
            }

            yield return null;
        }
    }

    // Proyecta las 4 esquinas world-space del RectTransform a pantalla
    // y comprueba si el punto de mira cae dentro del bounding rect.
    static bool IsGazeOnButton(RectTransform rt, Camera cam)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (var c in corners)
        {
            Vector3 s = cam.WorldToScreenPoint(c);
            if (s.z < 0) return false; // detrás de la cámara
            if (s.x < minX) minX = s.x;
            if (s.x > maxX) maxX = s.x;
            if (s.y < minY) minY = s.y;
            if (s.y > maxY) maxY = s.y;
        }

#if UNITY_EDITOR
        Vector2 gaze = Input.mousePosition;        // en editor: usar ratón para testear
#else
        Vector2 gaze = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f); // en VR: centro = reticle
#endif

        return gaze.x >= minX && gaze.x <= maxX &&
               gaze.y >= minY && gaze.y <= maxY;
    }

    // Coloca panelRespuestas en World Space frente al jugador, mirándolo
    void PosicionarPanelFrenteAlJugador()
    {
        if (panelRespuestas == null) return;
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 forward = cam.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        forward.Normalize();

        // 1.5 m al frente, 0.7 m sobre la altura de los ojos (no tapa la fibra)
        panelRespuestas.transform.position =
            cam.transform.position + forward * 1.5f + Vector3.up * 0.7f;

        panelRespuestas.transform.LookAt(cam.transform.position);
        panelRespuestas.transform.Rotate(0f, 180f, 0f);
    }

    void ShowFeedback(bool correct, string message)
    {
        feedbackText.color = correct ? Color.green : new Color(1f, 0.4f, 0f);
        feedbackText.text  = message;
        StartCoroutine(ClearFeedback(2.5f));
    }

    IEnumerator ClearFeedback(float delay)
    {
        yield return new WaitForSeconds(delay);
        feedbackText.text = "";
    }

    // ══════════════════════════════════════════════════════════════════
    // TEST TEMPORAL — verificación del paso 5 del refactor (evaluaciones
    // de correctitud y scores). BORRAR este método al terminar de probar.
    // ══════════════════════════════════════════════════════════════════

    [ContextMenu("TEST: Simular checkout con scores fijos")]
    void TestCheckoutScoresFijos()
    {
        (int s2, int s5, int s6)[] casos = { (3, 4, 5), (1, 2, 2) };
        foreach (var (s2, s5, s6) in casos)
        {
            scoreStep2 = s2;
            scoreStep5 = s5;
            scoreStep6 = s6;

            int total = scoreStep2 + scoreStep5 + scoreStep6;
            float calificacion = logica.CalcularCalificacionFinal(scoreStep2, scoreStep5, scoreStep6);
            float esperado = (s2 + s5 + s6) / 14f * 10f;

            Debug.Log($"TestCheckout: scoreStep2={scoreStep2} scoreStep5={scoreStep5} scoreStep6={scoreStep6} total={total}/14 calificacion={calificacion:F2} (esperado {esperado:F2})");
        }
    }

}
