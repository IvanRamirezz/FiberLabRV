using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Tutorial de introducción al entorno VR.
/// Se ejecuta antes de cada práctica la primera vez. Si ya fue completado,
/// pregunta si el alumno quiere repetirlo.
/// Al terminar llama a PracticaSwitcher.Instance.ActivarPractica().
/// </summary>
public class InstructionManager_Demo : MonoBehaviour
{
    // ══════════════════════════════════════════════════════
    // REFERENCIAS UI
    // ══════════════════════════════════════════════════════

    [Header("UI — Instrucciones")]
    public TextMeshProUGUI instructionText;

    int _lastTouchPressId = -10;

    [Header("Panel botones (Paso 3 y pregunta de salto)")]
    public GameObject panelRespuestas;
    public Button[]   botones;         // mínimo 3 (para Paso 3); Paso de salto usa sólo 2

    [Header("Inicio del Demo — posición de arranque")]
    public Transform puntoInicioDemo;  // Empty frente a la mesa; define posición Y rotación
    public Transform cuerpoJugador;    // RealPlayer (Movimiento)
    public Transform camaraRig;        // Player (Camara) — necesario para rotar la vista

    [Header("Paso 1 — Objetos para mirar")]
    public Highlightable[] objetosMirar; // 2–3 objetos en escena con el componente Highlightable

    [Header("Paso 4 — Punto de destino (opcional — se salta si puntoMeta es null)")]
    public Transform     puntoMeta;
    public Highlightable destinoHighlight;
    public Transform     playerTransform;
    public float         radioMeta = 1.5f;

    [Header("Paso 4 — Cables a resaltar")]
    public Highlightable cable1Highlight;  // cuerpo del patchcord 1 → se pone amarillo
    public Highlightable cable2Highlight;  // cuerpo del patchcord 2 → se pone amarillo

    [Header("Paso 4 — Puntas agarrables (CableEnd)")]
    [Tooltip("Asigna la punta azul (CableEnd) de uno de los patchcords iluminados")]
    public CableEnd cableEnd1;
    [Tooltip("Asigna la punta azul (CableEnd) del segundo patchcord (opcional)")]
    public CableEnd cableEnd2;

    // ══════════════════════════════════════════════════════
    // CONSTANTES
    // ══════════════════════════════════════════════════════

    static readonly Color BTN_NORMAL  = new Color(0.20f, 0.40f, 0.70f, 1f);
    static readonly Color BTN_HOVERED = new Color(1.00f, 0.85f, 0.00f, 1f);
    const string PREFS_KEY = "demo_completada";

    // Variable auxiliar para leer resultado de corrutinas
    bool _resultadoPregunta;

    // ══════════════════════════════════════════════════════
    // ARRANQUE
    // ══════════════════════════════════════════════════════

    IEnumerator Start()
    {
        if (panelRespuestas != null) panelRespuestas.SetActive(false);

        bool yaCompletada = PlayerPrefs.GetInt(PREFS_KEY, 0) == 1;

        if (yaCompletada)
        {
            yield return StartCoroutine(PreguntarVerDemo());
            if (!_resultadoPregunta)
            {
                PracticaSwitcher.Instance?.ActivarPractica();
                yield break;
            }
        }

        yield return StartCoroutine(RunDemo());
    }

    // ══════════════════════════════════════════════════════
    // PREGUNTA ¿VER TUTORIAL?  (countdown — sin botones)
    // ══════════════════════════════════════════════════════

    IEnumerator PreguntarVerDemo()
    {
        float countdown = 6f;
        bool presionado = false;

        while (countdown > 0f && !presionado)
        {
            int segs = Mathf.CeilToInt(countdown);
            SetText(
                "Ya completaste el tutorial anteriormente.\n\n" +
                "<b>Presiona el control</b> para verlo de nuevo.\n\n" +
                $"<size=75%>O espera <b>{segs}s</b> para ir directo a la práctica...</size>");

            if (TouchInput.ButtonDown("Fire1", ref _lastTouchPressId))
                presionado = true;

            countdown -= Time.deltaTime;
            yield return null;
        }

        _resultadoPregunta = presionado; // true = ver demo, false = saltar
    }

    // ══════════════════════════════════════════════════════
    // FLUJO PRINCIPAL DEL DEMO
    // ══════════════════════════════════════════════════════

    IEnumerator RunDemo()
    {
        TeletransportarJugador();
        yield return StartCoroutine(Paso0_Bienvenida());
        yield return StartCoroutine(Paso1_Reticula());
        yield return StartCoroutine(Paso2_SeleccionarObjeto());
        yield return StartCoroutine(Paso3_BotonesRespuesta());
        yield return StartCoroutine(Paso4_ConectarCable());
        yield return StartCoroutine(Paso5_Desplazamiento());
        yield return StartCoroutine(Paso6_Cierre());

        PlayerPrefs.SetInt(PREFS_KEY, 1);
        PlayerPrefs.Save();

        PracticaSwitcher.Instance?.ActivarPractica();
    }

    // ─────────────────────────────────────────────
    // Paso 0 — Bienvenida
    // ─────────────────────────────────────────────

    IEnumerator Paso0_Bienvenida()
    {
        SetText(
            "Bienvenido al <b>Laboratorio Virtual de Fibra Óptica</b>.\n\n" +
            "Antes de comenzar tu práctica,\n" +
            "aprenderás a moverte e interactuar dentro del visor.\n\n" +
            "<size=70%>Presiona el botón del control para continuar.</size>");

        yield return StartCoroutine(WaitForConfirm());
    }

    // ─────────────────────────────────────────────
    // Paso 1 — La retícula
    // ─────────────────────────────────────────────

    IEnumerator Paso1_Reticula()
    {
        SetText(
            "El <b>punto blanco</b> en el centro de tu visión\n" +
            "es tu retícula — es tu \"cursor\" en VR.\n" +
            "Cuando apuntas a algo con lo que puedes interactuar,\n" +
            "<b>el punto se vuelve verde</b>.\n\n" +
            "Mira los objetos iluminados que aparecen frente a ti.");

        foreach (var obj in objetosMirar)
            if (obj != null) obj.Highlight(true);

        foreach (var obj in objetosMirar)
        {
            if (obj == null) continue;
            while (!EstaApuntandoA(obj))
            {
                HandInteraction.ReticleOverride = null;
                yield return null;
            }
            HandInteraction.ReticleOverride = Color.green;
            yield return new WaitForSeconds(0.5f);
        }

        foreach (var obj in objetosMirar)
            if (obj != null) obj.Highlight(false);

        HandInteraction.ReticleOverride = null;

        SetText(
            "¡Correcto! Cuando el punto es verde,\n" +
            "significa que puedes interactuar con ese objeto.\n\n" +
            "<size=70%>Presiona start para continuar.</size>");

        yield return StartCoroutine(WaitForConfirm());
    }

    // ─────────────────────────────────────────────
    // Paso 2 — Seleccionar con Fire1
    // ─────────────────────────────────────────────

    IEnumerator Paso2_SeleccionarObjeto()
    {
        if (objetosMirar == null || objetosMirar.Length == 0) yield break;

        Highlightable objetivo = objetosMirar[0];
        if (objetivo != null) objetivo.Highlight(true);

        SetText(
            "Ahora apunta al objeto iluminado\n" +
            "y <b>presiona el botón start</b>\n" +
            "cuando el punto esté verde para seleccionarlo.");

        bool seleccionado = false;
        while (!seleccionado)
        {
            bool apuntando = EstaApuntandoA(objetivo);
            HandInteraction.ReticleOverride = apuntando ? Color.green : (Color?)null;

            if (apuntando && TouchInput.ButtonDown("Fire1", ref _lastTouchPressId))
                seleccionado = true;

            yield return null;
        }

        HandInteraction.ReticleOverride = null;
        if (objetivo != null) objetivo.Highlight(false);

        SetText(
            "¡Perfecto!\n" +
            "<b>Apuntar + Presionar start = Seleccionar</b>\n\n" +
            "Así interactuarás con los objetos del laboratorio.\n\n" +
            "<size=70%>Presiona start para continuar.</size>");

        yield return StartCoroutine(WaitForConfirm());
    }

    // ─────────────────────────────────────────────
    // Paso 3 — Botones de respuesta
    // ─────────────────────────────────────────────

    IEnumerator Paso3_BotonesRespuesta()
    {
        SetText(
            "Durante las prácticas verás preguntas con botones.\n" +
            "<b>Mira hacia arriba</b> para ver los botones,\n" +
            "luego <b>apunta al botón</b> que deseas → se pondrá <color=yellow>amarillo</color>.\n" +
            "Presiona <b>start</b> para confirmar.\n\n" +
            "<size=70%>Elige cualquiera — esto es solo práctica.</size>");

        string[] opciones = { "Opción A", "Opción B", "Opción C" };
        string respuesta = null;
        yield return StartCoroutine(WaitForButtonSelection(opciones, r => respuesta = r));

        SetText(
            $"Elegiste: <b>{respuesta}</b>\n\n" +
            "¡Así funcionan todos los botones del laboratorio!\n\n" +
            "<size=70%>Presiona start para continuar.</size>");

        yield return StartCoroutine(WaitForConfirm());
    }

    // ─────────────────────────────────────────────
    // Paso 4 — Desplazamiento (opcional)
    // ─────────────────────────────────────────────

    IEnumerator Paso4_Desplazamiento_Internal()
    {
        if (puntoMeta == null) yield break;

        SetText(
            "Por último, practica moverte dentro del laboratorio.\n\n" +
            "Dirígete hacia el <b>objeto iluminado en amarillo</b>.\n\n" +
            "<size=70%>Observa alrededor de tu entorno para encontrarlo.</size>");

        if (destinoHighlight != null) destinoHighlight.Highlight(true);

        yield return new WaitUntil(() =>
            playerTransform != null &&
            Vector3.Distance(playerTransform.position, puntoMeta.position) <= radioMeta);

        if (destinoHighlight != null) destinoHighlight.Highlight(false);

        SetText(
            "¡Llegaste!\n\n" +
            "Así navegarás entre las estaciones del laboratorio.\n\n" +
            "<size=70%>Presiona el control para continuar.</size>");

        yield return StartCoroutine(WaitForConfirm());
    }

    // ─────────────────────────────────────────────
    // Paso 5 — Cierre
    // ─────────────────────────────────────────────

    // ─────────────────────────────────────────────
    // Paso 4 — Conectar cables
    // ─────────────────────────────────────────────

    IEnumerator Paso4_ConectarCable()
    {
        if (cable1Highlight != null) cable1Highlight.Highlight(true);
        if (cable2Highlight != null) cable2Highlight.Highlight(true);

        SetText(
            "Conecta los cables entre dispositivos del laboratorio.\n" +
            "<b>Apunta al puerto:</b> el punto se volverá <b>verde</b>.\n" +
            "Apunta al destino: el punto se volverá <b>rojo</b>.\n\n" +
            "<size=70%>Presiona start para conectar y elige una opción.</size>\n" +
            "<size=70%>Conecta los patchcords iluminados.</size>");

        // Fallback: contar via OnRelease (DropAndPlace path)
        int conexiones = 0;
        System.Action<GrabbableID> onRelease = _ => conexiones++;
        HandInteraction.OnRelease += onRelease;

        // Esperar hasta que al menos 1 CableEnd esté conectado,
        // o OnRelease haya disparado al menos 1 vez,
        // o el LinkStateManager reporte link establecido.
        while (!AlgunCableConectado())
        {
            if (conexiones >= 1) break;
            if (LinkStateManager.Instance != null &&
                LinkStateManager.Instance.CurrentScenario != LinkScenario.Disconnected)
                break;
            yield return null;
        }

        HandInteraction.OnRelease -= onRelease;

        if (cable1Highlight != null) cable1Highlight.Highlight(false);
        if (cable2Highlight != null) cable2Highlight.Highlight(false);

        SetText(
            "¡Bien hecho!\n\n" +
            "Así conectas cables entre dispositivos en el laboratorio.\n\n" +
            "<size=70%>Presiona el control para continuar.</size>");

        yield return StartCoroutine(WaitForConfirm());
    }

    bool AlgunCableConectado()
    {
        if (cableEnd1 != null && cableEnd1.connectedSocket != null) return true;
        if (cableEnd2 != null && cableEnd2.connectedSocket != null) return true;
        return false;
    }

    // ─────────────────────────────────────────────
    // Paso 5 — Desplazamiento (último paso práctico)
    // ─────────────────────────────────────────────

    IEnumerator Paso5_Desplazamiento() { yield return StartCoroutine(Paso4_Desplazamiento_Internal()); }

    // ─────────────────────────────────────────────
    // Paso 6 — Cierre
    // ─────────────────────────────────────────────

    IEnumerator Paso6_Cierre()
    {
        SetText(
            "¡Listo! Ya dominas las mecánicas del laboratorio:\n\n" +
            "  Punto verde → puedes interactuar\n" +
            "  Control (Fire1) → seleccionar / confirmar\n" +
            "  Botón amarillo → estás apuntando a él\n" +
            "  Caminar → acércate a los puntos marcados\n" +
            "  Agarre (Submit) → tomar / soltar cables\n\n" +
            "<size=70%>Presiona el control para comenzar tu práctica.</size>");

        yield return StartCoroutine(WaitForConfirm());
    }

    // ══════════════════════════════════════════════════════
    // UTILIDADES
    // ══════════════════════════════════════════════════════

    void TeletransportarJugador()
    {
        if (puntoInicioDemo == null) return;

        float yRot = puntoInicioDemo.eulerAngles.y;

        // Mueve y rota el cuerpo físico
        if (cuerpoJugador != null)
        {
            var cc = cuerpoJugador.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            cuerpoJugador.position = puntoInicioDemo.position;
            cuerpoJugador.rotation = Quaternion.Euler(0f, yRot, 0f);
            if (cc != null) cc.enabled = true;
        }

        // Rota el rig de cámara para que el visor arranque mirando en esa dirección
        if (camaraRig != null)
        {
            camaraRig.position = puntoInicioDemo.position;
            camaraRig.rotation = Quaternion.Euler(0f, yRot, 0f);
        }
    }

    void SetText(string msg)
    {
        if (instructionText != null) instructionText.text = msg;
    }

    bool EstaApuntandoA(Highlightable obj)
    {
        if (obj == null) return false;
        Camera cam = Camera.main;
        if (cam == null) return false;
        if (Physics.Raycast(cam.transform.position, cam.transform.forward,
                            out RaycastHit hit, 15f))
        {
            return hit.collider.GetComponentInParent<Highlightable>() == obj;
        }
        return false;
    }

    IEnumerator WaitForConfirm()
    {
        yield return null;
        yield return new WaitUntil(() =>
            TouchInput.ButtonDown("Fire1", ref _lastTouchPressId));
    }

    IEnumerator WaitForButtonSelection(string[] opciones, System.Action<string> onSelected)
    {
        if (panelRespuestas == null || botones == null || botones.Length < opciones.Length)
        {
            Debug.LogWarning("[Demo] panelRespuestas o botones no asignados.");
            yield break;
        }

        for (int i = 0; i < botones.Length; i++)
        {
            var label = botones[i].GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = i < opciones.Length ? opciones[i] : "";
            botones[i].gameObject.SetActive(i < opciones.Length);
            botones[i].GetComponent<Image>().color = BTN_NORMAL;
        }

        PosicionarPanelFrenteAlJugador();
        panelRespuestas.SetActive(true);

        int hoveredIdx = -1;
        while (true)
        {
            Camera cam = Camera.main;
            int newHovered = -1;
            if (cam != null)
                for (int i = 0; i < opciones.Length; i++)
                    if (IsGazeOnButton(botones[i].GetComponent<RectTransform>(), cam))
                    { newHovered = i; break; }

            if (newHovered != hoveredIdx)
            {
                if (hoveredIdx >= 0)
                    botones[hoveredIdx].GetComponent<Image>().color = BTN_NORMAL;
                if (newHovered >= 0)
                    botones[newHovered].GetComponent<Image>().color = BTN_HOVERED;
                hoveredIdx = newHovered;
            }

            if (hoveredIdx >= 0 &&
                TouchInput.ButtonDown("Fire1", ref _lastTouchPressId))
            {
                botones[hoveredIdx].GetComponent<Image>().color = BTN_NORMAL;
                panelRespuestas.SetActive(false);
                onSelected(opciones[hoveredIdx]);
                yield return null;
                yield break;
            }

            yield return null;
        }
    }

    void PosicionarPanelFrenteAlJugador()
    {
        if (panelRespuestas == null) return;
        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 forward = cam.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        forward.Normalize();
        panelRespuestas.transform.position =
            cam.transform.position + forward * 1.5f + Vector3.up * 0.7f;
        panelRespuestas.transform.LookAt(cam.transform.position);
        panelRespuestas.transform.Rotate(0f, 180f, 0f);
    }

    static bool IsGazeOnButton(RectTransform rt, Camera cam)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        foreach (var c in corners)
        {
            Vector3 s = cam.WorldToScreenPoint(c);
            if (s.z < 0) return false;
            if (s.x < minX) minX = s.x;
            if (s.x > maxX) maxX = s.x;
            if (s.y < minY) minY = s.y;
            if (s.y > maxY) maxY = s.y;
        }
#if UNITY_EDITOR
        Vector2 gaze = Input.mousePosition;
#else
        Vector2 gaze = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
#endif
        return gaze.x >= minX && gaze.x <= maxX && gaze.y >= minY && gaze.y <= maxY;
    }
}
