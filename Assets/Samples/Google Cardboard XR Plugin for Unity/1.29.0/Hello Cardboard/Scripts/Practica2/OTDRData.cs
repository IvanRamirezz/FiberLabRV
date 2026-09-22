using System;
using System.Collections.Generic;
using UnityEngine;

// ════════════════════════════════════════════════════════════════
// Clases de datos para deserializar los JSON generados por MATLAB.
// Equivalente conceptual a LinkScenarioData del BER Tester.
//
// IMPORTANTE: JsonUtility ignora en silencio los campos del JSON que
// no existan aquí. Si el script de MATLAB cambia y estas clases no,
// los datos nuevos simplemente no llegan y no hay error que lo avise.
// ════════════════════════════════════════════════════════════════

[Serializable]
public class OTDRScenarioData
{
    public string escenario;
    public string color;
    public string descripcion;
    public float z_total;
    public float z_fin;
    public float bobina_lanzamiento_km;   // metadato: no entra en el modelo
    public float bobina_recepcion_km;     // metadato: no entra en el modelo

    public List<OTDREventData> eventos;

    // Rango en km que abarca cada imagen generada. Es lo que permite
    // colocar un marcador en cualquier nivel de zoom a partir de la
    // distancia del evento, sin guardar coordenadas por imagen.
    public List<OTDRVista> vistas;

    // Geometría del área de datos dentro del PNG (0..1). Los ejes y
    // etiquetas ocupan márgenes, así que la zona graficada no cubre
    // el 100% de la imagen.
    public float plot_left;
    public float plot_right;

    public OTDRParametros parametros;
}

[Serializable]
public class OTDREventData
{
    public string tipo;                   // "no_reflectivo", "reflectivo", "rotura"
    public float distancia_km;

    // -1 marca pérdida total no medible (rotura). Un OTDR real tampoco
    // reporta cifra ahí: no queda señal con la cual medirla.
    public float perdida_dB_1310;
    public float perdida_dB_1550;

    // Reflectancia real, siempre NEGATIVA. 0 significa "no aplica"
    // (evento no reflectivo).
    public float reflectancia_dB;

    // Amplitud del pico de Fresnel usada por MATLAB para graficar.
    // Valor positivo, sin significado físico para el alumno.
    public float amplitud_pico_dB;

    public string descripcion;

    // Normalizada sobre z_total: solo es válida en la vista completa.
    // Para las vistas con zoom se usa distancia_km contra OTDRVista.
    public float posicion_normalizada_x;
}

[Serializable]
public class OTDRVista
{
    public string id;          // "complete", "zoom_left", "zoom_right"
    public float inicio_km;
    public float fin_km;
}

[Serializable]
public class OTDRParametros
{
    public float alpha_1310_dBkm;
    public float alpha_1550_dBkm;
    public float P0_dB;
    public float sigma_evento_km;
    public float sigma_ruido_base_dB;
    public float piso_ruido_dB;
}