/// <summary>
/// Utilidad para parsear arrays JSON con JsonUtility de Unity,
/// ya que JsonUtility no soporta arrays en la raíz directamente.
/// Uso: JsonHelper.FromJson<MiClase>(jsonString)
/// </summary>
public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        string wrapped = "{ \"Items\": " + json + "}";
        var wrapper = UnityEngine.JsonUtility.FromJson<Wrapper<T>>(wrapped);
        return wrapper?.Items;
    }

    [System.Serializable]
    private class Wrapper<T> { public T[] Items; }
}
