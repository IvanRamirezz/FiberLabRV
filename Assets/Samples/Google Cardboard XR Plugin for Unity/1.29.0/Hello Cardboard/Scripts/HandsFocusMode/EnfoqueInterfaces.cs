using UnityEngine;

public class FocusModeManager : MonoBehaviour
{
    public static FocusModeManager Instance;

    [Header("Referencias")]
    public GameObject backgroundDimmer;
    public Transform focusRoot;
    public bool IsInFocusMode => inFocusMode;
    GameObject currentUI;
    bool inFocusMode;

    [SerializeField] HandInteraction handInteraction;
    [SerializeField] MotionObjectController motionObjectController;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (backgroundDimmer != null)
            backgroundDimmer.SetActive(false);

        if (focusRoot != null)
            focusRoot.gameObject.SetActive(false);
    }
    //public void Enter(GameObject uiToShow)
    //{
    //    if (inFocusMode) return;
    //    inFocusMode = true;

    //    Transform cam = Camera.main.transform;

    //    // 1️⃣ Posición frente al jugador (distancia REAL)
    //    Vector3 forwardFlat = new Vector3(cam.forward.x, 0f, cam.forward.z).normalized;
    //    focusRoot.position = cam.position + forwardFlat * 0.75f;

    //    // 2️⃣ Rotación SOLO en Y (yaw)
    //    focusRoot.rotation = Quaternion.LookRotation(forwardFlat, Vector3.up);

    //    // 3️⃣ Activar visuales
    //    if (backgroundDimmer != null)
    //        backgroundDimmer.SetActive(true);

    //    focusRoot.gameObject.SetActive(true);

    //    currentUI = uiToShow;
    //    currentUI.SetActive(true);

    //    DisableWorldInteraction();

    //    Debug.Log("Focus Mode ENTER completado");
    //}
    public void Enter(GameObject uiToShow)
    {
        if (inFocusMode) return;

        inFocusMode = true;

        Transform cam = Camera.main.transform;

        Vector3 forwardFlat = new Vector3(cam.forward.x, 0f, cam.forward.z).normalized;
        focusRoot.position = cam.position + forwardFlat * 0.75f;
        focusRoot.rotation = Quaternion.LookRotation(forwardFlat, Vector3.up);

        if (backgroundDimmer != null)
            backgroundDimmer.SetActive(true);

        focusRoot.gameObject.SetActive(true);

        // 🔹 DESACTIVAR UI PREVIA
        if (currentUI != null)
            currentUI.SetActive(false);

        // 🔹 ACTIVAR SOLO LA NUEVA
        currentUI = uiToShow;
        currentUI.SetActive(true);

        DisableWorldInteraction();

        Debug.Log("Focus Mode ENTER completado");
    }

    public void Exit()
    {
        if (!inFocusMode) return;

        if (currentUI != null)
            currentUI.SetActive(false);

        focusRoot.gameObject.SetActive(false);

        if (backgroundDimmer != null)
            backgroundDimmer.SetActive(false);

        currentUI = null;
        inFocusMode = false;

        EnableWorldInteraction();
    }

    void DisableWorldInteraction()
    {
        // Ejemplo:
        // PlayerController.enabled = false;
        Debug.Log("Deshabilitando HandInteraction");
        handInteraction.enabled = false;
        motionObjectController.enabled = false;
    }

    void EnableWorldInteraction()
    {
        // Reactiva scripts deshabilitados
        handInteraction.enabled = true;
        motionObjectController.enabled = true; 
    }
}
