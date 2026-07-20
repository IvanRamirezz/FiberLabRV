using UnityEngine;

/// <summary>
/// ScriptableObject que guarda las credenciales de Supabase.
/// Crear en: Assets > Create > Config > SupabaseConfig
/// NUNCA subir el .asset con la key real a un repositorio público.
/// </summary>
[CreateAssetMenu(fileName = "SupabaseConfig", menuName = "Config/SupabaseConfig")]
public class SupabaseConfig : ScriptableObject
{
    [Header("Supabase Credentials")]
    public string url = "";

    [TextArea]
    public string anonKey = "";
}
