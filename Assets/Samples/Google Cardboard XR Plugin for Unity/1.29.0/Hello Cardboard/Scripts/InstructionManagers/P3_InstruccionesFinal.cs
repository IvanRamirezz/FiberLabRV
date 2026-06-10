using UnityEngine;
using TMPro;
using System.Collections;
public class InstructionManagerP3 : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;                 // tu cámara o root del jugador
    public TextMeshProUGUI instructionText;

    [Header("Objetivo")]
    public Transform targetPoint;           // lugar al que debe llegar
    public float triggerRadius = 1.5f;      // metros

    [Header("Objetivo 2")]
    public Transform targetPoint2;           // lugar al que debe llegar
    public float triggerRadius2 = 1.5f;      // metros
 
    private int step = 0;

    void Start()
    {
        instructionText.text = "Instrucciones: Dirígete hacia la mesa 1";
    }

    void Update()
    {
        if (step == 0)
        {
            float dist = Vector3.Distance(player.position, targetPoint.position);

            if (dist <= triggerRadius)
            {
                StartCoroutine(NextStep());
            }
        }

        if (step == 1)
        {
            float dist = Vector3.Distance(player.position, targetPoint2.position);

            if (dist <= triggerRadius2)
            {
                StartCoroutine(NextStep());
            }
        }
    }

    IEnumerator NextStep()
    {
        step++;

        // ──────────────── INICIO / ORIENTACIÓN ────────────────
        if (step == 1)
        {
            instructionText.text =
                "Bienvenido a la práctica de medición de BER en enlaces ópticos.";
            yield return new WaitForSeconds(4f);

            instructionText.text =
                "Acércate a la mesa número tres, correspondiente a esta práctica.";
        }

        // ──────────────── FASE 1: EXPLORACIÓN ────────────────
        else if (step == 2)
        {
            instructionText.text =
                "Observa los equipos sobre la mesa: transmisor, receptor, atenuador y medidor BER.";
            yield return new WaitForSeconds(4f);

            instructionText.text =
                "Acerca tu mano a cada equipo para conocer su función en la prueba.";
        }

        // ──────────────── FASE 2: CONEXIÓN ────────────────
        else if (step == 3)
        {
            instructionText.text =
                "Conecta el transmisor óptico al atenuador y luego al receptor usando patchcords monomodo.";
        }

        // ──────────────── FASE 3: CONFIGURACIÓN TX ────────────────
        else if (step == 4)
        {
            instructionText.text =
                "Configura el transmisor: tasa de bits 2.5 Gb/s, patrón PRBS7 y potencia de –3 dBm.";
            yield return new WaitForSeconds(4f);

            instructionText.text =
                "Cuando termines, habilita la transmisión seleccionando Enable TX.";
        }

        // ──────────────── FASE 4: CONFIGURACIÓN RX ────────────────
        else if (step == 5)
        {
            instructionText.text =
                "Verifica en el receptor la potencia recibida y el estado de sincronización.";
            yield return new WaitForSeconds(3f);

            instructionText.text =
                "Asegúrate de que el receptor indique: Sync OK.";
        }

        // ──────────────── FASE 5: MEDICIÓN BER ────────────────
        else if (step == 6)
        {
            instructionText.text =
                "Configura el BER Tester con PRBS7, 2.5 Gb/s y un tiempo de medición de 10 segundos.";
            yield return new WaitForSeconds(4f);

            instructionText.text =
                "Presiona Start BER Test y observa los resultados.";
        }

        // ──────────────── FASE 6: DEGRADACIÓN ────────────────
        else if (step == 7)
        {
            instructionText.text =
                "Aumenta la atenuación del enlace hasta 15 dB y repite la medición.";
            yield return new WaitForSeconds(4f);

            instructionText.text =
                "Observa cómo el BER aumenta y puede perderse la sincronización.";
        }

        // ──────────────── FASE 7: CONCLUSIÓN ────────────────
        else if (step == 8)
        {
            instructionText.text =
                "Registra una reflexión sobre el efecto de la atenuación en el BER y la sincronización.";
            yield return new WaitForSeconds(4f);

            instructionText.text =
                "Cuando termines, envía tu conclusión.";
        }

        // ──────────────── SALIDA DEL ENTORNO ────────────────
        else if (step == 9)
        {
            instructionText.text =
                "Para finalizar la práctica, dirígete al computador y selecciona Salir.";
        }
    }

}
