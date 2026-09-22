using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System;

// ══════════════════════════════════════════════════════════════
// InstructionManagerPrac2.cs
// Guía paso a paso de la Práctica 2: análisis de un cable de seis
// fibras con OTDR.
//
// Misma estructura que InstructionManagerPrac3:
//   - Update() hace polling de los pasos posicionales y de estado
//   - NextStep() es una corrutina secuencial con el texto de cada paso
//   - Los eventos estáticos de los equipos disparan los avances
//
// Además de la barra de instrucciones, usa un TMP secundario para
// explicar términos según el evento que el alumno abre. Así la
// retroalimentación llega cuando la necesita y no como un bloque
// teórico al principio.
// ══════════════════════════════════════════════════════════════

public class InstructionManagerPrac2 : MonoBehaviour
{
    public static InstructionManagerPrac2 Instance;

    // ══════════════════════════════════════════════════════════
    // REFERENCIAS DE EQUIPOS
    // ══════════════════════════════════════════════════════════

    [Header("OTDR")]
    [SerializeField] OTDRController otdr;
    public Highlightable otdrHighlight;
    public UIHighlightable botonPowerHighlight;      // Encendido del equipo
    public UIHighlightable menuSetupHighlight;       // Barra inferior: Setup
    public UIHighlightable menuInfoHighlight;        // Barra inferior: Information
    public UIHighlightable menuTraceHighlight;       // Barra inferior: Trace
    public UIHighlightable menuEventTableHighlight;  // Barra inferior: Event Table
    public UIHighlightable botonMedirHighlight;      // Botón de medir del Setup
    public UIHighlightable botonZoomDerechoHighlight;
    public UIHighlightable botonVistaCompletaHighlight;

    [Header("Cable de seis fibras")]
    public Highlightable cableHighlight;

    // ══════════════════════════════════════════════════════════
    // REFERENCIAS DE NAVEGACIÓN
    // ══════════════════════════════════════════════════════════

    [Header("Referencias generales")]
    public Transform player;
    public TextMeshProUGUI instructionText;

    [Header("Retroalimentación contextual")]
    [Tooltip("TMP secundario para explicar términos al abrir un evento.")]
    public TextMeshProUGUI explanationText;
    public float explanationDuration = 10f;

    [Header("Punto de llegada: mesa del OTDR")]
    public Transform targetMesa;
    public float triggerRadiusMesa = 1.5f;

    [Header("Punto de checkout (fin de práctica)")]
    public Transform targetCheckout;
    public float triggerRadiusCheckout = 1.5f;

    [Header("Escena del cuestionario")]
    public string nombreEscenaCuestionario = "Cuestionario_P2";

    // ══════════════════════════════════════════════════════════
    // ESTADO INTERNO
    // ══════════════════════════════════════════════════════════

    private int step = 0;
    public bool allowFocusMode;

    // Índices de fibra según el orden del dropdown
    const int FIBRA_AZUL = 0;

    // La fase autónoma termina cuando cinco fibras quedan ANALIZADAS
    // (medidas y con al menos una tarjeta de detalle abierta).
    const int FIBRAS_ANTES_DE_EVALUACION = 5;

    private Coroutine explicacionActiva;

    // ══════════════════════════════════════════════════════════
    // MAPA DE PASOS
    // ══════════════════════════════════════════════════════════
    //
    // Step 0:  Inicio → alumno camina hacia la mesa del OTDR
    // Step 1:  Llegó → reconocimiento del OTDR
    // Step 2:  Reconocimiento del cable y enlace con la Práctica 1
    // Step 3:  Instrucción de abrir el OTDR (espera OnOTDROpened)
    // Step 4:  Espera el botón de encendido (OnPowerChanged)
    // Step 5:  Pantalla Setup explicada → pide Information
    // Step 6:  Pantalla Information → pide volver a Setup
    // Step 7:  Mide F01-Azul a 1310 nm
    // Step 8:  Lectura de la traza → pide zoom a la mitad derecha
    // Step 9:  Observa el fin de fibra de cerca → pide vista completa
    // Step 10: Pide abrir el marcador del evento
    // Step 11: Tarjeta leída → pide la tabla de eventos
    // Step 12: Mide la MISMA fibra a 1550 nm
    // Step 13: Comparación 1310/1550 → fase autónoma (fibras 2 a 5)
    // Step 14: Fase de evaluación: fibra blanca
    // Step 15: Conclusión → dirigirse al checkout
    // Step 16: Llegó al checkout → cargar escena del cuestionario
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
        instructionText.text = "Instrucciones: Dirígete hacia la mesa del OTDR.";

        if (explanationText != null) explanationText.text = "";
    }

    // ══════════════════════════════════════════════════════════
    // UPDATE — Polling para pasos basados en posición o estado
    // ══════════════════════════════════════════════════════════

    void Update()
    {
        switch (step)
        {
            case 0: // Esperando que el alumno llegue a la mesa
                if (targetMesa != null &&
                    Vector3.Distance(player.position, targetMesa.position) <= triggerRadiusMesa)
                {
                    StartCoroutine(NextStep());
                }
                break;

            case 13: // Esperando que ANALICE las fibras 2 a 5
                if (otdr != null && otdr.AnalyzedFiberCount >= FIBRAS_ANTES_DE_EVALUACION)
                {
                    StartCoroutine(NextStep());
                }
                break;

            case 15: // Esperando que el alumno llegue al checkout
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

            case 1: // Llegó a la mesa → presentar el OTDR
                instructionText.text = "Has llegado a la mesa de trabajo. \nA continuación conocerás el equipo.";
                yield return new WaitForSeconds(3f);

                otdrHighlight.Highlight(true);
                instructionText.text = "Este es un <color=yellow>OTDR</color> (Reflectómetro Óptico \nen el Dominio del Tiempo). \nEnvía pulsos de luz por la fibra y analiza \nlo que regresa por retrodispersión.";
                yield return new WaitForSeconds(6f);

                instructionText.text = "Con él puedes ubicar empalmes, conectores \ny roturas, y saber a qué distancia están, \nsin acceder al otro extremo del cable.";
                yield return new WaitForSeconds(6f);

                otdrHighlight.Highlight(false);
                StartCoroutine(NextStep());
                break;

            case 2: // Reconocimiento del cable, enlazado con la Práctica 1
                cableHighlight.Highlight(true);
                instructionText.text = "Este es un <color=yellow>cable de cinco fibras</color>. \nEn la Práctica 1 identificaste los colores \nde los hilos según la norma TIA-598-C.";
                yield return new WaitForSeconds(7f);

                instructionText.text = "Ese color no es solo una etiqueta \npara ordenar el cable: cada hilo es \nun <color=yellow>canal de transmisión \nindependiente</color>.";
                yield return new WaitForSeconds(7f);

                instructionText.text = "Comparten la misma cubierta y el mismo \nrecorrido, pero cada uno tiene su propio \nestado. Uno puede estar intacto y \nel de al lado, roto.";
                yield return new WaitForSeconds(7f);

                instructionText.text = "Por eso se miden de uno en uno. \nVas a analizar los seis hilos y determinar \nel estado del cable completo.";
                yield return new WaitForSeconds(6f);

                cableHighlight.Highlight(false);
                StartCoroutine(NextStep());
                break;

            // ──────────────────────────────────────────────
            // FASE 2: ENCENDIDO Y EXPLORACIÓN DE LA INTERFAZ
            // ──────────────────────────────────────────────

            case 3: // Pedir que abra el OTDR
                allowFocusMode = true;
                otdrHighlight.Highlight(true);
                instructionText.text = "Instrucciones: Acércate al OTDR y ábrelo \npara usar su interfaz.";
                // Espera OnOTDROpened
                break;

            case 4: // Ya está dentro → pedir encendido
                otdrHighlight.Highlight(false);
                instructionText.text = "Estás frente a la interfaz del equipo. \nLa pantalla está apagada.";
                yield return new WaitForSeconds(3f);

                botonPowerHighlight.Highlight(true);
                instructionText.text = "Instrucciones: Presiona el botón de \n<color=yellow>encendido</color> para iniciar el OTDR.";
                // Espera OnPowerChanged(true)
                break;

            case 5: // Encendido → explicar la pantalla Setup
                botonPowerHighlight.Highlight(false);
                instructionText.text = "El equipo arranca en la pantalla \n<color=yellow>Setup</color>, donde se eligen los \nparámetros de la medición.";
                yield return new WaitForSeconds(5f);

                instructionText.text = "El índice de refracción está fijo en \n<color=yellow>1.4681</color>, el valor de una fibra \nmonomodo G.652. De él depende que \nlas distancias sean correctas.";
                yield return new WaitForSeconds(7f);

                instructionText.text = "El equipo trabaja en modo <color=yellow>Auto</color> \ncon promediado activo: elige solo el ancho \nde pulso y promedia varias adquisiciones \npara reducir el ruido.";
                yield return new WaitForSeconds(7f);

                menuInfoHighlight.Highlight(true);
                instructionText.text = "Instrucciones: Antes de medir, entra a \n<color=yellow>Information</color> para conocer \nlos tipos de evento.";
                // Espera OnScreenChanged(Info)
                break;

            case 6: // Pantalla Information
                menuInfoHighlight.Highlight(false);
                instructionText.text = "Aquí están los iconos que usa el equipo \npara cada tipo de evento.";
                yield return new WaitForSeconds(4f);

                instructionText.text = "Un evento <color=yellow>no reflectivo</color> es un \nescalón hacia abajo sin pico: típicamente \nun empalme por fusión o una curvatura.";
                yield return new WaitForSeconds(7f);

                instructionText.text = "Un evento <color=yellow>reflectivo</color> muestra un \npico hacia arriba antes del escalón. \nLo produce una interfaz aire-vidrio: \nun conector o una fibra rota.";
                yield return new WaitForSeconds(8f);

                menuSetupHighlight.Highlight(true);
                instructionText.text = "Instrucciones: Regresa a <color=yellow>Setup</color> \npara hacer tu primera medición.";
                // Espera OnScreenChanged(Setup)
                break;

            // ──────────────────────────────────────────────
            // FASE 3: PRIMERA MEDICIÓN GUIADA (FIBRA AZUL)
            // ──────────────────────────────────────────────

            case 7: // Pedir la primera medición
                menuSetupHighlight.Highlight(false);
                botonMedirHighlight.Highlight(true);
                instructionText.text = "Instrucciones: Selecciona la fibra \n<color=yellow>F01 - Azul</color> a <color=yellow>1310 nm</color> \ny ejecuta la medición.";
                // Espera OnMeasurementCompleted(FIBRA_AZUL, 1310)
                break;

            case 8: // Traza en pantalla → enseñar a leerla y usar el zoom
                botonMedirHighlight.Highlight(false);
                instructionText.text = "Esta es la traza de la fibra azul. \nEl eje horizontal es distancia en km \ny el vertical, potencia en dB.";
                yield return new WaitForSeconds(6f);

                instructionText.text = "La línea baja de forma constante: \nesa pendiente es la <color=yellow>atenuación</color> \nde la fibra, unos 0.35 dB por km \na 1310 nm.";
                yield return new WaitForSeconds(7f);

                instructionText.text = "Hacia el final hay un pico seguido de \nuna caída al ruido, pero en vista completa \nse ve comprimido contra el borde.";
                yield return new WaitForSeconds(6f);

                botonZoomDerechoHighlight.Highlight(true);
                instructionText.text = "Instrucciones: Presiona \n<color=yellow>Mitad derecha</color> para ampliar \nesa zona de la traza.";
                // Espera OnZoomChanged("zoom_right")
                break;

            case 9: // Zoom aplicado → explicar el fin de fibra y volver
                botonZoomDerechoHighlight.Highlight(false);
                instructionText.text = "Ahora se distingue con claridad: \nun pico y luego la caída al piso de ruido. \nEs el <color=yellow>fin de la fibra</color>.";
                yield return new WaitForSeconds(7f);

                instructionText.text = "El zoom no cambia la medición, solo \nla escala con que la ves. En campo se usa \npara separar eventos que en vista completa \nparecen uno solo.";
                yield return new WaitForSeconds(8f);

                botonVistaCompletaHighlight.Highlight(true);
                instructionText.text = "Instrucciones: Regresa a \n<color=yellow>Vista completa</color>.";
                // Espera OnZoomChanged("complete")
                break;

            case 10: // Pedir la tarjeta de detalle
                botonVistaCompletaHighlight.Highlight(false);
                instructionText.text = "Instrucciones: Selecciona el marcador \nnumerado sobre la traza para ver \nel detalle del evento.";
                // Espera OnEventDetailOpened
                break;

            case 11: // Detalle leído → mandar a la tabla
                instructionText.text = "La tarjeta muestra distancia, pérdida \ny reflectancia. La <color=yellow>reflectancia</color> \nsiempre es negativa: mientras más \ncercana a cero, peor el evento.";
                yield return new WaitForSeconds(8f);

                menuEventTableHighlight.Highlight(true);
                instructionText.text = "Instrucciones: Abre la \n<color=yellow>tabla de eventos</color> para ver \ntodos los eventos en forma de lista.";
                // Espera OnScreenChanged(EventTable)
                break;

            case 12: // Tabla vista → pedir la misma fibra a 1550
                menuEventTableHighlight.Highlight(false);
                instructionText.text = "Es el mismo contenido que los marcadores, \npero en el formato que entrega un reporte \nde campo.";
                yield return new WaitForSeconds(5f);

                menuSetupHighlight.Highlight(true);
                instructionText.text = "Instrucciones: Vuelve a Setup y mide la \n<color=yellow>misma fibra azul a 1550 nm</color>.";
                // Espera OnMeasurementCompleted(FIBRA_AZUL, 1550)
                break;

            // ──────────────────────────────────────────────
            // FASE 4: MEDICIÓN AUTÓNOMA
            // ──────────────────────────────────────────────

            case 13: // Comparación y arranque de la fase autónoma
                menuSetupHighlight.Highlight(false);
                instructionText.text = "Compara con la medición anterior: \nla pendiente es <color=yellow>menor</color>. \nA 1550 nm la fibra atenúa unos \n0.22 dB/km en vez de 0.35.";
                yield return new WaitForSeconds(8f);

                instructionText.text = "Por eso los enlaces largos usan 1550 nm. \nLos eventos, en cambio, siguen en \nla misma distancia: no dependen \nde la longitud de onda.";
                yield return new WaitForSeconds(8f);

                instructionText.text = "Ahora te toca a ti. Mide las fibras \n<color=yellow>naranja, verde, café y gris</color>, \ny abre el detalle de sus eventos.";
                yield return new WaitForSeconds(6f);

                instructionText.text = "Instrucciones: Una fibra cuenta como \nanalizada cuando abres al menos un evento. \nRevisa el contador en pantalla.";
                // Polling en Update de AnalyzedFiberCount
                break;

            // ──────────────────────────────────────────────
            // FASE 5: EVALUACIÓN
            // ──────────────────────────────────────────────

            case 14: // Fibra de evaluación
                instructionText.text = "Ya conoces cinco de las seis fibras \ndel cable. Encontraste un empalme limpio, \nun conector sucio, una macrocurvatura \ny una rotura.";
                yield return new WaitForSeconds(8f);

                instructionText.text = "Instrucciones: Analiza la fibra \n<color=yellow>F06 - Blanca</color> con calma. \nCuenta sus eventos, identifica el tipo \nde cada uno y a qué distancia están.";
                yield return new WaitForSeconds(7f);

                instructionText.text = "Instrucciones: El cuestionario final \npreguntará sobre esta fibra y ya no \ntendrás la traza a la vista. \nObsérvala con atención.";
                // Espera OnAllFibersMeasured (las seis analizadas)
                break;

            case 15: // Conclusión
                instructionText.text = "Has caracterizado el cable completo.";
                yield return new WaitForSeconds(3f);

                instructionText.text = "Un OTDR no solo dice si una fibra falla: \ndice <color=yellow>dónde</color> y <color=yellow>de qué tipo</color> \nes la falla. Eso es lo que permite \nreparar sin abrir todo el trayecto.";
                yield return new WaitForSeconds(8f);

                allowFocusMode = false;
                instructionText.text = "Instrucciones: Dirígete a la computadora \npara terminar la práctica.";
                // Polling de posición en Update
                break;

            case 16: // Checkout
                instructionText.text = "Práctica completada.";
                yield return new WaitForSeconds(2f);
                SceneManager.LoadScene(nombreEscenaCuestionario);
                break;
        }
    }

    // ══════════════════════════════════════════════════════════
    // SUSCRIPCIÓN A EVENTOS
    // ══════════════════════════════════════════════════════════

    void OnEnable()
    {
        OTDRController.OnOTDROpened += HandleOTDROpened;
        OTDRController.OnMeasurementCompleted += HandleMeasurementCompleted;
        OTDRController.OnEventDetailOpened += HandleEventDetailOpened;
        OTDRController.OnAllFibersMeasured += HandleAllFibersMeasured;
        OTDRController.OnZoomChanged += HandleZoomChanged;

        OTDRUIScreenManager.OnPowerChanged += HandlePowerChanged;
        OTDRUIScreenManager.OnScreenChanged += HandleScreenChanged;
    }

    void OnDisable()
    {
        OTDRController.OnOTDROpened -= HandleOTDROpened;
        OTDRController.OnMeasurementCompleted -= HandleMeasurementCompleted;
        OTDRController.OnEventDetailOpened -= HandleEventDetailOpened;
        OTDRController.OnAllFibersMeasured -= HandleAllFibersMeasured;
        OTDRController.OnZoomChanged -= HandleZoomChanged;

        OTDRUIScreenManager.OnPowerChanged -= HandlePowerChanged;
        OTDRUIScreenManager.OnScreenChanged -= HandleScreenChanged;
    }

    // ══════════════════════════════════════════════════════════
    // HANDLERS DE EVENTOS
    // ══════════════════════════════════════════════════════════

    /// <summary>El alumno entró al Focus Mode del OTDR.</summary>
    void HandleOTDROpened()
    {
        if (step == 3)
            StartCoroutine(NextStep());
    }

    /// <summary>El alumno presionó el botón de encendido.</summary>
    void HandlePowerChanged(bool encendido)
    {
        if (encendido && step == 4)
            StartCoroutine(NextStep());
    }

    /// <summary>
    /// Cambio de pantalla dentro del OTDR. Cada paso espera una
    /// pantalla distinta, de ahí el filtro por step Y por id.
    /// </summary>
    void HandleScreenChanged(OTDRScreenID id)
    {
        if (step == 5 && id == OTDRScreenID.Info) { StartCoroutine(NextStep()); return; }
        if (step == 6 && id == OTDRScreenID.Setup) { StartCoroutine(NextStep()); return; }
        if (step == 11 && id == OTDRScreenID.EventTable) { StartCoroutine(NextStep()); return; }
    }

    /// <summary>Cambio de nivel de zoom sobre la traza.</summary>
    void HandleZoomChanged(string zoomLevel)
    {
        if (step == 8 && zoomLevel == "zoom_right") { StartCoroutine(NextStep()); return; }
        if (step == 9 && zoomLevel == "complete") { StartCoroutine(NextStep()); return; }
    }

    /// <summary>
    /// Una medición terminó. Los pasos guiados exigen una fibra y una
    /// longitud de onda concretas: si el alumno mide otra cosa, la
    /// instrucción se mantiene en pantalla y no avanza.
    /// </summary>
    void HandleMeasurementCompleted(int fiberIdx, int wavelength)
    {
        if (step == 7 && fiberIdx == FIBRA_AZUL && wavelength == 1310)
        {
            StartCoroutine(NextStep());
            return;
        }

        if (step == 12 && fiberIdx == FIBRA_AZUL && wavelength == 1550)
        {
            StartCoroutine(NextStep());
            return;
        }
    }

    /// <summary>
    /// El alumno abrió la tarjeta de detalle de un evento. Además de
    /// avanzar el paso guiado, muestra la explicación del término en
    /// el TMP secundario: llega en el momento en que el alumno está
    /// mirando ese evento, no como teoría previa.
    /// </summary>
    void HandleEventDetailOpened(OTDREventData ev)
    {
        MostrarExplicacion(ExplicacionDeEvento(ev));

        if (step == 10)
            StartCoroutine(NextStep());
    }

    /// <summary>Las seis fibras quedaron analizadas.</summary>
    void HandleAllFibersMeasured()
    {
        if (step == 14)
            StartCoroutine(NextStep());
    }

    // ══════════════════════════════════════════════════════════
    // RETROALIMENTACIÓN CONTEXTUAL
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Texto de apoyo según el evento abierto. Se busca por palabra
    /// clave en la descripción que viene del JSON, con respaldo por
    /// tipo de evento.
    /// </summary>
    string ExplicacionDeEvento(OTDREventData ev)
    {
        if (ev == null) return "";

        string desc = ev.descripcion != null ? ev.descripcion.ToLower() : "";

        if (desc.Contains("macrocurvatura"))
            return "<color=yellow>Macrocurvatura:</color> la fibra está doblada con un \nradio demasiado cerrado y parte de la luz escapa del núcleo. \nAfecta mucho más a 1550 nm que a 1310 nm, \nporque la longitud de onda mayor viaja menos confinada.";

        if (desc.Contains("contaminado"))
            return "<color=yellow>Conector contaminado:</color> polvo o grasa en la cara del conector. \nAumenta la pérdida y empeora la reflectancia,\n porque la luz rebota en la suciedad.\n Es la falla más común en campo \ny casi siempre se corrige limpiando.";

        if (desc.Contains("empalme"))
            return "<color=yellow>Empalme por fusión:</color> dos fibras unidas fundiendo el vidrio.\n No hay interfaz aire-vidrio,\n así que no refleja: en la traza es un escalón sin pico. \nPor debajo de 0.1 dB se considera un empalme bien hecho.";

        if (desc.Contains("rotura"))
            return "<color=yellow>Rotura:</color> la fibra está cortada. \nEl extremo roto refleja con fuerza y después no queda señal: \nla traza cae al piso de ruido y ya no se recupera. \nNo se reporta pérdida en dB porque \nno hay nada que medir del otro lado.";

        if (desc.Contains("fin de fibra"))
            return "<color=yellow>Fin de fibra:</color> el último evento de la traza. \nEl extremo cortado del vidrio refleja y después solo queda ruido. \nSirve para confirmar la longitud real del enlace.";

        if (desc.Contains("conector"))
            return "<color=yellow>Conector:</color> unión mecánica entre dos fibras. \nSiempre deja una interfaz aire-vidrio, por eso refleja\n Un conector limpio pierde poco y tiene una reflectancia muy negativa, \ncerca de -45 dB."
                ;

        // Respaldo por tipo
        switch (ev.tipo)
        {
            case "reflectivo":
                return "<color=yellow>Evento reflectivo:</color> pico hacia arriba antes del escalón. Indica una interfaz aire-vidrio.";
            case "no_reflectivo":
                return "<color=yellow>Evento no reflectivo:</color> escalón hacia abajo sin pico. La luz se pierde sin rebotar.";
            default:
                return "";
        }
    }

    void MostrarExplicacion(string texto)
    {
        if (explanationText == null || string.IsNullOrEmpty(texto)) return;

        if (explicacionActiva != null) StopCoroutine(explicacionActiva);
        explicacionActiva = StartCoroutine(RutinaExplicacion(texto));
    }

    IEnumerator RutinaExplicacion(string texto)
    {
        explanationText.text = texto;
        yield return new WaitForSeconds(explanationDuration);
        explanationText.text = "";
        explicacionActiva = null;
    }
}