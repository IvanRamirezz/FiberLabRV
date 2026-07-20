# Cuestionario Práctica 1 — captura de respuestas por botones — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Exception for this plan:** Tasks 4 and 5 hand-edit two shared `.unity` YAML scene files and allocate new `fileID`s that must not collide within each file. A fresh subagent per task cannot see fileIDs claimed by a sibling task working the same file. **Execute this plan inline, in order, in a single session** (superpowers:executing-plans) rather than via subagent-driven-development.

**Goal:** Make the 1-5 / multiple-choice buttons in `Feedback.unity` and `Feedback2.unity` actually capture the player's selection, save it to PlayerPrefs, and submit it to Supabase — replacing the current no-op buttons.

**Architecture:** A new generic `ButtonChoiceSelector` component sits on each button-group container and tracks which child button was pressed (by index and by its own label text). `QuestionarioPage` (Feedback, questions 1-2) and `QuestionarioFinal` (Feedback2, questions 3-5) gain a third `TipoPregunta` case, `BotonesOpcion`, that reads `TextoSeleccionado` from the assigned selector the same way they already read `TMP_InputField`/`TMP_Dropdown`. Two pre-existing structural bugs (a dead-end scene reference, an orphaned unwired component, and mislabeled question text on both scenes) are fixed as part of the same change since they sit in the same files and were required for the flow to work end-to-end.

**Tech Stack:** Unity 6000.5.4f1, C# (Assembly-CSharp), TextMeshPro, uGUI (`Button`, `Image`), PlayerPrefs, Supabase REST (`UnityWebRequest`).

## Global Constraints

- No automated test suite exists in this repo (per `CLAUDE.md`) — validation is manual via Unity Play Mode. Every task ends with a manual verification step instead of an automated test run.
- Scene files (`.unity`) are hand-edited as YAML text — Unity is not available to run in batch mode because the user has the project open in the Editor (confirmed: batchmode fails with "another Unity instance is running with this project open"). All edits must produce syntactically valid YAML that Unity can reload without errors.
- `fileID`s are unique **per file only** — a `fileID` used in `Feedback.unity` may coincidentally also appear in `Feedback2.unity`; that is not a collision.
- Follow the existing project convention for wiring buttons: `OnClick` → method with an `int` parameter, configured from the Inspector (`m_Mode: 3`, `m_IntArgument: N`), same pattern as `SeleccionarPractica.Seleccionar(int)`.
- Answer values stored in PlayerPrefs (`q_N_respuesta`) and sent to Supabase must be human-readable text (button label), matching the existing convention for text/dropdown answers — never a raw numeric index alone.

---

### Task 1: `ButtonChoiceSelector` component

**Files:**
- Create: `Assets/Scripts/Questionnaire/ButtonChoiceSelector.cs`
- Create: `Assets/Scripts/Questionnaire/ButtonChoiceSelector.cs.meta` (guid `b8d6d826ea1943a2af237acd35d56511`)

**Interfaces:**
- Produces: `public class ButtonChoiceSelector : MonoBehaviour` with `public Button[] botones`, `public int IndiceSeleccionado { get; }` (-1 = none), `public string TextoSeleccionado { get; }` (`""` = none), `public void Seleccionar(int indice)`.

- [ ] **Step 1: Write the script**

```csharp
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
```

- [ ] **Step 2: Create the `.meta` file with a fixed GUID**

Unity normally generates this automatically on import, but since no Editor
instance is available to import right now, create it manually so the GUID
used later in the scene edits (Tasks 4-5) is known in advance:

```yaml
fileFormatVersion: 2
guid: b8d6d826ea1943a2af237acd35d56511
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
```

- [ ] **Step 3: Manual verification**

Open the project in Unity Editor (or let the reload happen if it's already
open) and confirm in the Console there are no compile errors, and that
`ButtonChoiceSelector` appears as an addable component (Add Component →
search "ButtonChoiceSelector").

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Questionnaire/ButtonChoiceSelector.cs Assets/Scripts/Questionnaire/ButtonChoiceSelector.cs.meta
git commit -m "feat: add ButtonChoiceSelector for questionnaire choice buttons"
```

---

### Task 2: Extend `QuestionarioPage` with the `BotonesOpcion` question type

**Files:**
- Modify: `Assets/Scripts/Questionnaire/Questionariopage.cs`

**Interfaces:**
- Consumes: `ButtonChoiceSelector.TextoSeleccionado` (Task 1).
- Produces: `QuestionarioPage.TipoPregunta.BotonesOpcion` (enum value `2`), fields `botonesPregunta1`/`botonesPregunta2`, used by Task 4 (scene wiring) and referenced the same way `QuestionarioFinal` will use its own copy (Task 3).

- [ ] **Step 1: Add the enum case and fields**

In `Assets/Scripts/Questionnaire/Questionariopage.cs`, change:

```csharp
    [Header("Respuesta Pregunta 1")]
    public TMP_InputField inputPregunta1;       // para texto libre
    public TMP_Dropdown dropdownPregunta1;    // para opción múltiple
```

to:

```csharp
    [Header("Respuesta Pregunta 1")]
    public TMP_InputField inputPregunta1;       // para texto libre
    public TMP_Dropdown dropdownPregunta1;    // para opción múltiple
    public ButtonChoiceSelector botonesPregunta1; // para botones de opción
```

and change:

```csharp
    [Header("Respuesta Pregunta 2")]
    public TMP_InputField inputPregunta2;
    public TMP_Dropdown dropdownPregunta2;
```

to:

```csharp
    [Header("Respuesta Pregunta 2")]
    public TMP_InputField inputPregunta2;
    public TMP_Dropdown dropdownPregunta2;
    public ButtonChoiceSelector botonesPregunta2;
```

and change:

```csharp
    public enum TipoPregunta { TextoLibre, OpcionMultiple }
```

to:

```csharp
    public enum TipoPregunta { TextoLibre, OpcionMultiple, BotonesOpcion }
```

- [ ] **Step 2: Read the button selection in `OnClickContinuar`**

Change:

```csharp
    public void OnClickContinuar()
    {
        string respuesta1 = ObtenerRespuesta(tipoPregunta1, inputPregunta1, dropdownPregunta1);
        string respuesta2 = ObtenerRespuesta(tipoPregunta2, inputPregunta2, dropdownPregunta2);
```

to:

```csharp
    public void OnClickContinuar()
    {
        string respuesta1 = ObtenerRespuesta(tipoPregunta1, inputPregunta1, dropdownPregunta1, botonesPregunta1);
        string respuesta2 = ObtenerRespuesta(tipoPregunta2, inputPregunta2, dropdownPregunta2, botonesPregunta2);
```

- [ ] **Step 3: Extend `ObtenerRespuesta` with the new branch**

Change:

```csharp
    private string ObtenerRespuesta(TipoPregunta tipo, TMP_InputField input, TMP_Dropdown dropdown)
    {
        if (tipo == TipoPregunta.TextoLibre)
            return input != null ? input.text.Trim() : "";
        else
            return dropdown != null ? dropdown.options[dropdown.value].text : "";
    }
```

to:

```csharp
    private string ObtenerRespuesta(TipoPregunta tipo, TMP_InputField input, TMP_Dropdown dropdown, ButtonChoiceSelector botones)
    {
        if (tipo == TipoPregunta.TextoLibre)
            return input != null ? input.text.Trim() : "";
        else if (tipo == TipoPregunta.OpcionMultiple)
            return dropdown != null ? dropdown.options[dropdown.value].text : "";
        else
            return botones != null ? botones.TextoSeleccionado : "";
    }
```

- [ ] **Step 4: Manual verification**

Reload the project in Unity Editor, confirm no compile errors in the
Console, and open `Feedback.unity` — the `QuestionarioPage` component on
`ContinuarCuestionario` should now show a `Botones Pregunta 1` / `Botones
Pregunta 2` field in the Inspector alongside the existing Input/Dropdown
fields.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Questionnaire/Questionariopage.cs
git commit -m "feat: support button-group answers in QuestionarioPage"
```

---

### Task 3: Extend `QuestionarioFinal` with questions 3, 4 and the `BotonesOpcion` type

**Files:**
- Modify: `Assets/Scripts/Questionnaire/Questionariofinal.cs`

**Interfaces:**
- Consumes: `QuestionarioPage.TipoPregunta` (Task 2, reused — `QuestionarioFinal` already reuses this enum), `ButtonChoiceSelector.TextoSeleccionado` (Task 1).
- Produces: `numeroPregunta3/4`, `textoPregunta3/4`, `tipoPregunta3/4`, `botonesPregunta3/4/5` fields consumed by Task 5 (scene wiring).

- [ ] **Step 1: Add Pregunta 3 and Pregunta 4 fields**

In `Assets/Scripts/Questionnaire/Questionariofinal.cs`, change:

```csharp
public class QuestionarioFinal : MonoBehaviour
{
    [Header("Pregunta 5 (ultima)")]
    public int numeroPregunta5 = 5;
    [TextArea] public string textoPregunta5 = "Pregunta 5...";
    public QuestionarioPage.TipoPregunta tipoPregunta5 = QuestionarioPage.TipoPregunta.TextoLibre;
    public TMP_InputField inputPregunta5;
    public TMP_Dropdown dropdownPregunta5;
```

to:

```csharp
public class QuestionarioFinal : MonoBehaviour
{
    [Header("Pregunta 3")]
    public int numeroPregunta3 = 3;
    [TextArea] public string textoPregunta3 = "Pregunta 3...";
    public QuestionarioPage.TipoPregunta tipoPregunta3 = QuestionarioPage.TipoPregunta.TextoLibre;
    public TMP_InputField inputPregunta3;
    public TMP_Dropdown dropdownPregunta3;
    public ButtonChoiceSelector botonesPregunta3;

    [Header("Pregunta 4")]
    public int numeroPregunta4 = 4;
    [TextArea] public string textoPregunta4 = "Pregunta 4...";
    public QuestionarioPage.TipoPregunta tipoPregunta4 = QuestionarioPage.TipoPregunta.TextoLibre;
    public TMP_InputField inputPregunta4;
    public TMP_Dropdown dropdownPregunta4;
    public ButtonChoiceSelector botonesPregunta4;

    [Header("Pregunta 5 (ultima)")]
    public int numeroPregunta5 = 5;
    [TextArea] public string textoPregunta5 = "Pregunta 5...";
    public QuestionarioPage.TipoPregunta tipoPregunta5 = QuestionarioPage.TipoPregunta.TextoLibre;
    public TMP_InputField inputPregunta5;
    public TMP_Dropdown dropdownPregunta5;
    public ButtonChoiceSelector botonesPregunta5;
```

- [ ] **Step 2: Validate and save all three questions in `OnClickEnviar`**

Change:

```csharp
    public void OnClickEnviar()
    {
        string respuesta5 = ObtenerRespuesta(tipoPregunta5, inputPregunta5, dropdownPregunta5);

        if (string.IsNullOrEmpty(respuesta5))
        {
            SetMsg("Responde la pregunta antes de enviar.");
            return;
        }

        GuardarRespuesta(numeroPregunta5, textoPregunta5, respuesta5);
```

to:

```csharp
    public void OnClickEnviar()
    {
        string respuesta3 = ObtenerRespuesta(tipoPregunta3, inputPregunta3, dropdownPregunta3, botonesPregunta3);
        string respuesta4 = ObtenerRespuesta(tipoPregunta4, inputPregunta4, dropdownPregunta4, botonesPregunta4);
        string respuesta5 = ObtenerRespuesta(tipoPregunta5, inputPregunta5, dropdownPregunta5, botonesPregunta5);

        if (string.IsNullOrEmpty(respuesta3) || string.IsNullOrEmpty(respuesta4) || string.IsNullOrEmpty(respuesta5))
        {
            SetMsg("Responde todas las preguntas antes de enviar.");
            return;
        }

        GuardarRespuesta(numeroPregunta3, textoPregunta3, respuesta3);
        GuardarRespuesta(numeroPregunta4, textoPregunta4, respuesta4);
        GuardarRespuesta(numeroPregunta5, textoPregunta5, respuesta5);
```

- [ ] **Step 3: Extend the private `ObtenerRespuesta` helper**

Change:

```csharp
    private string ObtenerRespuesta(QuestionarioPage.TipoPregunta tipo,
                                    TMP_InputField input, TMP_Dropdown dropdown)
    {
        if (tipo == QuestionarioPage.TipoPregunta.TextoLibre)
            return input != null ? input.text.Trim() : "";
        else
            return dropdown != null ? dropdown.options[dropdown.value].text : "";
    }
```

to:

```csharp
    private string ObtenerRespuesta(QuestionarioPage.TipoPregunta tipo,
                                    TMP_InputField input, TMP_Dropdown dropdown,
                                    ButtonChoiceSelector botones)
    {
        if (tipo == QuestionarioPage.TipoPregunta.TextoLibre)
            return input != null ? input.text.Trim() : "";
        else if (tipo == QuestionarioPage.TipoPregunta.OpcionMultiple)
            return dropdown != null ? dropdown.options[dropdown.value].text : "";
        else
            return botones != null ? botones.TextoSeleccionado : "";
    }
```

- [ ] **Step 4: Update `LimpiarRespuestasGuardadas` call sites — no change needed**

`ConstruirJson(totalPreguntas: 5)` and `LimpiarRespuestasGuardadas(totalPreguntas: 5)` already
loop `1..5` reading `q_{i}_pregunta`/`q_{i}_respuesta` from PlayerPrefs — since Task 4
(Feedback) already saves `q_1`/`q_2` and this task now saves `q_3`/`q_4`/`q_5`, no change
is required here. Leave both calls as `totalPreguntas: 5`.

- [ ] **Step 5: Manual verification**

Reload in Unity Editor, confirm no compile errors. Open `Feedback2.unity` and confirm the
`QuestionarioFinal` component on `SubmitPrac1` now shows `Pregunta 3` / `Pregunta 4` /
`Pregunta 5` sections in the Inspector.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Questionnaire/Questionariofinal.cs
git commit -m "feat: capture questions 3-4 and button answers in QuestionarioFinal"
```

---

### Task 4: Wire `Feedback.unity` (questions 1-2)

**Files:**
- Modify: `Assets/Scenes/Flow/Feedback.unity`

**Interfaces:**
- Consumes: `ButtonChoiceSelector` (Task 1, guid `b8d6d826ea1943a2af237acd35d56511`), `QuestionarioPage.botonesPregunta1/2` and `TipoPregunta.BotonesOpcion` (Task 2).

Button → index mapping (order doesn't affect correctness since the selector reads each
button's own label text at click time, but keeps `botones[]` in scene child order):

| Group | Question | Button fileID | OnClick index |
|---|---|---|---|
| OpcionesP1C1 (Q1) | navegación | 81730002 | 0 |
| OpcionesP1C1 | | 601736379 | 1 |
| OpcionesP1C1 | | 779167928 | 2 |
| OpcionesP1C1 | | 739445538 | 3 |
| OpcionesP1C1 | | 1065815238 | 4 |
| OpcionesP1C2 (Q2) | instrucciones | 874277496 | 0 |
| OpcionesP1C2 | | 171370702 | 1 |
| OpcionesP1C2 | | 1152051167 | 2 |
| OpcionesP1C2 | | 1542309425 | 3 |
| OpcionesP1C2 | | 916285851 | 4 |

New `ButtonChoiceSelector` component fileIDs to create: **564743646** (on `OpcionesP1C1`,
GameObject fileID `903861294`), **826814281** (on `OpcionesP1C2`, GameObject fileID
`2012046206`).

`QuestionarioPage` component to edit: fileID `1490713430` (on GameObject
`ContinuarCuestionario`, fileID `1490713427`).

- [ ] **Step 1: Add the `ButtonChoiceSelector` component for Q1**

Find the `OpcionesP1C1` GameObject block (starts `--- !u!1 &903861294`). Its
`m_Component` list currently is:

```yaml
  m_Component:
  - component: {fileID: 903861295}
  - component: {fileID: 903861296}
```

Change it to:

```yaml
  m_Component:
  - component: {fileID: 903861295}
  - component: {fileID: 903861296}
  - component: {fileID: 564743646}
```

Immediately after the existing `CanvasRenderer` block for this GameObject (`--- !u!222
&903861296`, ends right before the next `--- !u!1 &...` GameObject block), insert a new
MonoBehaviour block:

```yaml
--- !u!114 &564743646
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 903861294}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: b8d6d826ea1943a2af237acd35d56511, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  botones:
  - {fileID: 81730002}
  - {fileID: 601736379}
  - {fileID: 779167928}
  - {fileID: 739445538}
  - {fileID: 1065815238}
  colorSeleccionado: {r: 0.2, g: 0.6, b: 0.9, a: 1}
```

- [ ] **Step 2: Add the `ButtonChoiceSelector` component for Q2**

Same pattern on the `OpcionesP1C2` GameObject block (`--- !u!1 &2012046206`), whose
`m_Component` list currently is:

```yaml
  m_Component:
  - component: {fileID: 2012046207}
  - component: {fileID: 2012046208}
```

Change to:

```yaml
  m_Component:
  - component: {fileID: 2012046207}
  - component: {fileID: 2012046208}
  - component: {fileID: 826814281}
```

Insert after its `CanvasRenderer` block:

```yaml
--- !u!114 &826814281
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 2012046206}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: b8d6d826ea1943a2af237acd35d56511, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  botones:
  - {fileID: 874277496}
  - {fileID: 171370702}
  - {fileID: 1152051167}
  - {fileID: 1542309425}
  - {fileID: 916285851}
  colorSeleccionado: {r: 0.2, g: 0.6, b: 0.9, a: 1}
```

- [ ] **Step 3: Wire each button's `OnClick(int)`**

For each of the 10 button fileIDs in the table above, find its `Button` MonoBehaviour
block (`--- !u!114 &<fileID>`) and replace its empty `m_OnClick`:

```yaml
  m_OnClick:
    m_PersistentCalls:
      m_Calls: []
```

with (substituting `<TARGET>` = the selector fileID for that button's group — `564743646`
for the 5 OpcionesP1C1 buttons, `826814281` for the 5 OpcionesP1C2 buttons — and
`<INDEX>` from the table):

```yaml
  m_OnClick:
    m_PersistentCalls:
      m_Calls:
      - m_Target: {fileID: <TARGET>}
        m_TargetAssemblyTypeName: ButtonChoiceSelector, Assembly-CSharp
        m_MethodName: Seleccionar
        m_Mode: 3
        m_Arguments:
          m_ObjectArgument: {fileID: 0}
          m_ObjectArgumentAssemblyTypeName: UnityEngine.Object, UnityEngine
          m_IntArgument: <INDEX>
          m_FloatArgument: 0
          m_StringArgument: 
          m_BoolArgument: 0
        m_CallState: 2
```

- [ ] **Step 4: Update the `QuestionarioPage` component fields**

Find `--- !u!114 &1490713430` and change:

```yaml
  textoPregunta1: "Describe brevemente la funci\xF3n de los siguientes componentes
    de un cable de fibra \xF3ptica: Esqueleto central de fibra de vidrio, Hilos de
    Kevlar y Buffers o tubos."
  tipoPregunta1: 0
  inputPregunta1: {fileID: 0}
  dropdownPregunta1: {fileID: 0}
  numeroPregunta2: 2
  textoPregunta2: "Durante la pr\xE1ctica, identificaste fibras \xF3pticas numeradas.
    \xBFPor qu\xE9 es necesario asignar colores diferentes a cada fibra dentro de
    un mismo cable?"
  tipoPregunta2: 0
  inputPregunta2: {fileID: 0}
  dropdownPregunta2: {fileID: 0}
  siguienteScene: CuestionarioPrac1-2
```

to:

```yaml
  textoPregunta1: "\xBFQu\xE9 tan f\xE1cil fue navegar y moverte dentro del entorno
    virtual?"
  tipoPregunta1: 2
  inputPregunta1: {fileID: 0}
  dropdownPregunta1: {fileID: 0}
  botonesPregunta1: {fileID: 564743646}
  numeroPregunta2: 2
  textoPregunta2: "\xBFLas instrucciones en pantalla fueron claras y f\xE1ciles
    de seguir?"
  tipoPregunta2: 2
  inputPregunta2: {fileID: 0}
  dropdownPregunta2: {fileID: 0}
  botonesPregunta2: {fileID: 826814281}
  siguienteScene: Feedback2
```

(`\xBF` = `¿`, `\xE9` = `é`, `\xE1` = `á` — Unity's YAML escaping for non-ASCII; keep the
same escaping style as the surrounding file.)

- [ ] **Step 5: Manual verification**

In Unity Editor: let the scene reload (click the `Feedback` tab or reopen the scene),
confirm the Console shows no YAML/parse errors and no missing-script warnings on
`OpcionesP1C1`/`OpcionesP1C2`/`ContinuarCuestionario`. Enter Play Mode, click one button
in each question row, confirm it highlights blue and the others reset to white, then
click "Continuar" — it should navigate to `Feedback2` (not error about a missing scene).

- [ ] **Step 6: Commit**

```bash
git add Assets/Scenes/Flow/Feedback.unity
git commit -m "fix: wire question 1-2 buttons and next-scene reference in Feedback"
```

---

### Task 5: Wire `Feedback2.unity` (questions 3-5)

**Files:**
- Modify: `Assets/Scenes/Flow/Feedback2.unity`

**Interfaces:**
- Consumes: `ButtonChoiceSelector` (Task 1), `QuestionarioFinal.botonesPregunta3/4/5` and
  `TipoPregunta.BotonesOpcion` (Task 3).

| Group | Question | Button fileID | OnClick index |
|---|---|---|---|
| OpcionesP1C3 (Q3, 3 opciones) | ritmo | 999774194 | 0 |
| OpcionesP1C3 | | 305638841 | 1 |
| OpcionesP1C3 | | 846571513 | 2 |
| OpcionesP1C4 (Q4, 1-5) | claridad | 1099178542 | 0 |
| OpcionesP1C4 | | 292174662 | 1 |
| OpcionesP1C4 | | 356340281 | 2 |
| OpcionesP1C4 | | 1976055936 | 3 |
| OpcionesP1C4 | | 342312157 | 4 |
| OpcionesP1C5 (Q5, Sí/No) | recomendación | 1798367325 | 0 |
| OpcionesP1C5 | | 523787467 | 1 |

New `ButtonChoiceSelector` component fileIDs to create: **560424746** (on
`OpcionesP1C3`, GameObject fileID to be found by name in Step 1), **845231295** (on
`OpcionesP1C4`), **955027030** (on `OpcionesP1C5`).

`QuestionarioFinal` component to edit: fileID `815816317` (on GameObject `SubmitPrac1`).
Orphaned `QuestionarioPage` component to disable: fileID `2031091022` (on GameObject
`ContinuarCuestionario`, fileID `2031091019`).

- [ ] **Step 1: Add `ButtonChoiceSelector` for Q3, Q4, Q5**

Find the `OpcionesP1C3` GameObject (`m_Name: OpcionesP1C3`). Its `m_Component` list is:

```yaml
  m_Component:
  - component: {fileID: 813486027}
  - component: {fileID: 813486028}
```

Change to:

```yaml
  m_Component:
  - component: {fileID: 813486027}
  - component: {fileID: 813486028}
  - component: {fileID: 560424746}
```

Insert after its `CanvasRenderer` block:

```yaml
--- !u!114 &560424746
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: <OpcionesP1C3 GameObject fileID>}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: b8d6d826ea1943a2af237acd35d56511, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  botones:
  - {fileID: 999774194}
  - {fileID: 305638841}
  - {fileID: 846571513}
  colorSeleccionado: {r: 0.2, g: 0.6, b: 0.9, a: 1}
```

(`<OpcionesP1C3 GameObject fileID>` — read it from the `--- !u!1 &<id>` line directly
above `m_Name: OpcionesP1C3`; do not guess it.)

Repeat identically for `OpcionesP1C4` (component list gains `- component: {fileID:
845231295}`; new block `--- !u!114 &845231295` with `botones:` = `1099178542,
292174662, 356340281, 1976055936, 342312157`) and `OpcionesP1C5` (component list gains
`- component: {fileID: 955027030}`; new block `--- !u!114 &955027030` with `botones:` =
`1798367325, 523787467`). Use each group's own GameObject fileID for `m_GameObject`.

- [ ] **Step 2: Wire each button's `OnClick(int)`**

Same pattern as Task 4 Step 3, for the 10 buttons in this scene's table, targeting
`560424746` (OpcionesP1C3 buttons), `845231295` (OpcionesP1C4 buttons), or `955027030`
(OpcionesP1C5 buttons) with the matching index.

- [ ] **Step 3: Disable the orphaned `QuestionarioPage` component**

Find `--- !u!114 &2031091022` (the `QuestionarioPage` on `ContinuarCuestionario`). It has
no button pointing at its `OnClickContinuar`, and its questions (3 and 4, worded about
fiber-optic colors/buffers) are being replaced by the real on-screen questions 3-4 now
owned by `QuestionarioFinal`. Change only:

```yaml
  m_Enabled: 1
```

to:

```yaml
  m_Enabled: 0
```

Leave the rest of the block untouched — disabling is enough to make it inert without
touching the GameObject's component list or the scene's root object list (safer than
deleting, since no Unity Editor is available right now to confirm the scene reloads
cleanly after a structural removal).

- [ ] **Step 4: Update the `QuestionarioFinal` component fields**

Find `--- !u!114 &815816317`. Change:

```yaml
  numeroPregunta5: 5
  textoPregunta5: "Reflexiona sobre la utilidad del entorno de realidad virtual utilizado
    en esta pr\xE1ctica. \xBFQu\xE9 beneficios aporta frente a una pr\xE1ctica presencial
    con cable f\xEDsico?"
  tipoPregunta5: 0
  inputPregunta5: {fileID: 0}
  dropdownPregunta5: {fileID: 0}
  sceneExito: EnvioExitoso
```

to:

```yaml
  numeroPregunta3: 3
  textoPregunta3: "\xBFEl ritmo de la pr\xE1ctica fue adecuado?"
  tipoPregunta3: 2
  inputPregunta3: {fileID: 0}
  dropdownPregunta3: {fileID: 0}
  botonesPregunta3: {fileID: 560424746}
  numeroPregunta4: 4
  textoPregunta4: "\xBFQu\xE9 tan claro te qued\xF3 el tema despu\xE9s de realizar
    la pr\xE1ctica?"
  tipoPregunta4: 2
  inputPregunta4: {fileID: 0}
  dropdownPregunta4: {fileID: 0}
  botonesPregunta4: {fileID: 845231295}
  numeroPregunta5: 5
  textoPregunta5: "\xBFRecomendar\xEDas esta forma de aprender a un compa\xF1ero?"
  tipoPregunta5: 2
  inputPregunta5: {fileID: 0}
  dropdownPregunta5: {fileID: 0}
  botonesPregunta5: {fileID: 955027030}
  sceneExito: EnvioExitoso
```

- [ ] **Step 5: Manual verification**

Reload `Feedback2.unity` in Unity Editor, confirm no console errors and no
missing-script warnings on `OpcionesP1C3`/`OpcionesP1C4`/`OpcionesP1C5`. Enter Play Mode
(via `Feedback.unity` → "Continuar" so `PlayerPrefs` for q_1/q_2 are populated, or
directly if just checking this scene), select one option per question, confirm
highlight/reset behaves correctly for groups of 3, 5, and 2 buttons, then click "Enviar
respuestas" and confirm (per role) it either posts to Supabase and loads `EnvioExitoso`,
or navigates to `EnvioProfe_Cues` — check the Console log line `"JSON a enviar: ..."`
includes all 5 questions with the corrected text as keys.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scenes/Flow/Feedback2.unity
git commit -m "fix: wire question 3-5 buttons, merge orphaned questions into QuestionarioFinal"
```

---

### Task 6: End-to-end manual verification

**Files:** none (verification only).

- [ ] **Step 1** — Play Mode from `Feedback.unity`: answer both questions, confirm
  `Debug.Log("Guardada pregunta 1: ...")` / `"Guardada pregunta 2: ..."` show the button
  label text (e.g. "5", "Adecuado") in the Console, then confirm "Continuar" is blocked
  with the existing error message if you try it before answering both.
- [ ] **Step 2** — Confirm clicking "Continuar" after answering both loads `Feedback2`.
- [ ] **Step 3** — In `Feedback2`, answer all three questions, confirm "Enviar
  respuestas" is blocked until all three are answered, then confirm it saves q_3/q_4/q_5
  and proceeds per role (alumno → Supabase POST logged as 2xx → `EnvioExitoso`; profe →
  `EnvioProfe_Cues`).
- [ ] **Step 4** — Report back to the user with the observed Play Mode results (this
  plan's author cannot run Unity Editor directly — the user must perform this final
  check and confirm).
