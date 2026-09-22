using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;
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
    public UIHighlightable botonPowerBERHighlight;   // Encendido del equipo
    public UIHighlightable menu1Highlight;           // Config
    public UIHighlightable menu2Highlight;           // Quick Test
    public UIHighlightable menu3Highlight;           // BER Test
    public UIHighlightable menu4Highlight;           // Info

    [Header("Cables de fibra")]
    public Highlightable cable1Highlight;     // Patchcord 1
    public Highlightable cable2Highlight;     // Patchcord 2
    public Highlightable LC1, LC2, LC3, LC4; // Conectores

    // ══════════════════════════════════════════════════════════
    // REFERENCIAS DE NAVEGACIÓN
    // ══════════════════════════════════════════════════════════

    [Header("Referencias generales")]
    public Transform player;
    public TextMeshProUGUI instructionText;

    [Header("Punto de llegada: Mesa 3")]
    public Transform targetMesa3;
    public float triggerRadiusMesa3 = 1.5f;
    public Highlightable mesa3Highlight;

    [Header("Punto de checkout (fin de práctica)")]
    public Transform targetCheckout;
    public float triggerRadiusCheckout = 1.5f;
    public Highlightable pcHighlight;

    [Header("Escena del cuestionario")]
    public string nombreEscenaCuestionario = "CuestionarioPrac3";

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
    // Step 5:  Instrucción de abrir el NetXpert (espera evento OnBERStarted)
    // Step 6:  Pausa de adaptación → espera a que presione el botón de encendido (evento OnBERPoweredOn)
    // Step 7:  Conoce sus 4 pantallas, configura interfaz (SFP+ 10Gbps) en Config
    // Step 8:  Interfaz configurada → ejecuta Quick Test (ve "No Link")
    // Step 9:  Quick Test hecho → revisa pantalla Info (relación duración/confiabilidad, IEEE 802.3an)
    // Step 10: Configura BER Test: 99 s de duración y velocidad 10 Gbps
    // Step 11: Configuración correcta → presiona Start Test por primera vez (ve "No Link")
    // Step 12: Sale del BERT → instrucción de conectar cables
    // Step 13: Alumno conecta los dos patchcords (LinkState → Nominal)
    // Step 14: Instrucción de configurar atenuador bajo
    // Step 15: Alumno abre atenuador, enciende, configura bajo
    // Step 16: Alumno ejecuta Quick Test (ve "PASS")
    // Step 17: Alumno ejecuta BER Test (ve ~0 errores, PASS)
    // Step 18: Instrucción de subir atenuación
    // Step 19: Alumno configura atenuador alto (-15 dB)
    // Step 20: Alumno ejecuta Quick Test (ve "FAIL")
    // Step 21: Alumno ejecuta BER Test (ve muchos errores, FAIL)
    // Step 22: Conclusión
    // Step 23: Dirigirse al checkout (polling en Update)
    // Step 24: Alumno llega al checkout → cargar escena cuestionario
    //
    // (La configuración de interfaz, duración y velocidad hecha en los steps 7-10
    //  se guarda en los mismos campos del BERTesterController, por lo que NO
    //  se vuelve a pedir en los steps 16-17 ni 20-21.)
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
        if (mesa3Highlight != null) mesa3Highlight.Highlight(true);
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
                    if (mesa3Highlight != null) mesa3Highlight.Highlight(false);
                    StartCoroutine(NextStep());
                }
                break;

            case 7: // Esperando que configure la interfaz correcta (SFP+ 10Gbps) en Config
                if (bert != null && bert.IsInterfaceConfigured())
                {
                    StartCoroutine(NextStep());
                }
                break;

            case 10: // Esperando que configure el BER Test (99 s, 10 Gbps)
                if (bert != null && bert.IsBerTestConfigCorrect())
                {
                    StartCoroutine(NextStep());
                }
                break;

            case 13: // Esperando que se conecten ambos patchcords
                if (LinkStateManager.Instance != null &&
                    LinkStateManager.Instance.CurrentScenario != LinkScenario.Disconnected)
                {
                    StartCoroutine(NextStep());
                }
                break;

            case 19: // Esperando configuración del atenuador (valor alto)
                if (CheckAttenuatorHighAttenuation())
                {
                    StartCoroutine(NextStep());
                }
                break;
            case 15:

                if (CheckAttenuatorLowAttenuation())
                {
                    StartCoroutine(NextStep());
                }
                break;

            case 23: // Esperando que el alumno llegue al checkout
                if (targetCheckout == null || player == null)
                {
                    if (Time.frameCount % 120 == 0)
                        Debug.Log($"[P3-DEBUG] case23: targetCheckout={(targetCheckout != null ? "OK" : "NULL")} player={(player != null ? "OK" : "NULL")}");
                    break;
                }

                float dist = Vector3.Distance(player.position, targetCheckout.position);
                if (Time.frameCount % 60 == 0) // ~1 vez por segundo
                    Debug.Log($"[P3-DEBUG] case23: distancia={dist:F2} radio={triggerRadiusCheckout} playerPos={player.position} checkoutPos={targetCheckout.position}");

                if (dist <= triggerRadiusCheckout)
                {
                    Debug.Log("[P3-DEBUG] case23: llegó al checkout, avanzando a case 24");
                    if (pcHighlight != null) pcHighlight.Highlight(false);
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
                instructionText.text = "Este es el <color=yellow>NetXpert XG SE</color>, \nun tester de cableado de red. \n Permite medir la tasa de error de bit (BER)\n en enlaces de fibra óptica.";
                yield return new WaitForSeconds(5f);

                instructionText.text = "Con él podrás verificar \nsi un enlace soporta transmisiones\n a 10 Gbps sin errores.";
                yield return new WaitForSeconds(4f);
                berHighlight.Highlight(false);

                StartCoroutine(NextStep());
                break;

            case 2: // Presentar el atenuador
                atenuadorHighlight.Highlight(true);
                instructionText.text = "Este es el atenuador óptico variable <color=yellow>EXFO FVA-600</color>. \nPermite simular pérdidas en el enlace de fibra.";
                yield return new WaitForSeconds(5f);

                instructionText.text = "Ajustando la atenuación,\n podrás observar cómo afecta la calidad de la transmisión.";
                yield return new WaitForSeconds(4f);
                atenuadorHighlight.Highlight(false);

                StartCoroutine(NextStep());
                break;

            case 3: // Presentar los cables
                if (cable1Highlight != null) cable1Highlight.Highlight(true);
                if (cable2Highlight != null) cable2Highlight.Highlight(true);
                instructionText.text = "Estos son los <color=yellow>patchcords</color> de fibra óptica monomodo. \n Los usarás para conectar el tester con el atenuador.";
                yield return new WaitForSeconds(5f);
                if (cable1Highlight != null) cable1Highlight.Highlight(false);
                if (cable2Highlight != null) cable2Highlight.Highlight(false);
                instructionText.text = "Cada patchcord tiene <color=yellow>conectores LC/UPC </color> \nen ambos extremos.";
                if (LC1 != null) LC1.Highlight(true);
                if (LC2 != null) LC2.Highlight(true);
                if (LC3 != null) LC3.Highlight(true);
                if (LC4 != null) LC4.Highlight(true);
                yield return new WaitForSeconds(4f);
                if (LC1 != null) LC1.Highlight(false);
                if (LC2 != null) LC2.Highlight(false);
                if (LC3 != null) LC3.Highlight(false);
                if (LC4 != null) LC4.Highlight(false);

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

            case 5: // Instrucción de abrir el NetXpert → espera evento OnBERStarted (FocusMode)
                allowFocusMode = true;
                berHighlight.Highlight(true);
                instructionText.text = "Primero, veamos qué ocurre sin conexiones.\nAbre el <color=yellow>NetXpert XG SE</color>.";

                // El HandleBERStarted() avanza cuando el alumno entra en FocusMode del BERT
                break;

            case 6: // Un momento para que el alumno se oriente, luego pedir que encienda el equipo
               

                botonPowerBERHighlight.Highlight(true);
                instructionText.text = "Enciende el equipo con este botón.";

                // El HandleBERPoweredOn() avanza cuando el alumno realmente presiona el botón
                break;

            case 7: // Conocer las 4 pantallas y pedir configurar la interfaz
                instructionText.text = "Esta es la interfaz del BER Tester.";
                yield return new WaitForSeconds(3f);

                instructionText.text = "A continuación se te mostrarán los botones. \nNo presiones nada aún.";
                yield return new WaitForSeconds(3f);
                menu1Highlight.Highlight(true);
                instructionText.text = "Config: aquí se configuran los parámetros \nde la interfaz de prueba.";
                yield return new WaitForSeconds(3f);
                menu1Highlight.Highlight(false);

                menu2Highlight.Highlight(true);
                instructionText.text = "Quick Test: una prueba rápida\n para verificar continuidad del enlace.";
                yield return new WaitForSeconds(3f);
                menu2Highlight.Highlight(false);

                menu3Highlight.Highlight(true);
                instructionText.text = "BER Test: la medición completa\n de la tasa de error de bit.";
                yield return new WaitForSeconds(3f);
                menu3Highlight.Highlight(false);

                menu4Highlight.Highlight(true);
                instructionText.text = "Info: información de referencia\n sobre el estándar de medición.";
                yield return new WaitForSeconds(3f);
                menu4Highlight.Highlight(false);

                menu1Highlight.Highlight(true);
                instructionText.text = "Entra a Config y selecciona la interfaz SFP+ 10Gbps.";

                // El Update() en case 7 espera a que se configure el dropdown correctamente
                break;

            case 8: // Interfaz configurada → pedir Quick Test
                menu1Highlight.Highlight(false);
                instructionText.text = "Interfaz configurada correctamente.\nAhora ejecuta un Quick Test.";

                waitingForQuickTest = true;
                quickTestCompleted = false;
                break;

            case 9: // Quick Test sin conexión completado → revisar pantalla Info
                instructionText.text = "El equipo indica que no hay enlace.\nAntes de continuar, revisa la pantalla Info.";
                yield return new WaitForSeconds(3f);

                menu4Highlight.Highlight(true);
                instructionText.text = "Ahí encontrarás la relación entre la confiabilidad\n de la prueba y su duración, \nsegún el estándar IEEE 802.3an.";
                yield return new WaitForSeconds(5f);

                instructionText.text = "Por ejemplo, una duración de 99 segundos\n ofrece una confiabilidad del 63%.";
                yield return new WaitForSeconds(5f);
                menu4Highlight.Highlight(false);

                StartCoroutine(NextStep());
                break;

            case 10: // Instrucción de configurar el BER Test (duración y velocidad)
                menu3Highlight.Highlight(true);
                instructionText.text = "Entra a BER Test y configura una duración de 99 segundos,\n con una velocidad máxima de 10 Gbps.";

                // El Update() en case 10 espera a que testDuration==99 y toggle10Gb.isOn
                break;

            case 11: // Configuración correcta → pedir presionar Start Test
                menu3Highlight.Highlight(false);
                instructionText.text = "Configuración correcta.\nAhora presiona Start Test para ejecutar tu primera medición.";

                waitingForBERTest = true;
                berTestCompleted = false;
                break;

            case 12: // BER Test sin conexión completado → pedir salir del BERT
                instructionText.text = "Sin conexión física, no es posible realizar mediciones. \n Sal del equipo para proceder con el cableado.";

                // Esperar a que salga del BERT (evento AtenuadorCompleted / BERCompleted)
                break;

            // ──────────────────────────────────────────────
            // FASE 3: CONEXIÓN DE CABLES
            // ──────────────────────────────────────────────

            case 13: // Instrucción de conectar cables
                instructionText.text = "Conecta el primer patchcord del puerto TX del NetXpert\n al puerto Input del atenuador.";
                yield return new WaitForSeconds(5f);
                

                instructionText.text = "Luego, conecta \nel segundo patchcord del puerto Output del atenuador\n al puerto RX del NetXpert.";
                yield return new WaitForSeconds(5f);
                instructionText.text = "El indicador se pondrá en <color=green>verde</color>\n cuando apuntes a los extremos del cable.";
                yield return new WaitForSeconds(5f);
                instructionText.text = "Luego, al presionar el botón A, lo podrás agarrar.";
                yield return new WaitForSeconds(5f);
                instructionText.text = "Con el conector en mano,\n apunta hacia el dispositivo que deseés conectar.";
                yield return new WaitForSeconds(5f);
                instructionText.text = "El marcador se pondrá en <color=red>rojo</color>\n cuando apuntes a un dispositivo.";
                yield return new WaitForSeconds(5f);
                instructionText.text =
                    "<b>Recapitulando:</b>\n" +
                    "Apunta al extremo del cable (<color=green>verde</color>), presiona <b>A</b> para tomarlo,\n" +
                    "apunta al puerto (<color=red>rojo</color>) y presiona <b>A</b> para conectar.\n" +
                    "<size=85%>TX → Input   |   Output → RX</size>";
                // El Update() en case 13 espera a que LinkState cambie de Disconnected
                break;

            case 14: // Cables conectados → instrucción de configurar atenuador bajo
                instructionText.text = "¡Enlace establecido! \nAhora configura el atenuador.";
                yield return new WaitForSeconds(3f);

                atenuadorHighlight.Highlight(true);
                instructionText.text = "Abre el <color=yellow>atenuador EXFO</color> \ny configura la atenuación a -5 dB \n (usa el botón Reset si lo necesitas).";
                break;

            // ──────────────────────────────────────────────
            // FASE 4: ESCENARIO NOMINAL (atenuación baja)
            // ──────────────────────────────────────────────

            case 15: // Esperando configuración del atenuador (polling en Update)
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

            case 16: // Atenuador configurado bajo → pedir Quick Test
                atenuadorHighlight.Highlight(false);
                instructionText.text = "Atenuador configurado. \n Ahora abre el <color=yellow>NetXpert XG SE</color> y ejecuta un Quick Test.";

                berHighlight.Highlight(true);
                waitingForQuickTest = true;
                quickTestCompleted = false;
                break;

            case 17: // Quick Test nominal completado → pedir BER Test
                instructionText.text = "El enlace soporta 10 Gbps. \nAhora ejecuta un BER Test completo para verificar la calidad.";

                waitingForBERTest = true;
                berTestCompleted = false;
                break;

            case 18: // BER Test nominal completado → instrucción de subir atenuación
                berHighlight.Highlight(false);
                instructionText.text = "Excelente.\n El enlace muestra muy pocos o ningún error. \n El BER es menor a 10^-12, \nlo cual cumple con el estándar IEEE 802.3an.";
                yield return new WaitForSeconds(5f);

                instructionText.text = "Ahora veremos qué ocurre cuando el enlace se degrada. \nSal del equipo.";
                break;

            // ──────────────────────────────────────────────
            // FASE 5: ESCENARIO DEGRADADO (atenuación alta)
            // ──────────────────────────────────────────────

            case 19: // Instrucción de subir atenuación
                atenuadorHighlight.Highlight(true);
                instructionText.text = "Usa el <color=yellow>atenuador EXFO</color> y sube la atenuación a -25 dB. \n Esto simulará un enlace con problemas.";

                // El Update() verifica CheckAttenuatorHighAttenuation()
                break;

            case 20: // Atenuador configurado alto → pedir Quick Test
                atenuadorHighlight.Highlight(false);
                instructionText.text = "Atenuación configurada a -25 dB.\n Abre el <color=yellow>NetXpert XG SE</color> y ejecuta un Quick Test.";

                berHighlight.Highlight(true);
                waitingForQuickTest = true;
                quickTestCompleted = false;
                break;

            case 21: // Quick Test degradado completado → pedir BER Test
                instructionText.text = "El enlace falla a 10 Gbps. \nEjecuta un BER Test completo\n para ver el impacto en detalle.";

                waitingForBERTest = true;
                berTestCompleted = false;
                break;

            case 22: // BER Test degradado completado → conclusión
                berHighlight.Highlight(false);
                instructionText.text = "Observa la diferencia: \n con alta atenuación, \n el BER aumentó drásticamente \ny el enlace no cumple con el estándar.";
                yield return new WaitForSeconds(6f);

                instructionText.text =
                    "Dirígete hacia la <b>PC</b> para finalizar la práctica.\n\n" +
                    "<size=70%>Observa alrededor de tu entorno — la PC iluminada en amarillo\n" +
                    "indica el lugar de salida. Mira hacia arriba para leer las instrucciones.</size>";
                allowFocusMode = false;
                if (pcHighlight != null) pcHighlight.Highlight(true);
                Debug.Log("[P3-DEBUG] case22 completado, llamando NextStep() hacia case23");

                StartCoroutine(NextStep());
                break;

            // ──────────────────────────────────────────────
            // FASE 6: CHECKOUT
            // ──────────────────────────────────────────────

            case 23: // Esperando que llegue al checkout (polling en Update)
                Debug.Log($"[P3-DEBUG] Entró a case23 (step={step}). targetCheckout={(targetCheckout != null ? "OK" : "NULL")} player={(player != null ? "OK" : "NULL")}");
                break;

            case 24: // Llegó al checkout → cargar cuestionario
                instructionText.text = "Práctica completada. Cargando cuestionario...";
                yield return new WaitForSeconds(2f);

                // Detener Cardboard/XR antes de cargar la escena 2D del cuestionario
                var xrMgr = XRGeneralSettings.Instance?.Manager;
                if (xrMgr != null && xrMgr.isInitializationComplete)
                {
                    xrMgr.StopSubsystems();
                    xrMgr.DeinitializeLoader();
                }
                yield return null; // un frame para que XR termine de cerrarse

                SceneManager.LoadScene(nombreEscenaCuestionario);
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
        BERTesterController.OnBERStarted += HandleBERStarted;
        InstrumentUIScreenManager.OnBERPoweredOn += HandleBERPoweredOn;

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
        BERTesterController.OnBERStarted -= HandleBERStarted;
        InstrumentUIScreenManager.OnBERPoweredOn -= HandleBERPoweredOn;

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
        if (step == 14)
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
    /// Se dispara cuando el alumno abre el BERT (entra en FocusMode).
    /// </summary>
    void HandleBERStarted()
    {
        Debug.Log($"[P3-DEBUG] HandleBERStarted disparado. step actual = {step} (objeto={name})", this);
        if (step == 5)
        {
            StartCoroutine(NextStep());
        }
    }

    /// <summary>
    /// Se dispara cuando el alumno realmente presiona el botón de encendido del BERT.
    /// </summary>
    void HandleBERPoweredOn()
    {
        if (step == 6)
        {
            botonPowerBERHighlight.Highlight(false);
            StartCoroutine(NextStep());
        }
    }

    /// <summary>
    /// Se dispara cuando el alumno SALE del BERT (cierra el instrumento).
    /// </summary>
    void HandleBERExited()
    {
        // Step 12: sale del BERT después de probar escenario Disconnected
        if (step == 12)
        {
            StartCoroutine(NextStep());
        }

        // Step 18: sale del BERT después de escenario Nominal
        if (step == 18)
        {
            StartCoroutine(NextStep());
        }

        // Step 22 no necesita handler porque ya mostró conclusión
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