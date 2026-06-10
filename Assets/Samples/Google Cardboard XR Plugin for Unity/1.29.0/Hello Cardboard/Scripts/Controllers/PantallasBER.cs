using UnityEngine;

public class InstrumentUIScreenManager : MonoBehaviour
{
    [Header("Screens")]
    public GameObject screenHome;
    public GameObject screenMargin;
    public GameObject screenConfig;
    public GameObject screenBERTest;
    public GameObject screenInfo;
    public GameObject screenOffPanel;   // panel negro

    GameObject currentScreen;

    bool isOn = false;

    void Start()
    {
        
    }

    public void ShowHome()
    {
        ShowScreen(screenHome);
    }

    public void ShowMargin()
    {
        ShowScreen(screenMargin);
    }

    public void ShowConfig()
    {
        ShowScreen(screenConfig);
    }

    public void ShowBERTest()
    {
        ShowScreen(screenBERTest);
    }

    public void ShowInfo()
    {
        ShowScreen(screenInfo);
    }
    public void TurnOnOffBERTester()
    {
        isOn = !isOn;

        if (isOn)
        {
            screenOffPanel.SetActive(false);
            ShowScreen(screenHome);
        }
        else
        {
            screenOffPanel.SetActive(true);
        }
        
    }
    void ShowScreen(GameObject newScreen)
    {
        if (currentScreen != null)
            currentScreen.SetActive(false);
        
        currentScreen = newScreen;
        currentScreen.SetActive(true);
    }
}