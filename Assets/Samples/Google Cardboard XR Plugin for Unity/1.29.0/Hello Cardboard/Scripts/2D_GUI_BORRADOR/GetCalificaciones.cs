using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections.Generic;

public class CalificacionesLoader : MonoBehaviour
{
    [Header("Supabase")]
    public string supabaseUrl = "https://cxmsimqrtkqsrxzasptq.supabase.co";
    public string anonKey = "sb_secret_oEZqSZojugA3JuHaH5dnww_ODmb_u0P";

    [Header("Alumno")]
    public int alumnoId = 1;

    [Header("TextMeshPros")]
    public TextMeshProUGUI practica1Text;
    public TextMeshProUGUI practica2Text;
    public TextMeshProUGUI practica3Text;

    void Start()
    {
        StartCoroutine(CargarCalificaciones());
    }

    IEnumerator CargarCalificaciones()
    {
        string url =
            $"{supabaseUrl}/rest/v1/resultados" +
            $"?alumno_id=eq.{alumnoId}" +
            $"&practica_id=in.(1,2,3)" +
            $"&select=practica_id,calificacion";

        UnityWebRequest request = UnityWebRequest.Get(url);

        request.SetRequestHeader("apikey", anonKey);
        request.SetRequestHeader("Authorization", "Bearer " + anonKey);
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(request.error);
            yield break;
        }

        string json = request.downloadHandler.text;

        // JsonUtility no soporta arrays directamente
        string wrapped = "{\"items\":" + json + "}";
        ResultadosWrapper data = JsonUtility.FromJson<ResultadosWrapper>(wrapped);

        foreach (Resultado r in data.items)
        {
            switch (r.practica_id)
            {
                case 1:
                    practica1Text.text = r.calificacion.ToString("0.0");
                    break;

                case 2:
                    practica2Text.text = r.calificacion.ToString("0.0");
                    break;

                case 3:
                    practica3Text.text = r.calificacion.ToString("0.0");
                    break;
            }
        }
    }
}

[System.Serializable]
public class Resultado
{
    public int practica_id;
    public float calificacion;
}

[System.Serializable]
public class ResultadosWrapper
{
    public Resultado[] items;
}

