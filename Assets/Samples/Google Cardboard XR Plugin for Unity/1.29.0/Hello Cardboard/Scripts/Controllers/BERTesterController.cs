using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class BERTesterController : MonoBehaviour, IFocusable
{
    [Header("UI")]
    public GameObject berUI;   // Canvas World Space
    public static event System.Action OnBERCompleted;
    public static event System.Action OnQuickTestFinished;   // NUEVO
    public static event System.Action OnBERTestFinished;     // NUEVO
    [Header("Simulation")]
    public float measurementTime = 10f;
    [Header("Navegation")]
    public GameObject firstSelectedButton;
    //public GameObject BotonDown;
    bool isMeasuring;
    [Header("Speed Selection")]
    public Toggle toggle100Mb;
    public Toggle toggle1Gb;
    public Toggle toggle2_5Gb;
    public Toggle toggle5Gb;
    public Toggle toggle10Gb;

    float selectedBitrate = 1e9f;
    [Header("UI Quick Test")]
    public TMP_Text resultTextQT;
    [Header("UI BER Test")]
    public TMP_Text resultText;
    public TMP_Text packetText;
    public TMP_Text timeText;

    public TMP_Text durationInput;
    int testDuration = 0;
    int maxDuration = 60;
    int minDuration = 0;

    bool testRunning = false;
    // ──────────────── ENTRY POINT ────────────────
    public void OpenBERTester()
    {
        Debug.Log("ABRIENDO BERTESTER");
        FocusModeManager.Instance.Enter(berUI);
        testDuration = 0;
        UpdateDurationDisplay();
        SelectFirstButton();
        //
    }

    // ──────────────── SIMULATED MEASUREMENT ────────────────


    public void StartBERTest()
    {
        StartCoroutine(MeasurementRoutine());
    }

    public void StartQuickTest()
    {
        StartCoroutine(RunQuickTest());
    }
    IEnumerator MeasurementRoutine()
    {
        if (isMeasuring) yield break;

        isMeasuring = true;

        UpdateBitrate();
        if (testDuration == 0)
        {
            Debug.Log("Modo continuo");
        }
        else
        {
            yield return StartCoroutine(RunBERTest(testDuration));
        }

        isMeasuring = false;
    }

    // ──────────────── EXIT ────────────────
    public void ExitBERTester()
    {

        OnBERCompleted?.Invoke();
        FocusModeManager.Instance.Exit();
        //if (isMeasuring) return;
        
        
    }
    void SelectFirstButton()
    {
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstSelectedButton);
    }
    public void OpenFocus()
    {
        OpenBERTester();
    }
    void UpdateBitrate()
    {
        if (toggle100Mb.isOn) selectedBitrate = 1e8f;
        if (toggle1Gb.isOn) selectedBitrate = 1e9f;
        if (toggle2_5Gb.isOn) selectedBitrate = 2.5e9f;
        if (toggle5Gb.isOn) selectedBitrate = 5e9f;
        if (toggle10Gb.isOn) selectedBitrate = 10e9f;
    }
    IEnumerator RunBERTest(float duration)
    {
        testRunning = true;

        resultText.text = "Running...";
        packetText.text = "";
        timeText.text = "";

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            timeText.text = "Elapsed time: " + elapsed.ToString("F1") + " s";

            yield return null;
        }

        GenerateResults(duration);

        testRunning = false;
    }

    IEnumerator RunQuickTest()
    {
        testRunning = true;
        resultTextQT.text = "Running Quick Test...";

        float elapsed = 0f;
        while (elapsed < 2)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        LinkScenarioData data = LinkStateManager.Instance.GetCurrentData();
        resultTextQT.text = data.quickTestMessage;
        OnQuickTestFinished?.Invoke();
        testRunning = false;
    }
    void GenerateResults(float duration)
    {
        LinkScenarioData data = LinkStateManager.Instance.GetCurrentData();

        // Caso 1: no hay link (Disconnected o Critical)
        if (!data.linkEstablished)
        {
            resultText.text = "Result: " + data.statusText;
            packetText.text = "No data transmitted";
            timeText.text += "\nTest aborted";
            OnBERTestFinished?.Invoke();
            return;
        }

        // Caso 2: link establecido, calcular resultados
        float bitsSent = selectedBitrate * duration;
        double ber = data.berNominal;

        // Errores esperados con pequeña dispersión para realismo
        double erroresEsperados = ber * bitsSent;
        int erroresMostrados;

        if (erroresEsperados < 1.0)
        {
            // Régimen de Poisson con lambda pequeño: casi siempre 0, ocasionalmente 1
            erroresMostrados = (UnityEngine.Random.value < erroresEsperados) ? 1 : 0;
        }
        else
        {
            // Régimen con muchos errores: dispersión gaussiana aprox. (+-8%)
            float dispersion = UnityEngine.Random.Range(0.92f, 1.08f);
            erroresMostrados = Mathf.RoundToInt((float)(erroresEsperados * dispersion));
        }

        // BER efectivo mostrado (recalculado a partir de los errores mostrados
        // para mantener coherencia visual entre el conteo de errores y el BER)
        double berMostrado = (bitsSent > 0) ? (erroresMostrados / bitsSent) : 0;

        resultText.text = "Result: " + erroresMostrados + " error(s)\nStatus: " + data.statusText;
        packetText.text = bitsSent.ToString("E2") + " bits | BER " +
                          (erroresMostrados > 0 ? berMostrado.ToString("E2") : "< 1E-12");
        OnBERTestFinished?.Invoke();
        timeText.text += "\nTest completed";
    }
    void UpdateDurationDisplay()
    {
        durationInput.text = testDuration.ToString();
    }
    public void IncreaseDuration()
    {
        if (testDuration < maxDuration)
        {
            testDuration++;
            UpdateDurationDisplay();
        }
    }
    public void DecreaseDuration()
    {
        if (testDuration > minDuration)
        {
            testDuration--;
            UpdateDurationDisplay();
        }
    }
}