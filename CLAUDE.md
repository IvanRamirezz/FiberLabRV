# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**FiberLabRV** — Unity 6 (6000.5.4f1) VR educational app for fiber optics lab training, targeting Android with Google Cardboard XR Plugin 1.29.0. Supports two modes: VR (Cardboard headset) and Normal (desktop with joystick).

## Building and Running

This is a Unity project — building the Android player, running scenes and Play Mode validation are done through the **Unity Editor 6000.5.4f1**. The only CLI workflow is the batch-mode EditMode test run described below.

- Open project in Unity Hub → Unity 6000.5.4f1
- Build target: Android (File > Build Settings)
- Enter Play Mode in the Editor to test desktop/Normal mode
- `GameModeManager.IsVRMode` (PlayerPrefs key `"GameMode"`) switches between modes at runtime

Only the pure domain logic in `Assets/Scripts/Logic/` has automated tests: EditMode tests in `Assets/Tests/EditMode/` (one `*LogicaTests.cs` per `*Logica` class plus `MensajesHttpTests.cs`; 162 as of writing). Everything else (MonoBehaviours, repositories, VR interaction) is validated manually in Play Mode. Run the EditMode suite from the terminal with the Editor **closed** (a project open in another Unity instance cannot be opened in batch mode); this also compiles the project, so `error CS` in the log means a broken build:

```bash
/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics \
  -projectPath "$PWD" -runTests -testPlatform EditMode \
  -testResults /path/to/results.xml -logFile /path/to/unity.log
```

Do not pass `-quit` together with `-runTests`. Exit code 0 = all passed, 2 = test failures.

## Script Locations

Custom game scripts live in two roots:

| Root | Contents |
|------|----------|
| `Assets/Scripts/` | Auth, Navigation, Network, Questionnaire, Shared utilities |
| `Assets/Samples/Google Cardboard XR Plugin for Unity/1.29.0/Hello Cardboard/Scripts/` | All VR interaction, instruction managers, P1, P3, controllers |

`Packages/manifest.json` also pulls in XR Interaction Toolkit, XR Hands, ARKit, Oculus, and ProBuilder — these are unused sample/leftover dependencies. Only `com.google.xr.cardboard` is part of the active VR pipeline.

`docs/superpowers/specs/` and `docs/superpowers/plans/` hold design specs and implementation plans for past features (touch controls, questionnaire rating buttons) — check them before reworking those areas.

`Assets/Practica1_Documentacion.md` is stale: it describes `P1_BufferMenu.cs` and `P1_GlobalCalcPanel.cs`, which no longer exist, omits `P1_Logica`/`P1_DatosRepository`/`P1_CablePart`/`P1_FiberIdentity`, and names the wrong shared scene. Trust the code in `Scripts/Practica1/` over it.

`docs/diagramas_arquitectura.md` has a class diagram and component flowchart covering every script under `Hello Cardboard/Scripts` — consult it for class-level relationships (e.g. which classes implement `IFocusable`, what `HandInteraction` touches) rather than re-deriving them from scratch.

Assembly layout:

| Assembly | Location | Holds | References |
|----------|----------|-------|------------|
| `Shared` | `Assets/Scripts/Shared/Shared.asmdef` | `SupabaseConfig.cs`, `JsonHelper.cs` | — |
| `Logic` | `Assets/Scripts/Logic/Logic.asmdef` | every `*Logica` class (`LoginLogica`, `JoinGroupLogica`, `CheckPracticaLogica`, `CalificacionesLogica`, `SatisfaccionLogica`, `QuestionarioFinalLogica`, `P1_Logica`) and `MensajesHttp` | `Shared` |
| `CardboardSamples` | `Assets/Samples/.../Hello Cardboard/Scripts/CardboardSamples.asmdef` | all VR interaction, instruction managers, P1, P3 | Cardboard, InputSystem, XR Management, TextMeshPro, `Shared`, `Logic` |
| `EditMode` | `Assets/Tests/EditMode/EditMode.asmdef` | EditMode tests (Editor-only, `UNITY_INCLUDE_TESTS`) | `Logic` |
| `Assembly-CSharp` (default) | rest of `Assets/Scripts/` (Auth, Navigation, Network, Questionnaire, UI) | MonoBehaviours and `*Repository` classes; no asmdef | everything `autoReferenced` |

Consequences: `Logic` and `EditMode` cannot see `Assembly-CSharp`, so a `*Logica` class must not reference a MonoBehaviour or a `*Repository` (they take and return plain values), and the tests can only cover `Logic`. `CardboardSamples` cannot reference `Assembly-CSharp` either, so VR scripts reach Supabase only through `Shared` (`SupabaseConfig`) and `Logic`; `P1_DatosRepository` lives in `CardboardSamples` for that reason.

## Scene Flow

```
Login → MenuPrincipal → InicioRV → [Demo.unity or PracticasRV] → CuestionarioX → TablaCalif
                                                                        ↓
                                                    Feedback (SatisfaccionGate) → EnvioExitoso
```

- `Login.unity` — Supabase auth, stores `sb_access_token`, `usuario_id`, `rol` in PlayerPrefs
- `MenuPrincipal.unity` — VR/Normal mode selection, sets `GameModeManager.IsVRMode`
- `InicioRV.unity` — "Insert phone" splash screen
- **`PracticasRV.unity`** (`Assets/Samples/.../Hello Cardboard/Scenes/`) — single scene that hosts ALL practices (P1, P3, and Demo). `InicioRV.unity` serializes `LoadPracticaScene.scenePractica1/2/3` all to `"PracticasRV"` (the C# defaults `RVPractica1/2/3` match no scene and are overridden in the Inspector). Do not confuse it with `Assets/Scenes/Practica3/Practica3.unity`, `Assets/Scenes/Practica1/Practica1.unity` and `Practica2.unity`: those are separate legacy/per-practice scenes, disabled in Build Settings, and `Practica1.unity` contains no P1 script components. `Assets/_Recovery/*.unity` are editor recovery copies — ignore them.
- `CuestionarioPracX.unity` — multi-page questionnaire, answers stored in PlayerPrefs as `q_N_pregunta` / `q_N_respuesta`, submitted to Supabase `resultados` table. **P1 is the exception**: it has no questionnaire — `P1_Instrucciones.cs` computes `calificacion` itself from in-VR step scores and POSTs directly to `resultados`, then loads `Feedback` instead of a `CuestionarioPracX` scene.
- `Feedback.unity` / `Feedback2.unity` — satisfaction survey (`encuestas_satisfaccion` table), gated by `SatisfaccionGate` (see below); reached after any practice's grading flow finishes
- `EnvioExitoso.unity` — terminal "submission successful" screen, reached after the satisfaction survey is submitted or skipped

## One-Scene Architecture (Critical)

**`PracticasRV.unity` is the shared scene for all practices** (the name `Practica3.unity` used in earlier versions of this file was wrong — that is a different scene). It contains all 3D assets for P1, P3, and the Demo simultaneously. Objects belonging to inactive practices start disabled.

Which practice runs is determined entirely by which `InstructionManager` component is **enabled**:

| Manager script | Practice |
|----------------|----------|
| `P1_InstructionManager` (`P1_Instrucciones.cs`) | Práctica 1 — TIA-598-C fiber identification |
| `InstructionManagerPrac3` (`InstructionManager_P3.cs`) | Práctica 3 — optical link quality / BER |
| `InstructionManager_Demo` | VR onboarding tutorial (runs first) |

`PracticaSwitcher.cs` coordinates startup: enables Demo first; Demo calls `PracticaSwitcher.Instance.ActivarPractica()` when done, which reads `PlayerPrefs.GetInt("practica_seleccionada")` to enable the correct manager. Set `practica_seleccionada` to `1` or `3` before loading the scene.

## Input System

Legacy Input System only. No new InputSystem package.

| Action | Input | Notes |
|--------|-------|-------|
| Select / confirm | `Fire1` (Cardboard tap or mouse left) | Read through `TouchInput.ButtonDown`, not raw `Input` — see below |
| Grab cable / place | `Fire1` | `HandInteraction.BotonAgarrar`, also through `TouchInput.ButtonDown` |
| Open focus mode | `Fire1` | `HandInteraction.BotonInteractuar`, also through `TouchInput.ButtonDown` |

`HandInteraction`'s `BotonAgarrar`/`BotonInteractuar` fields default to `"Submit"`/`"Jump"` in the C# script, but the actual `HandInteraction` component on `PracticasRV.unity` overrides both to `"Fire1"` in the Inspector — check the scene's serialized value, not the script default, before assuming which physical/joystick button drives grab or focus-mode. All three actions sharing one physical button/on-screen tap is safe because a raycast hit is either a grabbable (cable/socket) or an `IFocusable` device, never both, so the unused branch is always a no-op on a given press.

### Normal-mode touch controls
On Android, Normal mode drives movement/look via on-screen touch (VR mode uses a connected controller instead). `GameModeInitializer` toggles a `touchControlsRoot` GameObject active only when `!GameModeManager.IsVRMode`; `TouchControlsBootstrap.Awake()` builds the UI at runtime (no prefab) — left-half-screen `TouchJoystick`, right-half-screen `TouchLook`, and a dedicated bottom-right `TouchActionButton` ("Start" circle) drawn on top so it captures taps before the look zone does.

Because Unity simulates mouse-button-0 from any touch, every "Fire1" read (`WaitForConfirm`, `submitButton` fields, `HandInteraction.BotonAgarrar`) **must** go through `TouchInput.ButtonDown(buttonName, ref lastTouchPressId)` instead of `Input.GetButtonDown`/`GetMouseButtonDown` directly — otherwise taps meant for the joystick/look zone would spuriously fire every confirm site. For `"Fire1"`, `ButtonDown` *replaces* physical input with the on-screen button when touch is active (avoiding that spurious fire); for any other button name (e.g. `"Submit"`, `"Jump"`, if a field is ever pointed back at one of those instead of `"Fire1"`) it instead *ORs* the on-screen button press on top of physical input, since those names are never spuriously triggered by a touch elsewhere on screen — only mouse-button-0/`Fire1` is auto-simulated from taps. Each call site owns its own `lastTouchPressId` field (init to `-10`) so it reacts exactly once per tap despite `TouchActionButton.Pressed`'s 1-frame grace window.

## Key Systems

### Player Rig
Two **separate root GameObjects** — both must be moved when teleporting:
- `RealPlayer (Movimiento)` — physical body, has `CapsuleCollider` (not `CharacterController`), drives movement
- `Player (Camara)` — camera/head rig, separate from body

Teleport pattern:
```csharp
cuerpoJugador.position = target.position;
camaraRig.position = target.position;
camaraRig.rotation = Quaternion.Euler(0f, yRot, 0f);
```

### Highlights
`Highlightable` component lives in `Iluminador3D.cs`. Call `obj.Highlight(true/false)` to pulse emission. Requires `Emission` enabled on the material's shader.

### Cable Connection
Two-layer system:
1. **Physics trigger** (`DeteccionConexion.cs` / `CableSocket.OnTriggerEnter` with tag `"CableEnd"`) — auto-connects when cable tip enters socket zone. Does NOT fire `HandInteraction.OnRelease`.
2. **Manual drop** (`HandInteraction.DropAndPlace()`) — player aims at `ConnectableDevice` and presses Submit → fires `HandInteraction.OnRelease`.

Always poll `CableEnd.connectedSocket != null` directly to detect connection, regardless of which path was used. Do not rely on `OnRelease` alone for cable detection.

`LinkStateManager` polls all 4 sockets (`txSocket`, `atenInSocket`, `atenOutSocket`, `rxSocket`) every frame — it only leaves `Disconnected` state when ALL four are assigned and connected.

### Static Events (HandInteraction)
```csharp
HandInteraction.OnGrab    // Action<GrabbableID> — fired on grab
HandInteraction.OnRelease // Action<GrabbableID> — fired on DropAndPlace only
HandInteraction.ReticleOverride // Color? — set to force reticle color for one frame
```

### Session / Auth
`SessionManager` (DontDestroyOnLoad singleton) handles Supabase JWT auth. Every scene that requires login must have a `SceneGuard` component — it validates the session UUID against the DB on Start and redirects to `Login` if invalid or duplicated (concurrent login detection).

`SupabaseConfig` is a ScriptableObject (`Assets > Create > Config > SupabaseConfig`). The `.asset` file with real credentials must not be committed to version control.

### Supabase Layering (Repository pattern)
Every Supabase-backed flow in `Assets/Scripts/` is split into layers (introduced in the "separar en capas" refactor):
- **MonoBehaviour** (`JoinGroup`, `SessionManager`, `SatisfaccionGate`, `CalificacionesLoader`, `CheckPractica`, `QuestionarioFinal`, `LoginSupabaseREST`, `P1_InstructionManager`) — owns UI, PlayerPrefs reads and scene changes; it feeds each HTTP response to its `*Logica` and acts on the result. Builds its repository and logic in `Awake()`: `repository = new XRepository(supabaseConfig)`.
- **`*Repository`** plain C# class (`SessionRepository`, `JoinGroupRepository`, `LoginRepository`, `CheckPracticaRepository`, `CalificacionesRepository`, `SatisfaccionRepository`, `QuestionarioFinalRepository`, `P1_DatosRepository`) — one coroutine per HTTP request, reports back via `Action<bool, long, string>` (ok, HTTP code, body). `code == 0` means network failure. It does not parse JSON, touch PlayerPrefs or pick the next step.
- **`*Logica`** plain C# class in `Assets/Scripts/Logic/` (`Logic.asmdef`), one per flow — pure domain logic: interprets `(ok, code, body)` into a result enum/struct (`LoginLogica.Interpretar*`, `JoinGroupLogica`, `CheckPracticaLogica`, `SatisfaccionLogica`, `CalificacionesLogica`), validates input and builds `respuestas_json` (`QuestionarioFinalLogica`, `P1_Logica`). No UI and no HTTP; unit-tested in `Assets/Tests/EditMode/`. Only `QuestionarioFinalLogica` touches PlayerPrefs (the `q_N_pregunta`/`q_N_respuesta` answers); the rest take plain values. `MensajesHttp` is a static helper that turns a failed POST `(code, body)` into the user-facing message (`code == 0` → network error).

P1 splits the POST in two: `P1_Logica.ConstruirRespuestasJson` builds the `respuestas_json` content (per-step scores) and `P1_DatosRepository.EnviarResultado` receives that string, wraps it with `alumno_id`/`practica_id`/`calificacion` and sends it. The repository no longer decides the JSON content.

Add new Supabase calls as a repository method and put the decision on the response in the matching `*Logica` (with an EditMode test); the MonoBehaviour only applies it. `JsonUtility` never leaves a nested `[Serializable]` object null — validate a field of it (e.g. `user.id`), not the reference. Never log tokens or request bodies that contain them. Several repository methods carry `NOTA:` comments documenting deliberate RLS/PostgREST edge-case behavior (e.g. 204 with zero rows updated counts as success) — read them before changing response handling.

### Access Code / Group Flow
`Assets/Scripts/Network/` and `Assets/Scripts/Navigation/` hold Supabase-driven flows outside the main practice loop, each dragging in the same `SupabaseConfig` asset:
- `JoinGroup` (`Network/JoinGroup.cs`) — student enters a group access code, looked up against `grupos.codigo_acceso`, then patches `alumnos` with the matched `grupo_id`.
- `CalificacionesLoader` (`Network/`) — reads `resultados` per `practica_id` to populate the `TablaCalif` grade display.
- `CheckPractica.cs` (`Navigation/`) — resolves the student's currently-active practice by querying `practicas_grupo` for a row where `grupo_id` matches and `fecha_inicio`/`fecha_fin` bracket the current time.

### Satisfaction Survey
Separate from the per-practice `CuestionarioPracX` knowledge questionnaire. `SatisfaccionGate` (`Assets/Scripts/Network/SatisfaccionGate.cs`), placed on `Feedback.unity`, hides the survey UI on `Start()` and queries `encuestas_satisfaccion` for an existing row for `alumno_id`. If one exists it redirects straight to `EnvioExitoso`; otherwise it reveals the UI. Non-`alumno` roles bypass the check. `QuestionarioFinal` (`Assets/Scripts/Questionnaire/QuestionarioFinal.cs`) drives the actual 3-question survey and POSTs `{alumno_id, respuestas_json}` to `encuestas_satisfaccion`, then routes to `EnvioExitoso` (role `alumno`) or `EnvioProfe_Cues` (role `profe`/admin).

### PlayerPrefs Keys (cross-scene state)

| Key | Type | Purpose |
|-----|------|---------|
| `sb_access_token` | string | Supabase JWT |
| `sb_refresh_token` | string | Refresh token |
| `usuario_id` | int | User ID |
| `alumno_id` | int | Student ID |
| `rol` | string | `"alumno"` or `"profe"` |
| `practica_id` | int | Practice assigned by server |
| `practica_seleccionada` | int | Practice to load (1 or 3) |
| `demo_completada` | int | 0/1 — whether VR tutorial was completed |
| `GameMode` | int | 0 = Normal, 1 = VR |
| `session_uuid` | string | Active session UUID for concurrent login detection |
| `q_N_pregunta` / `q_N_respuesta` | string | Questionnaire answers (N = 1–5) |

## InstructionManager Pattern

All practice flows use coroutine-driven managers enabled/disabled by `PracticaSwitcher`. The pattern:

```csharp
IEnumerator Start() { yield return StartCoroutine(RunPractice()); }

IEnumerator WaitForConfirm()
{
    yield return null; // skip frame to avoid same-frame input
    yield return new WaitUntil(() => TouchInput.ButtonDown("Fire1", ref _lastTouchPressId));
}
```

Each step is a separate `IEnumerator` method called with `yield return StartCoroutine(StepX())`. To return a value from a coroutine, use a class-level field (e.g., `bool _resultadoPregunta`) set inside the coroutine, since `yield return` cannot return values.

## Supabase Schema (relevant tables)

- `usuarios` — `usuario_id`, `active_session_uuid` (for session validation)
- `resultados` — `alumno_id`, `practica_id`, `calificacion`, `respuestas_json` (JSONB)
- `grupos` — `grupo_id`, `codigo_acceso`
- `alumnos` — `alumno_id`, `grupo_id`
- `practicas` — `id`, `titulo`, `codigo`
- `practicas_grupo` — `grupo_id`, `practica_id`, `fecha_inicio`, `fecha_fin` (assigns a practice's active time window to a group)
- `encuestas_satisfaccion` — `encuesta_id`, `alumno_id`, `respuestas_json` (JSONB) — satisfaction survey, separate from `resultados`
