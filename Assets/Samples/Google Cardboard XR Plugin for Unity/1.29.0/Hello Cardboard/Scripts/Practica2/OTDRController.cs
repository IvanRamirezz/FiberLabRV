using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// ════════════════════════════════════════════════════════════════
// OTDRController.cs
// Controlador principal del OTDR virtual (Práctica 2).
// Sigue el mismo patrón de BERTesterController:
//   - Implementa IFocusable para integrarse con FocusModeManager
//   - Usa eventos estáticos para comunicarse con el InstructionManager
//   - Gestiona la carga de trazas y la lógica de interacción
//
// Quiz y Feedback se retiraron: las preguntas de predicción se
// resuelven en el cuestionario escrito de la práctica.
// ════════════════════════════════════════════════════════════════

public class OTDRController : MonoBehaviour, IFocusable
{
    public static OTDRController Instance { get; private set; }

    // ──────────────── EVENTOS PARA P2_InstructionManager ────────────────
    // Reemplaza la declaración actual: el manager necesita saber QUÉ se midió.
    public static event System.Action<int, int> OnMeasurementCompleted;  // fiberIdx, longitud de onda
    public static event System.Action OnAllFibersMeasured;
    public static event System.Action OnOTDROpened;
    public static event System.Action OnOTDRExited;
    // El evento ahora lleva los datos del evento, no solo el índice:
    // el manager necesita el tipo y la descripción para explicar términos.
    public static event System.Action<OTDREventData> OnEventDetailOpened;
    public static event System.Action<string> OnZoomChanged;   // "complete", "zoom_left", "zoom_right"

    // ── Análisis, no solo medición ──────────────────────────────
    // Una fibra cuenta como analizada cuando el alumno abrió al menos
    // una tarjeta de detalle. Medir y salir ya no basta.
    bool[] fiberAnalyzed = new bool[6];
    int fibersAnalyzed = 0;
    public int AnalyzedFiberCount => fibersAnalyzed;

    void MarkCurrentFiberAnalyzed()
    {
        int idx = dropdownFiber != null ? dropdownFiber.value : 0;
        if (fiberAnalyzed[idx]) return;

        fiberAnalyzed[idx] = true;
        fibersAnalyzed++;

        RefreshFiberDropdown();
        UpdateFiberCounter();

        if (fibersAnalyzed >= fiberNames.Length)
            OnAllFibersMeasured?.Invoke();
    }
    // Contador para el polling de la fase autónoma.
    public int MeasuredFiberCount => fibersCompleted;

    // ──────────────── UI REFERENCES ────────────────
    [Header("Focus Mode")]
    public GameObject otdrUI;              // Canvas World Space del OTDR

    [Header("Navigation")]
    public GameObject firstSelectedButton; // Control seleccionado al entrar (el de encendido)

    [Header("Screens")]
    public OTDRUIScreenManager screens;

    [Header("Setup Screen")]
    public TMP_Dropdown dropdownFiber;
    public TMP_Dropdown dropdownWavelength;
    //public TMP_Dropdown dropdownRange;
    //public TMP_Text labelSORFilename;      // Opcional: nombre compuesto del .sor

    [Header("Progress Screen")]
    public TMP_Text progressText;
    [Tooltip("Duración simulada de la adquisición, en segundos.")]
    public float measurementDuration = 1.5f;

    [Header("Trace Screen")]
    public Image traceImage;               // Imagen donde se muestra la traza
    public GameObject eventMarkerPrefab;   // Prefab del botón marcador de evento
    public Transform eventMarkerParent;    // Parent dentro del área de la traza
    public RectTransform traceAreaRect;    // RectTransform del área de la traza

    [Tooltip("Botón de la columna al que salen los marcadores con el joystick.")]
    public Button botonSalidaMarcadores;   // normalmente BotonReset

    [Header("Geometría de la traza")]
    [Tooltip("Altura del riel de marcadores, 0 = abajo, 1 = arriba del área.")]
    [Range(0f, 1f)] public float markerRailY = 0.88f;
    [Tooltip("Usados solo si el JSON no trae plot_left / plot_right.")]
    [Range(0f, 0.5f)] public float plotLeftFallback = 0.12f;
    [Range(0.5f, 1f)] public float plotRightFallback = 0.96f;

    [Header("Event Detail Popup")]
    public GameObject eventDetailPanel;
    public Button botonCerrarDetalle;
    public TMP_Text textEventType;
    public TMP_Text textEventDistance;
    public TMP_Text textEventLoss;
    public TMP_Text textEventReflectance;
    public TMP_Text textEventDescription;
    public TMP_Text textCount;
    [Header("Event Table Screen")]
    public Transform eventTableContent;
    public GameObject eventTableRowPrefab;
    public Color rowColorA = new Color(0.92f, 0.92f, 0.95f);
    public Color rowColorB = new Color(0.86f, 0.86f, 0.91f);

    [Header("General UI")]
    public TMP_Text fiberCounterText;      // "3/6"

    [Header("Rutas en Resources")]
    public string carpetaTrazas = "trazas_otdr_new";
    public string carpetaDatos = "otdr_datos";

    // ──────────────── STATE ────────────────
    OTDRScenarioData currentScenario;
    int currentWavelength = 1310;
    string currentZoomLevel = "complete";
    int fibersCompleted = 0;
    bool[] fiberMeasured = new bool[6];

    GameObject lastSelectedBeforeDetail;

    List<GameObject> spawnedMarkers = new List<GameObject>();
    List<GameObject> spawnedTableRows = new List<GameObject>();

    // Nombres de archivo, tal como los genera MATLAB. Sin acentos.
    readonly string[] fiberNames = {
        "F01_Azul", "F02_Naranja", "F03_Verde",
        "F04_Cafe", "F05_Gris", "F06_Blanco"
    };

    // Nombres para mostrar en la interfaz. NO derivar uno del otro con
    // reemplazos de texto: el día que falle se depura cadenas, no fibra.
    readonly string[] fiberDisplayNames = {
        "F01 - Azul", "F02 - Naranja", "F03 - Verde",
        "F04 - Café", "F05 - Gris", "F06 - Blanco"
    };

    /// <summary>La fibra 6 es el escenario de evaluación del cuestionario escrito.</summary>
    public bool IsEvaluationFiber(int idx) => idx == 5;

    public bool IsDetailOpen =>
        eventDetailPanel != null && eventDetailPanel.activeSelf;

    // ──────────────── LIFECYCLE ────────────────

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        InitializeDropdowns();
        
    }

    // ──────────────── IFOCUSABLE ────────────────

    public void OpenFocus()
    {
        OpenOTDR();
    }

    public void OpenOTDR()
    {
        FocusModeManager.Instance.Enter(otdrUI);
        UpdateFiberCounter();
        SelectFirstButton();
        OnOTDROpened?.Invoke();
    }

    public void ExitOTDR()
    {
        OnOTDRExited?.Invoke();
        FocusModeManager.Instance.Exit();
    }

    // ──────────────── DROPDOWN SETUP ────────────────

    void InitializeDropdowns()
    {
        if (dropdownFiber != null)
        {
            dropdownFiber.ClearOptions();
            dropdownFiber.AddOptions(BuildFiberOptions());
            //dropdownFiber.onValueChanged.AddListener(delegate { UpdateSORLabel(); });
        }

        if (dropdownWavelength != null)
        {
            dropdownWavelength.ClearOptions();
            dropdownWavelength.AddOptions(new List<string> { "1310 nm", "1550 nm" });
            //dropdownWavelength.onValueChanged.AddListener(delegate { UpdateSORLabel(); });
        }

        // El rango no altera la traza: las imágenes ya vienen generadas con
        // su propia escala. Se mantiene por fidelidad con el equipo real.
        //if (dropdownRange != null)
        //{
        //    dropdownRange.ClearOptions();
        //    dropdownRange.AddOptions(new List<string> { "5 km", "25 km", "100 km" });
        //}
    }

    List<string> BuildFiberOptions()
    {
        List<string> opciones = new List<string>();
        for (int i = 0; i < fiberDisplayNames.Length; i++)
        {
            string prefix = fiberAnalyzed[i] ? "✅" : "";
            opciones.Add(prefix + fiberDisplayNames[i]);
        }
        return opciones;
    }

    /*void UpdateSORLabel()
    {
        if (dropdownFiber == null || dropdownWavelength == null) return;

        currentWavelength = dropdownWavelength.value == 0 ? 1310 : 1550;

        if (labelSORFilename != null)
        {
            string filename = $"{fiberNames[dropdownFiber.value]}_{currentWavelength}nm.sor";
            labelSORFilename.text = filename;
        }
    }*/

    void RefreshFiberDropdown()
    {
        if (dropdownFiber == null) return;

        int currentValue = dropdownFiber.value;
        dropdownFiber.ClearOptions();
        dropdownFiber.AddOptions(BuildFiberOptions());
        dropdownFiber.value = currentValue;
    }

    // ──────────────── MEASUREMENT FLOW ────────────────

    /// <summary>Llamado por el botón "Start" del Setup.</summary>
    public void StartMeasurement()
    {
        StartCoroutine(MeasurementRoutine());
    }

    IEnumerator MeasurementRoutine()
    {
        int fiberIdx = dropdownFiber != null ? dropdownFiber.value : 0;
        currentWavelength = (dropdownWavelength != null && dropdownWavelength.value == 1) ? 1550 : 1310;
        currentZoomLevel = "complete";

        LoadScenarioData(fiberIdx);

        if (screens != null) screens.ShowProgress();
        if (progressText != null) progressText.text = "Adquiriendo traza...";

        yield return new WaitForSeconds(measurementDuration);

        LoadTraceImage(fiberIdx);

        // La pantalla Trace debe estar ACTIVA antes de posicionar los
        // marcadores: el RectTransform de un objeto inactivo no tiene
        // medidas fiables y todos caerían en cero.
        if (screens != null) screens.ShowTrace();
        yield return null;                 // un frame para que Unity calcule el layout
        Canvas.ForceUpdateCanvases();

        SpawnEventMarkers();
        PopulateEventTable();
        MarkCurrentFiberComplete();

        OnMeasurementCompleted?.Invoke(fiberIdx, currentWavelength);
    }

    // ──────────────── DATA LOADING ────────────────

    void LoadScenarioData(int fiberIdx)
    {
        string jsonPath = $"{carpetaDatos}/{fiberNames[fiberIdx]}_datos";
        TextAsset jsonAsset = Resources.Load<TextAsset>(jsonPath);

        if (jsonAsset == null)
        {
            Debug.LogError($"[OTDR] No se encontró el JSON: Resources/{jsonPath}");
            currentScenario = null;
            return;
        }

        currentScenario = JsonUtility.FromJson<OTDRScenarioData>(jsonAsset.text);

        if (currentScenario != null && (currentScenario.vistas == null || currentScenario.vistas.Count == 0))
        {
            Debug.LogError($"[OTDR] El JSON de {fiberNames[fiberIdx]} no trae el bloque 'vistas'. " +
                           "Regenera los datos con la versión actual del script de MATLAB.");
        }
    }

    void LoadTraceImage(int fiberIdx)
    {
        string nombre = $"{fiberNames[fiberIdx]}_{currentWavelength}nm_{currentZoomLevel}";
        Sprite traza = Resources.Load<Sprite>($"{carpetaTrazas}/{nombre}");

        if (traza == null)
        {
            Debug.LogError($"[OTDR] No se encontró la traza: Resources/{carpetaTrazas}/{nombre}");
            return;
        }

        if (traceImage != null) traceImage.sprite = traza;
    }

    // ──────────────── GEOMETRÍA DE MARCADORES ────────────────

    /// <summary>Vista (rango en km) que corresponde al nivel de zoom actual.</summary>
    OTDRVista GetCurrentView()
    {
        if (currentScenario == null || currentScenario.vistas == null) return null;

        OTDRVista v = currentScenario.vistas.Find(x => x.id == currentZoomLevel);
        if (v == null)
            Debug.LogError($"[OTDR] El JSON no define la vista '{currentZoomLevel}'.");

        return v;
    }

    /// <summary>
    /// Convierte la distancia de un evento en una posición dentro del área
    /// de la traza. Devuelve false si el evento cae fuera de la vista actual.
    ///
    /// No se usa posicion_normalizada_x porque está calculada sobre z_total
    /// y solo es correcta en la vista completa.
    /// </summary>
    bool TryGetMarkerPosition(OTDREventData ev, out Vector2 pos)
    {
        pos = Vector2.zero;

        OTDRVista v = GetCurrentView();
        if (v == null || traceAreaRect == null) return false;

        float span = v.fin_km - v.inicio_km;
        if (span <= 0f) return false;

        float t = (ev.distancia_km - v.inicio_km) / span;
        if (t < 0f || t > 1f) return false;

        // Los ejes y etiquetas del PNG ocupan márgenes: el área graficada
        // no cubre el 100% de la imagen.
        float left = currentScenario.plot_left > 0f ? currentScenario.plot_left : plotLeftFallback;
        float right = currentScenario.plot_right > 0f ? currentScenario.plot_right : plotRightFallback;

        float imgX = left + t * (right - left);

        float w = traceAreaRect.rect.width;
        float h = traceAreaRect.rect.height;

        // El pivot del marcador está centrado, de ahí el desplazamiento de 0.5.
        pos = new Vector2((imgX - 0.5f) * w, (markerRailY - 0.5f) * h);
        return true;
    }

    // ──────────────── EVENT MARKERS ────────────────

    void SpawnEventMarkers()
    {
        ClearEventMarkers();

        if (currentScenario == null || currentScenario.eventos == null)
        {
            Debug.LogWarning("[OTDR] SpawnEventMarkers: no hay escenario cargado.");
            return;
        }

        if (eventMarkerPrefab == null || eventMarkerParent == null)
        {
            Debug.LogWarning("[OTDR] SpawnEventMarkers: falta el prefab o el parent en el inspector.");
            return;
        }

        Debug.Log($"[OTDR] Instanciando {currentScenario.eventos.Count} marcadores. " +
                  $"TraceArea: {traceAreaRect.rect.width}x{traceAreaRect.rect.height}");

        for (int i = 0; i < currentScenario.eventos.Count; i++)
        {
            GameObject marker = Instantiate(eventMarkerPrefab, eventMarkerParent);
            marker.SetActive(true);

            int eventIndex = i;  // capturar para el closure
            Button markerButton = marker.GetComponent<Button>();
            if (markerButton != null)
                markerButton.onClick.AddListener(() => ShowEventDetail(eventIndex));

            TMP_Text markerLabel = marker.GetComponentInChildren<TMP_Text>();
            if (markerLabel != null)
                markerLabel.text = (i + 1).ToString();

            spawnedMarkers.Add(marker);
        }

        RepositionMarkers();
        SetupMarkerNavigation();
    }

    void ClearEventMarkers()
    {
        foreach (var marker in spawnedMarkers)
            if (marker != null) Destroy(marker);

        spawnedMarkers.Clear();
    }

    /// <summary>
    /// Recoloca los marcadores según el nivel de zoom actual y oculta los
    /// que quedan fuera de la vista.
    /// </summary>
    void RepositionMarkers()
    {
        if (currentScenario == null || currentScenario.eventos == null) return;

        for (int i = 0; i < spawnedMarkers.Count && i < currentScenario.eventos.Count; i++)
        {
            GameObject marker = spawnedMarkers[i];
            if (marker == null) continue;

            bool visible = TryGetMarkerPosition(currentScenario.eventos[i], out Vector2 pos);
            marker.SetActive(visible);

            if (!visible) continue;

            RectTransform rect = marker.GetComponent<RectTransform>();
            if (rect != null) rect.anchoredPosition = pos;
        }
    }

    /// <summary>
    /// Cierra la navegación por joystick entre marcadores y deja una salida
    /// hacia la columna de botones. Sin la salida, el alumno entra al área
    /// de la traza y no puede volver a los controles.
    /// </summary>
    void SetupMarkerNavigation()
    {
        List<Button> visibles = new List<Button>();
        foreach (var m in spawnedMarkers)
        {
            if (m == null || !m.activeSelf) continue;
            Button b = m.GetComponent<Button>();
            if (b != null) visibles.Add(b);
        }

        for (int i = 0; i < visibles.Count; i++)
        {
            Navigation nav = new Navigation { mode = Navigation.Mode.Automatic   };

            //if (i > 0) nav.selectOnLeft = visibles[i - 1];
            //else nav.selectOnLeft = botonSalidaMarcadores;   // salida a la columna

            //if (i < visibles.Count - 1) nav.selectOnRight = visibles[i + 1];

            visibles[i].navigation = nav;
        }

        // Entrada desde la columna hacia el primer marcador.
        if (botonSalidaMarcadores != null && visibles.Count > 0)
        {
            Navigation navCol = botonSalidaMarcadores.navigation;
            navCol.mode = Navigation.Mode.Explicit;
            navCol.selectOnRight = visibles[0];
            botonSalidaMarcadores.navigation = navCol;
        }

        // No se toca la selección actual: de eso se encarga el gestor de
        // pantallas al mostrar el panel.
    }

    // ──────────────── EVENT DETAIL POPUP ────────────────

    public void ShowEventDetail(int eventIndex)
    {
        if (currentScenario == null || currentScenario.eventos == null) return;
        if (eventIndex < 0 || eventIndex >= currentScenario.eventos.Count) return;

        OTDREventData evt = currentScenario.eventos[eventIndex];

        if (textEventType != null) textEventType.text = GetEventTypeDisplay(evt.tipo);
        if (textEventDistance != null) textEventDistance.text = $"{evt.distancia_km:F3} km";
        if (textEventLoss != null) textEventLoss.text = FormatLoss(evt);
        if (textEventReflectance != null) textEventReflectance.text = FormatReflectance(evt);
        if (textEventDescription != null) textEventDescription.text = evt.descripcion;
        if (textCount != null) textCount.text = $"{eventIndex + 1} de {currentScenario.eventos.Count}";
        //HighlightMarker(eventIndex);

        // Guardar el foco anterior ANTES de mover la selección, para poder
        // devolverlo al cerrar.
        if (EventSystem.current != null)
            lastSelectedBeforeDetail = EventSystem.current.currentSelectedGameObject;

        if (eventDetailPanel != null) eventDetailPanel.SetActive(true);

        if (botonCerrarDetalle != null)
            SetSelection(botonCerrarDetalle.gameObject);

        MarkCurrentFiberAnalyzed();
        OnEventDetailOpened?.Invoke(evt);
    }

    public void HideEventDetail()
    {
        if (eventDetailPanel != null) eventDetailPanel.SetActive(false);

        if (lastSelectedBeforeDetail != null && lastSelectedBeforeDetail.activeInHierarchy)
            SetSelection(lastSelectedBeforeDetail);
        else if (botonSalidaMarcadores != null)
            SetSelection(botonSalidaMarcadores.gameObject);

        lastSelectedBeforeDetail = null;
    }

    /*void HighlightMarker(int eventIndex)
    {
        for (int i = 0; i < spawnedMarkers.Count; i++)
        {
            if (spawnedMarkers[i] == null) continue;

            Image img = spawnedMarkers[i].GetComponent<Image>();
            if (img == null) continue;

            img.color = (i == eventIndex)
                ? new Color(1f, 0.6f, 0.1f, 1f)    // resaltado
                : new Color(0.9f, 0.3f, 0.1f, 1f); // normal
        }
    }*/

    // ──────────────── FORMATO DE VALORES ────────────────

    string GetEventTypeDisplay(string tipo)
    {
        switch (tipo)
        {
            case "no_reflectivo": return "Evento no reflectivo";
            case "reflectivo": return "Evento reflectivo";
            case "rotura": return "Rotura de fibra";
            default: return tipo;
        }
    }

    /// <summary>
    /// El JSON marca con -1 la pérdida de una rotura. Un OTDR real tampoco
    /// reporta cifra ahí: no queda señal con la cual medirla.
    /// </summary>
    string FormatLoss(OTDREventData evt)
    {
        float loss = currentWavelength == 1310 ? evt.perdida_dB_1310 : evt.perdida_dB_1550;

        if (evt.tipo == "rotura" || loss < 0f) return "Pérdida total";
        return $"{loss:F2} dB";
    }

    /// <summary>
    /// La reflectancia ya viene NEGATIVA del JSON; 0 significa "no aplica".
    /// </summary>
    string FormatReflectance(OTDREventData evt)
    {
        if (evt.reflectancia_dB < 0f) return $"{evt.reflectancia_dB:F1} dB";
        return "—";
    }

    // ──────────────── EVENT TABLE ────────────────

    public void PopulateEventTable()
    {
        ClearEventTable();

        if (currentScenario == null || currentScenario.eventos == null) return;
        if (eventTableContent == null || eventTableRowPrefab == null) return;

        for (int i = 0; i < currentScenario.eventos.Count; i++)
        {
            OTDREventData evt = currentScenario.eventos[i];

            GameObject row = Instantiate(eventTableRowPrefab, eventTableContent);
            row.SetActive(true);

            // Fondo alterno: en Cardboard las líneas divisorias delgadas
            // producen aliasing y se leen peor que las bandas de color.
            Image rowBackground = row.GetComponent<Image>();
            if (rowBackground != null)
                rowBackground.color = (i % 2 == 0) ? rowColorA : rowColorB;

            // Orden esperado en la jerarquía del prefab:
            // [No, Distancia, Tipo, Pérdida]. La reflectancia se dejó fuera
            // de la tabla: cinco columnas no caben legibles en media pantalla
            // por ojo. Vive en la tarjeta de detalle.
            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>();
            if (texts.Length >= 4)
            {
                texts[0].text = (i + 1).ToString();
                texts[1].text = $"{evt.distancia_km:F2} km";
                texts[2].text = GetEventTypeDisplay(evt.tipo);
                texts[3].text = FormatLoss(evt);
            }
            else
            {
                Debug.LogWarning($"[OTDR] El prefab de fila tiene {texts.Length} TMP_Text; se esperaban 4.");
            }

            int eventIndex = i;
            Button rowButton = row.GetComponent<Button>();
            if (rowButton != null)
                rowButton.onClick.AddListener(() =>
                {
                    // La tarjeta de detalle vive dentro del panel Trace. Hay que
                    // mostrarlo antes, o el EventSystem no puede pasarle el foco
                    // y la navegación se queda sin objeto seleccionado.
                    if (screens != null) screens.ShowTrace();
                    ShowEventDetail(eventIndex);
                });

            spawnedTableRows.Add(row);
        }
    }

    void ClearEventTable()
    {
        foreach (var row in spawnedTableRows)
            if (row != null) Destroy(row);

        spawnedTableRows.Clear();
    }

    // ──────────────── TRACE NAVIGATION (ZOOM) ────────────────



    public void PanLeft()
    {
        if (currentZoomLevel != "zoom_left") ApplyZoom("zoom_left");
    }

    public void PanRight()
    {
        if (currentZoomLevel != "zoom_right") ApplyZoom("zoom_right");
    }

    public void ResetView()
    {
        if (currentZoomLevel != "complete") ApplyZoom("complete");
    }

    void ApplyZoom(string zoomLevel)
    {
        currentZoomLevel = zoomLevel;
        LoadTraceImage(dropdownFiber != null ? dropdownFiber.value : 0);
        RepositionMarkers();
        SetupMarkerNavigation();

        // Si el foco estaba en un marcador que ya no se muestra, el joystick
        // se queda sin objeto seleccionado.
        GameObject actual = EventSystem.current != null
            ? EventSystem.current.currentSelectedGameObject : null;

        if (actual == null || !actual.activeInHierarchy)
            SetSelection(botonSalidaMarcadores != null ? botonSalidaMarcadores.gameObject : null);
        OnZoomChanged?.Invoke(zoomLevel);
    }

    // ──────────────── FIBER COMPLETION TRACKING ────────────────

    /// <summary>Marca la fibra actual como medida y actualiza el contador.</summary>
    public void MarkCurrentFiberComplete()
    {
        int fiberIdx = dropdownFiber != null ? dropdownFiber.value : 0;

        if (!fiberMeasured[fiberIdx])
        {
            fiberMeasured[fiberIdx] = true;
            fibersCompleted++;
        }

        RefreshFiberDropdown();
        UpdateFiberCounter();

        //if (fibersCompleted >= fiberNames.Length)
        //    OnAllFibersMeasured?.Invoke();
    }

    void UpdateFiberCounter()
    {
        if (fiberCounterText != null)
            fiberCounterText.text = $"{fibersAnalyzed}/{fiberNames.Length}";
    }

    public bool AreAllFibersMeasured()
    {
        return fibersAnalyzed >= fiberNames.Length;
    }

    // ──────────────── HELPERS ────────────────

    void SelectFirstButton()
    {
        SetSelection(firstSelectedButton);
    }

    /// <summary>
    /// Unity ignora la asignación si el objeto ya estaba seleccionado,
    /// por eso el paso previo por null.
    /// </summary>
    void SetSelection(GameObject target)
    {
        if (EventSystem.current == null) return;

        EventSystem.current.SetSelectedGameObject(null);
        if (target != null)
            EventSystem.current.SetSelectedGameObject(target);
    }
}