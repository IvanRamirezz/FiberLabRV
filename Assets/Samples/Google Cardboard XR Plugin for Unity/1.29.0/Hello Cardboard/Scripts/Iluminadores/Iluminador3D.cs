using UnityEngine;
using System.Collections;

public class Highlightable : MonoBehaviour
{
    public Renderer rend;
    public Color highlightColor = Color.yellow;
    public Color grayColor      = new Color(0.25f, 0.25f, 0.25f);
    public float maxEmission = 6f;
    public float pulseSpeed = 1f;

    Color originalColor;
    Color originalEmission;

    Coroutine pulseRoutine;

    void Awake()
    {
        if (rend == null)
            rend = GetComponentInChildren<Renderer>();

        originalColor = rend.material.color;

        if (rend.material.HasProperty("_EmissionColor"))
            originalEmission = rend.material.GetColor("_EmissionColor");
    }

    public void Highlight(bool value)
    {
        if (value)
        {
            rend.material.color = highlightColor;

            if (pulseRoutine == null)
                pulseRoutine = StartCoroutine(PulseEmission());
        }
        else
        {
            if (pulseRoutine != null)
            {
                StopCoroutine(pulseRoutine);
                pulseRoutine = null;
            }

            rend.material.color = originalColor;

            if (rend.material.HasProperty("_EmissionColor"))
                rend.material.SetColor("_EmissionColor", originalEmission);
        }
    }

    public void GrayOut(bool value)
    {
        if (value)
        {
            if (rend.material.HasProperty("_EmissionColor"))
                rend.material.SetColor("_EmissionColor", Color.black);
            rend.material.color = grayColor;
        }
        else
        {
            if (rend.material.HasProperty("_EmissionColor"))
                rend.material.SetColor("_EmissionColor", originalEmission);
            rend.material.color = originalColor;
        }
    }

    IEnumerator PulseEmission()
    {
        rend.material.EnableKeyword("_EMISSION");

        float t = 0f;

        while (true)
        {
            t += Time.deltaTime * pulseSpeed;
            float wave      = (Mathf.Sin(t) + 1f) / 2f;          // 0..1
            float intensity = Mathf.Lerp(0.5f, maxEmission, wave);
            float colorLerp = Mathf.Lerp(0.2f, 1f, wave);

            rend.material.SetColor("_EmissionColor", highlightColor * intensity);
            // Pulse the albedo too so the flash is visible even without bloom/HDR
            rend.material.color = Color.Lerp(originalColor, highlightColor * 1.5f, colorLerp);
            yield return null;
        }
    }
}

