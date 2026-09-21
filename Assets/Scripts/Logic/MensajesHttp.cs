// Mensajes de error al usuario para los envíos POST a Supabase. Los repositorios
// solo reportan (ok, código HTTP, cuerpo); quien los consume decide qué mostrar.
// Código 0 = fallo de red (ver "Supabase Layering" en CLAUDE.md).
public static class MensajesHttp
{
    public const string ErrorDeRed = "Error de red. Intenta de nuevo.";

    public static string ErrorEnvio(long codigoHttp, string cuerpo) =>
        codigoHttp == 0 ? ErrorDeRed : $"Error {codigoHttp}: {cuerpo}";
}
