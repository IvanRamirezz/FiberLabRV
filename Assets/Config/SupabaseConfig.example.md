# SupabaseConfig.asset

`Assets/Config/SupabaseConfig.asset` está en `.gitignore` a propósito porque
contiene las credenciales reales de Supabase (URL del proyecto y clave
anónima). Cada desarrollador debe crear su propia copia local; no existe un
asset versionado con valores reales.

## Cómo crearlo

1. En el editor de Unity: `Assets > Create > Config > SupabaseConfig`.
2. Guardarlo como `Assets/Config/SupabaseConfig.asset` (la ruta importa: es la
   que ignora git y la que referencian las escenas).
3. Completar los campos en el Inspector:

| Campo | Descripción |
|-------|-------------|
| `url` | URL del proyecto Supabase (`https://<project-ref>.supabase.co`). |
| `anonKey` | Clave anónima (`anon` / `public`) del proyecto, **no** la `service_role`. |

4. Arrastrar el asset a cada MonoBehaviour que lo requiera (`SessionManager`,
   `JoinGroup`, `CalificacionesLoader`, `CheckPractica`, `LoginSupabaseREST`,
   `QuestionarioFinal`, `SatisfaccionGate`, `P1_InstructionManager`, etc.) en
   los campos `supabaseConfig` del Inspector de cada escena.

## Dónde obtener los valores

Supabase Dashboard → proyecto correspondiente → **Project Settings > API**:
la URL y la clave `anon public` están ahí.

## Reglas

- Nunca commitear `SupabaseConfig.asset` con valores reales.
- Nunca pegar la URL ni la clave en logs, mensajes de commit o este
  repositorio.
- Ver `Assets/Scripts/Shared/SupabaseConfig.cs` para la definición del
  ScriptableObject.
