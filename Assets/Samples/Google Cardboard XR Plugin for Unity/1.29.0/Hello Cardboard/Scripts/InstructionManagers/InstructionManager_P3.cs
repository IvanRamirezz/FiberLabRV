using UnityEngine;
using TMPro;
using System.Collections;
using System;

public class InstructionManagerPrac3 : MonoBehaviour
{
    public static InstructionManagerPrac3 Instance;

    // ══════════════════════════════════════════════════════════
    // REFERENCIAS DE EQUIPOS
    // ══════════════════════════════════════════════════════════

    [Header("Atenuador")]
    [SerializeField] OpticalAttenuatorController attenuator;
    public Highlightable atenuadorHighlight;
    public UIHighlightable botonAHighlight;   // Power
    public UIHighlightable botonBHighlight;   // Reset
    public UIHighlightable botonCHighlight;   // Wavelength mode
    public UIHighlightable botonDHighlight;   // Up
    public UIHighlightable botonEHighlight;   // Toggle unit
    public UIHighlightable botonFHighlight;   // Power mode
    public UIHighlightable botonGHighlight;   // Down

    [Header("BERT (NetXpert XG)")]
    [SerializeField] BERTesterController bert;
    public Highlightable berHighlight;
    public UIHighlightable menu1Highlight;
    public UIHighlightable menu2Highlight;
    public UIHighlightable menu3Highlight;
    public UIHighlightable menu4Highlight;

    [Header("Cables de fibra")]
    public Highlightable cable1Highlight;     // Patchcord 1
    public Highlightable cable2Highlight;     // Patchcord 2

    // ══════════════════════════════════════════════════════════
    // REFERENCIAS DE NAVEGACIÓN
    // ══════════════════════════════════════════════════════════

    [Header("Referencias generales")]
    public Transform player;
    public TextMeshProUGUI instructionText;

    [Header("Punto de llegada: Mesa 3")]
    public Transform targetMesa3;
    public float triggerRadiusMesa3 = 1.5f;

    [Header("Punto de checkout (fin de práctica)")]
    public Transform targetCheckout;
    public float triggerRadiusCheckout = 1.5f;

    [Header("Escena del cuestionario")]
    public string nombreEscenaCuestionario = "Cuestionario_P3";

    // ══════════════════════════════════════════════════════════
    // ESTADO INTERNO
    // ══════════════════════════════════════════════════════════

    private int step = 0;
    public bool allowFocusMode;
    private bool waitingForBERTest = false;
    private bool waitingForQuickTest = false;
    private bool berTestCompleted = false;
    private bool quickTestCompleted = false;

    // ══════════════════════════════════════════════════════════
    // MAPA DE PASOS
    // ══════════════════════════════════════════════════════════
    //
    // Step 0:  Inicio → alumno camina hacia mesa 3
    // Step 1:  Llegó a mesa 3 → reconocimiento del BERT
    // Step 2:  Reconocimiento del atenuador
    // Step 3:  Reconocimiento de los cables
    // Step 4:  Fin de reconocimiento → instrucción escenario Disconnected
    // Step 5:  Alumno abre BERT → ejecuta Quick Test (ve "No Link")
    // Step 6:  Alumno ejecuta BER Test completo (ve "No Link")
    // Step 7:  Sale del BERT → instrucción de conectar cables
    // Step 8:  Alumno conecta los dos patchcords (LinkState → Nominal)
    // Step 9:  Instrucción de configurar atenuador bajo
    // Step 10: Alumno abre atenuador, enciende, configura bajo
    // Step 11: Alumno ejecuta Quick Test (ve "PASS")
    // Step 12: Alumno ejecuta BER Test (ve ~0 errores, PASS)
    // Step 13: Instrucción de subir atenuación
    // Step 14: Alumno configura atenuador alto (-15 dB)
    // Step 15: Alumno ejecuta Quick Test (ve "FAIL")
    // Step 16: Alumno ejecuta BER Test (ve muchos errores, FAIL)
    // Step 17: Conclusión → dirigirse al checkout
    // Step 18: Alumno llega al checkout → cargar escena cuestionario
    //
    // ══════════════════════════════════════════════════════════

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        allowFocusMode = false;
        instructionText.text = "Instrucciones: Dirígete hacia la mesa 3.";
    }

    // ══════════════════════════════════════════════════════════
    // UPDATE — Polling para pasos basados en posición o estado
    // ══════════════════════════════════════════════════════════

    void Update()
    {
        switch (step)
        {
            case 0: // Esperando que el alumno llegue a mesa 3
                if (Vector3.Distance(player.position, targetMesa3.position) <= triggerRadiusMesa3)
                {
                    StartCoroutine(NextStep());
                }
                break;

            case 8: // Esperando que se conecten ambos patchcords
                if (LinkStateManager.Instance != null &&
                    LinkStateManager.Instance.CurrentScenario != LinkScenario.Disconnected)
                {
                    StartCoroutine(NextStep());
                }
                break;

           
            case 14: // Esperando configuración del atenuador (valor alto)
                if (CheckAttenuatorHighAttenuation())
                {
                    StartCoroutine(NextStep());
                }
                break;
            case 10:
                
                if (CheckAttenuatorLowAttenuation())
                {
                    StartCoroutine(NextStep());
                }
                break;
                
            case 18: // Esperando que el alumno llegue al checkout
                if (targetCheckout != null &&
                    Vector3.Distance(player.position, targetCheckout.position) <= triggerRadiusCheckout)
                {
                    StartCoroutine(NextStep());
                }
                break;
        }
    }

    // ══════════════════════════════════════════════════════════
    // NEXTSTEP — Lógica secuencial de la práctica
    // ══════════════════════════════════════════════════════════

    IEnumerator NextStep()
    {
        step++;
        switch (step)
        {
            // ──────────────────────────────────────────────
            // FASE 1: RECONOCIMIENTO DEL ENTORNO
            // ──────────────────────────────────────────────

            case 1: // Llegó a mesa 3 → presentar el BERT
                instructionText.text = "Has llegado a la mesa de trabajo. \nA continuación conocerás los equipos.";
                yield return new WaitForSeconds(3f);

                berHighlight.Highlight(true);
                instructionText.text = "Este es el NetXpert XG SE, un tester de cableado de red. \n Permite medir la tasa de error de bit (BER)\n en enlaces de fibra óptica.";
                yield return new WaitForSeconds(5f);

                instructionText.text = "Con él podrás verificar si un enlace soporta transmisiones\n a 10 Gbps sin errores.";
                yield return new WaitForSeconds(4f);
                berHighlight.Highlight(false);

                StartCoroutine(NextStep());
                break;

            case 2: // Presentar el atenuador
                atenuadorHighlight.Highlight(true);
                instructionText.text = "Este es el atenuador óptico variable EXFO FVA-600. \nPermite simular pérdidas en el enlace de fibra.";
                yield return new WaitForSeconds(5f);

                instructionText.text = "Ajustando la atenuación,\n podrás observar cómo afecta la calidad de la transmisión.";
                yield return new WaitForSeconds(4f);
                atenuadorHighlight.Highlight(false);

                StartCoroutine(NextStep());
                break;

            case 3: // Presentar los cables
                if (cable1Highlight != null) cable1Highlight.Highlight(true);
                if (cable2Highlight != null) cable2Highlight.Highlight(true);
                instructionText.text = "Estos son los patchcords de fibra óptica monomodo. \n Los usarás para conectar el tester con el atenuador.";
                yield return new WaitForSeconds(5f);

                instructionText.text = "Cada patchcord tiene conectores LC/UPC en ambos extremos.";
                yield return new WaitForSeconds(4f);
                if (cable1Highlight != null) cable1Highlight.Highlight(false);
                if (cable2Highlight != null) cable2Highlight.Highlight(false);

                StartCoroutine(NextStep());
                break;

            case 4: // Fin de reconocimiento → transición al escenario Disconnected
                instructionText.text = "Bien, ya conoces los equipos. \nAhora comenzaremos la práctica.";
                yield return new WaitForSeconds(3f);

                StartCoroutine(NextStep());
                break;

            // ──────────────────────────────────────────────
            // FASE 2: ESCENARIO DISCONNECTED
            // ──────────────────────────────────────────────

            case 5: // Instrucción para probar sin conexiones
                allowFocusMode = true;
                berHighlight.Highlight(true);
                instructionText.text = "Primero, veamos qué ocurre sin conexiones. \n Abre el NetXpert XG y ejecuta un Quick Test.";

                // Esperar a que el alumno abra el BERT y ejecute Quick Test
                waitingForQuickTest = true;
                quickTestCompleted = false;
                break;

            case 6: // Quick Test sin conexión completado → pedir BER Test
                berHighlight.Highlight(false);
                instructionText.text = "El equipo indica que no hay enlace. \n Ahora ejecuta un BER Test completo para confirmar.";

                waitingForBERTest = true;
                berTestCompleted = false;
                break;

            case 7: // BER Test sin conexión completado → pedir salir del BERT
                instructionText.text = "Sin conexión física, no es posible realizar mediciones. \n Sal del equipo para proceder con el cableado.";

                // Esperar a que salga del BERT (evento AtenuadorCompleted / BERCompleted)
                break;

            // ──────────────────────────────────────────────
            // FASE 3: CONEXIÓN DE CABLES
            // ──────────────────────────────────────────────

            case 8: // Instrucción de conectar cables
                instructionText.text = "Conecta el primer patchcord del puerto TX del NetXpert\n al puerto Input del atenuador.";
                yield return new WaitForSeconds(5f);

                instructionText.text = "Luego, conecta \nel segundo patchcord del puerto Output del atenuador\n al puerto RX del NetXpert.";

                // El Update() en case 8 espera a que LinkState cambie de Disconnected
                break;

            case 9: // Cables conectados → instrucción de configurar atenuador bajo
                instructionText.text = "¡Enlace establecido! \nAhora configura el atenuador.";
                yield return new WaitForSeconds(3f);

                atenuadorHighlight.Highlight(true);
                instructionText.text = "Abre el atenuador y configura la atenuación a -5 dB \n (usa el botón Reset si lo necesitas).";
                break;

            // ──────────────────────────────────────────────
            // FASE 4: ESCENARIO NOMINAL (atenuación baja)
            // ──────────────────────────────────────────────

            case 10: // Esperando configuración del atenuador (polling en Update)
                     // El Update() verifica CheckAttenuatorLowAttenuation()
                instructionText.text = "Esta es la interfaz del atenuador.";
                yield return new WaitForSeconds(3f);

                instructionText.text = "A continuación se te mostrarán los botones. \nNo presiones nada aún.";
                yield return new WaitForSeconds(3f);

                instructionText.text = "Este botón sirve para resetear a cero \nel valor de la potencia de atenuación.";
                botonBHighlight.Highlight(true);
                yield return new WaitForSeconds(3f);
                botonBHighlight.Highlight(false);

                botonCHighlight.Highlight(true);
                instructionText.text = "Este botón sirve para configurar \nel valor de la longitud de onda.";
                yield return new WaitForSeconds(3f);
                botonCHighlight.Highlight(false);

                botonFHighlight.Highlight(true);
                instructionText.text = "Este botón sirve para configurar \nel valor de la potencia de atenuación.";
                yield return new WaitForSeconds(3f);
                botonFHighlight.Highlight(false);

                botonDHighlight.Highlight(true);
                botonGHighlight.Highlight(true);
                instructionText.text = "Los podrás configurar con estos dos botones,\n para subir el valor y para bajarlo.";
                yield return new WaitForSeconds(3f);
                botonDHighlight.Highlight(false);
                botonGHighlight.Highlight(false);

                instructionText.text = "Bien, procede a configurar la potencia a -5 dB \n y la longitud de onda a 1550nm ";
                
                break;

            case 11: // Atenuador configurado bajo → pedir Quick Test
                atenuadorHighlight.Highlight(false);
                instructionText.text = "Atenuador configurado. \n Ahora abre el NetXpert y ejecuta un Quick Test.";

                berHighlight.Highlight(true);
                waitingForQuickTest = true;
                quickTestCompleted = false;
                break;

            case 12: // Quick Test nominal completado → pedir BER Test
                instructionText.text = "El enlace soporta 10 Gbps. \nAhora ejecuta un BER Test completo para verificar la calidad.";

                waitingForBERTest = true;
                berTestCompleted = false;
                break;

            case 13: // BER Test nominal completado → instrucción de subir atenuación
                berHighlight.Highlight(false);
                instructionText.text = "Excelente.\n El enlace muestra muy pocos o ningún error. \n El BER es menor a 10^-12, \nlo cual cumple con el estándar IEEE 802.3an.";
                yield return new WaitForSeconds(5f);

                instructionText.text = "Ahora veremos qué ocurre cuando el enlace se degrada. \nSal del equipo.";
                break;

            // ──────────────────────────────────────────────
            // FASE 5: ESCENARIO DEGRADADO (atenuación alta)
            // ──────────────────────────────────────────────

            case 14: // Instrucción de subir atenuación
                atenuadorHighlight.Highlight(true);
                instructionText.text = "Abre el atenuador y sube la atenuación a -25 dB. \n Esto simulará un enlace con problemas.";

                // El Update() verifica CheckAttenuatorHighAttenuation()
                break;

            case 15: // Atenuador configurado alto → pedir Quick Test
                atenuadorHighlight.Highlight(false);
                instructionText.text = "Atenuación configurada a -25 dB.\n Abre el NetXpert y ejecuta un Quick Test.";

                berHighlight.Highlight(true);
                waitingForQuickTest = true;
                quickTestCompleted = false;
                break;

            case 16: // Quick Test degradado completado → pedir BER Test
                instructionText.text = "El enlace falla a 10 Gbps. \nEjecuta un BER Test completo\n para ver el impacto en detalle.";

                waitingForBERTest = true;
                berTestCompleted = false;
                break;

            case 17: // BER Test degradado completado → conclusión
                berHighlight.Highlight(false);
                instructionText.text = "Observa la diferencia: \n con alta atenuación, \n el BER aumentó drásticamente \ny el enlace no cumple con el estándar.";
                yield return new WaitForSeconds(6f);

                instructionText.text = "Has completado la práctica. \n Dirígete al punto de salida\n para continuar con el cuestionario.";
                allowFocusMode = false;
                break;

            // ──────────────────────────────────────────────
            // FASE 6: CHECKOUT
            // ──────────────────────────────────────────────

            case 18: // Esperando que llegue al checkout (polling en Update)
                break;

            case 19: // Llegó al checkout → cargar cuestionario
                instructionText.text = "Práctica completada. Cargando cuestionario...";
                yield return new WaitForSeconds(2f);

                // TODO: Cambiar a la escena del cuestionario
                // UnityEngine.SceneManagement.SceneManager.LoadScene(nombreEscenaCuestionario);
                Debug.Log("Cargar escena: " + nombreEscenaCuestionario);
                break;

            default:
                break;
        }
    }

    // ══════════════════════════════════════════════════════════
    // SUSCRIPCIÓN A EVENTOS
    // ══════════════════════════════════════════════════════════

    void OnEnable()
    {
        // Eventos del atenuador
        OpticalAttenuatorController.AtenuadorCompleted += HandleAtenuadorCompleted;
        OpticalAttenuatorController.AtenuadorStarted += HandleAtenuadorStarted;
        OpticalAttenuatorController.OnAttenuatorPoweredOn += HandleAtenuadorEncendido;

        // Eventos del BERT
        BERTesterController.OnBERCompleted += HandleBERExited;

        // Eventos del LinkStateManager
        LinkStateManager.OnScenarioChanged += HandleScenarioChanged;

        // TODO: Suscribirse a eventos de test completado cuando los implementes
        BERTesterController.OnQuickTestFinished += HandleQuickTestFinished;
        BERTesterController.OnBERTestFinished += HandleBERTestFinished;
    }

    void OnDisable()
    {
        OpticalAttenuatorController.AtenuadorCompleted -= HandleAtenuadorCompleted;
        OpticalAttenuatorController.AtenuadorStarted -= HandleAtenuadorStarted;
        OpticalAttenuatorController.OnAttenuatorPoweredOn -= HandleAtenuadorEncendido;

        BERTesterController.OnBERCompleted -= HandleBERExited;

        LinkStateManager.OnScenarioChanged -= HandleScenarioChanged;

        BERTesterController.OnQuickTestFinished -= HandleQuickTestFinished;
        BERTesterController.OnBERTestFinished -= HandleBERTestFinished;
    }

    // ══════════════════════════════════════════════════════════
    // HANDLERS DE EVENTOS
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Se dispara cuando el alumno enciende el atenuador.
    /// </summary>
    void HandleAtenuadorEncendido()
    {
        // No se usa directamente en esta práctica,
        // pero se deja como hook disponible.
    }

    /// <summary>
    /// Se dispara cuando el alumno abre el atenuador (Focus Mode).
    /// </summary>
    void HandleAtenuadorStarted()
    {
        // No se usa directamente en esta práctica.
        if (step == 9)
        {
            StartCoroutine(NextStep());
        }
    }

    /// <summary>
    /// Se dispara cuando el alumno sale del atenuador.
    /// </summary>
    void HandleAtenuadorCompleted()
    {
        // Avanzar después de configurar atenuador en ciertos pasos
        // (la validación real se hace por polling en Update con los Check*)
    }

    /// <summary>
    /// Se dispara cuando el alumno SALE del BERT (cierra el instrumento).
    /// </summary>
    void HandleBERExited()
    {
        // Step 7: sale del BERT después de probar escenario Disconnected
        if (step == 7)
        {
            StartCoroutine(NextStep());
        }

        // Step 13: sale del BERT después de escenario Nominal
        if (step == 13)
        {
            StartCoroutine(NextStep());
        }

        // Step 17 no necesita handler porque ya mostró conclusión
    }

    /// <summary>
    /// Se dispara cuando el LinkStateManager cambia de escenario.
    /// Útil para detectar automáticamente la conexión de cables.
    /// </summary>
    void HandleScenarioChanged(LinkScenario newScenario)
    {
        Debug.Log("[InstructionManager] Escenario cambió a: " + newScenario);

        // No necesitamos hacer nada aquí porque el polling en Update
        // ya detecta el cambio. Pero el log es útil para debugging.
    }

    /// <summary>
    /// Se dispara cuando un Quick Test termina.
    /// TODO: Necesitas añadir este evento en BERTesterController.
    /// Dispáralo al final de RunQuickTest():
    ///   public static event Action OnQuickTestFinished;
    ///   ...
    ///   OnQuickTestFinished?.Invoke();
    /// </summary>
    public void HandleQuickTestFinished()
    {
        if (!waitingForQuickTest) return;

        waitingForQuickTest = false;
        quickTestCompleted = true;
        StartCoroutine(NextStep());
    }

    /// <summary>
    /// Se dispara cuando un BER Test completo termina.
    /// TODO: Necesitas añadir este evento en BERTesterController.
    /// Dispáralo al final de GenerateResults():
    ///   public static event Action OnBERTestFinished;
    ///   ...
    ///   OnBERTestFinished?.Invoke();
    /// </summary>
    public void HandleBERTestFinished()
    {
        if (!waitingForBERTest) return;

        waitingForBERTest = false;
        berTestCompleted = true;
        StartCoroutine(NextStep());
    }

    // ══════════════════════════════════════════════════════════
    // VALIDACIONES DE ESTADO
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Verifica que el atenuador está configurado con atenuación baja
    /// (0 dB, que es el valor reset).
    /// </summary>
    bool CheckAttenuatorLowAttenuation()
    {

       // Debug.LogWarning((attenuator==null) + "pepe");
        //Debug.LogWarning( (attenuator.IsOn) + " pecas");
        if (attenuator == null || !attenuator.IsOn)
            return false;

        //float atenuacionAbs = Mathf.Abs(attenuator.CurrentPower);
        //Debug.LogWarning(atenuacionAbs+" es la atenuación");
        //Debug.LogWarning(Mathf.Approximately(attenuator.CurrentPower, -5f) + " es la condición");
        return (attenuator.CurrentWavelength == 1550 &&
           Mathf.Approximately(attenuator.CurrentPower, -5f)); // 0 o -5 dB se consideran "bajo"
    }

    /// <summary>
    /// Verifica que el atenuador está configurado con atenuación alta
    /// (-15 dB o más negativo).
    /// </summary>
    bool CheckAttenuatorHighAttenuation()
    {
        if (attenuator == null || !attenuator.IsOn)
            return false;

        //float atenuacionAbs = Mathf.Abs(attenuator.CurrentPower);
        return Mathf.Approximately(attenuator.CurrentPower, -25f);
    }
}
