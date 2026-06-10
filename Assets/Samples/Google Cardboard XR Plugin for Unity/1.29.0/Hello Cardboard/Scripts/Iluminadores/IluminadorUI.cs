using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIHighlightable : MonoBehaviour
{
    Graphic graphic;
    Button button;

    public Color highlightColor = Color.yellow;
    public float pulseSpeed = 10f;
    public float maxAlpha = 1f;
    public float minAlpha = 0.05f;

    Color originalColor;
    ColorBlock originalColorBlock;

    Coroutine pulseRoutine;

    void Awake()
    {
        graphic = GetComponent<Graphic>();

        if (graphic == null)
        {
            Debug.LogError("UIHighlightable requiere un componente Graphic.");
            return;
        }

        originalColor = graphic.color;

        button = GetComponent<Button>();

        if (button != null)
            originalColorBlock = button.colors;
    }

    public void Highlight(bool value)
    {
        if (value)
        {
            if (button != null)
            {
                ColorBlock colors = button.colors;

                colors.normalColor = highlightColor;
                colors.highlightedColor = highlightColor;
                colors.selectedColor = highlightColor;

                button.colors = colors;
            }

            if (pulseRoutine == null)
                pulseRoutine = StartCoroutine(Pulse());
        }
        else
        {
            if (pulseRoutine != null)
            {
                StopCoroutine(pulseRoutine);
                pulseRoutine = null;
            }

            if (button != null)
                button.colors = originalColorBlock;

            graphic.color = originalColor;
        }
    }

    IEnumerator Pulse()
    {
        float t = 0f;

        while (true)
        {
            t += Time.deltaTime * pulseSpeed;

            float alpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(t) + 1f) / 2f);

            Color c = highlightColor;
            c.a = alpha;

            if (graphic != null)
                graphic.color = c;

            yield return null;
        }
    }
}