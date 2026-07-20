using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Se coloca en el contenedor de un grupo de botones de opción única
/// (2, 3 o N botones). Cada botón llama a Seleccionar(indice) desde su
/// OnClick, configurado en el Inspector con el índice 0-based del botón
/// dentro de `botones`.
/// </summary>
public class ButtonChoiceSelector : MonoBehaviour
{
    public Button[] botones;
    public Color colorSeleccionado = new Color(0.2f, 0.6f, 0.9f, 1f);

    public int IndiceSeleccionado { get; private set; } = -1;
    public string TextoSeleccionado { get; private set; } = "";

    public void Seleccionar(int indice)
    {
        if (botones == null || indice < 0 || indice >= botones.Length) return;

        IndiceSeleccionado = indice;
        TextoSeleccionado = botones[indice].GetComponentInChildren<TMP_Text>().text;

        for (int i = 0; i < botones.Length; i++)
        {
            var image = botones[i].GetComponent<Image>();
            if (image != null)
                image.color = (i == indice) ? colorSeleccionado : Color.white;
        }
    }
}
