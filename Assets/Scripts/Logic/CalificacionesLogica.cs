using UnityEngine;

// Lógica de dominio de CalificacionesLoader: mensajes de error según el código
// HTTP y, a partir del cuerpo de resultados, la calificación de cada práctica y
// el promedio. No sabe de UI (eso es de CalificacionesLoader) ni de HTTP (eso es
// de CalificacionesRepository). code == 0 significa falla de red.
// Los números usan la cultura actual (ToString("0.0")), como antes.
public class CalificacionesLogica
{
    // Texto de una calificación que aún no existe.
    public const string Pendiente = "-";

    public class Resumen
    {
        public string practica1 = Pendiente;
        public string practica2 = Pendiente;
        public string practica3 = Pendiente;
        public string promedio = Pendiente;
    }

    [System.Serializable] private class Resultado { public int practica_id; public float calificacion; }
    [System.Serializable] private class ResultadosWrapper { public Resultado[] items; }

    public string MensajeError(long code)
    {
        if (code == 0) return "Error de red. Intenta de nuevo.";
        if (code == 401 || code == 403) return "Sin permisos para ver calificaciones (revisa RLS/policies).";
        return "Error al consultar calificaciones.";
    }

    // Si hay varias filas de una misma práctica, la última es la que se muestra;
    // todas cuentan para el promedio.
    public Resumen Interpretar(string body)
    {
        string wrapped = "{\"items\":" + body + "}";
        ResultadosWrapper data = JsonUtility.FromJson<ResultadosWrapper>(wrapped);

        var resumen = new Resumen();
        float suma = 0f;
        int count = 0;

        if (data?.items != null)
        {
            foreach (var r in data.items)
            {
                switch (r.practica_id)
                {
                    case 1: resumen.practica1 = r.calificacion.ToString("0.0"); break;
                    case 2: resumen.practica2 = r.calificacion.ToString("0.0"); break;
                    case 3: resumen.practica3 = r.calificacion.ToString("0.0"); break;
                }

                suma += r.calificacion;
                count++;
            }
        }

        resumen.promedio = count > 0 ? (suma / count).ToString("0.0") : Pendiente;
        return resumen;
    }
}
