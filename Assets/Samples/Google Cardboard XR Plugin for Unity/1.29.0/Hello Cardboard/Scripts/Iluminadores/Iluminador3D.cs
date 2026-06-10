using UnityEngine;
using System.Collections;

public class Highlightable : MonoBehaviour
{
    public Renderer rend;
    public Color highlightColor = Color.yellow;
    public float maxEmission = 2f;
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

    IEnumerator PulseEmission()
    {
        rend.material.EnableKeyword("_EMISSION");

        float t = 0f;

        while (true)
        {
            t += Time.deltaTime * pulseSpeed;
            float intensity = Mathf.Lerp(0.5f, maxEmission, (Mathf.Sin(t) + 1f) / 2f);

            rend.material.SetColor("_EmissionColor", highlightColor * intensity);
            yield return null;
        }
    }
}

