using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class OpticalAttenuatorController : MonoBehaviour, IFocusable
{
    [Header("UI")]
    public GameObject attenuatorUI;
    public TextMeshProUGUI displayText;
    public GameObject firstSelectedButton;

    [Header("Values")]
    public float currentPower = 0f;
    public float powerStep = 5f;
    public float minPower = -60f;
    public float maxPower = 0f;

    int wavelengthIndex = 0;
    int[] wavelengths = new int[] { 1250, 1310, 1550 };

    bool isOn = false;

    public static event System.Action AtenuadorStarted;
    public static event System.Action AtenuadorCompleted;
    public static event System.Action OnAttenuatorPoweredOn;
    public static event System.Action OnAttenuatorLambda;
    public static event System.Action OnAttenuatorPower;


    enum Mode
    {
        None,
        Wavelength,
        Power
    }
    public int CurrentWavelength
    {
        get { return wavelengths[wavelengthIndex]; }
    }

    public float CurrentPower
    {
        get { return currentPower; }
    }

    public bool IsOn
    {
        get { return isOn; }
    }
    Mode currentMode = Mode.None;

    // ─────────────────────────────
    // Focus Entry
    // ─────────────────────────────
    public void OpenFocus()
    {
        FocusModeManager.Instance.Enter(attenuatorUI);
        SelectFirstButton();
        UpdateDisplay();
        AtenuadorStarted?.Invoke();
    }

    // ─────────────────────────────
    // BOTÓN A – POWER
    // ─────────────────────────────
    public void ButtonA_Power()
    {
        isOn = !isOn;

        if (!isOn)
        {
            currentMode = Mode.None;
        }
        else
        {
            OnAttenuatorPoweredOn?.Invoke();
        }

            UpdateDisplay();
    }

    // ─────────────────────────────
    // BOTÓN B – RESET
    // ─────────────────────────────
    public void ButtonB_Reset()
    {
        if (!isOn) return;

        currentPower = 0f;
        UpdateDisplay();
    }

    // ─────────────────────────────
    // BOTÓN C – WAVELENGTH MODE
    // ─────────────────────────────
    public void ButtonC_Wavelength()
    {
        if (!isOn) return;

        currentMode = Mode.Wavelength;
        UpdateDisplay();
    }

    // ─────────────────────────────
    // BOTÓN F – POWER MODE
    // ─────────────────────────────
    public void ButtonF_PowerMode()
    {
        if (!isOn) return;

        currentMode = Mode.Power;
        UpdateDisplay();
    }

    // ─────────────────────────────
    // BOTÓN D – UP
    // ─────────────────────────────
    public void ButtonD_Up()
    {
        if (!isOn) return;

        if (currentMode == Mode.Wavelength)
        {
            wavelengthIndex++;
            if (wavelengthIndex >= wavelengths.Length)
                wavelengthIndex = wavelengths.Length - 1;
        }
        else if (currentMode == Mode.Power)
        {
            currentPower += powerStep;
            currentPower = Mathf.Clamp(currentPower, minPower, maxPower);
        }

        UpdateDisplay();
    }

    // ─────────────────────────────
    // BOTÓN G – DOWN
    // ─────────────────────────────
    public void ButtonG_Down()
    {
        if (!isOn) return;

        if (currentMode == Mode.Wavelength)
        {
            wavelengthIndex--;
            if (wavelengthIndex < 0)
                wavelengthIndex = 0;
        }
        else if (currentMode == Mode.Power)
        {
            currentPower -= powerStep;
            currentPower = Mathf.Clamp(currentPower, minPower, maxPower);
        }

        UpdateDisplay();
    }

    // ─────────────────────────────
    // BOTÓN E – UNIDAD
    // ─────────────────────────────
    bool showDbm = false;

    public void ButtonE_ToggleUnit()
    {
        if (!isOn) return;

        showDbm = !showDbm;
        UpdateDisplay();
    }

    // ─────────────────────────────
    // DISPLAY
    // ─────────────────────────────
    void UpdateDisplay()
    {
        if (!isOn)
        {
            displayText.text = "OFF";
            return;
        }

        switch (currentMode)
        {
            case Mode.Wavelength:
                displayText.text = wavelengths[wavelengthIndex] + " nm";
                break;

            case Mode.Power:
                if (showDbm)
                    displayText.text = "Power\n" + currentPower + " dBm";
                else
                    displayText.text = "Power\n" + currentPower + " dB";
                break;

            default:
                displayText.text = "ON\n" + wavelengths[wavelengthIndex] + " nm";
                break;
        }
    }

    // ─────────────────────────────
    // EXIT
    // ─────────────────────────────
    public void ExitInstrument()
    {
        AtenuadorCompleted?.Invoke();
        FocusModeManager.Instance.Exit();
        
    }

    void SelectFirstButton()
    {
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstSelectedButton);
    }
}