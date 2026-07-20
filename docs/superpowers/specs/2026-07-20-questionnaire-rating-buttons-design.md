# Cuestionario Práctica 1 — captura de respuestas por botones (1-5)

## Contexto

El cuestionario de feedback de Práctica 1 (`Feedback.unity` → `Feedback2.unity`) presenta
preguntas de escala 1-5 usando botones `Btn1`..`Btn5`, pero ninguno tiene lógica de
selección conectada: son `Button` estándar sin `OnClick` wireado y sin componente que
registre cuál fue presionado. `QuestionarioPage` y `QuestionarioFinal` (los scripts que
guardan las respuestas en PlayerPrefs y las envían a Supabase) solo saben leer
`TMP_InputField` (texto libre) o `TMP_Dropdown` (opción múltiple).

Además, al mapear el flujo se encontraron dos problemas estructurales:

1. `Feedback.unity` → `QuestionarioPage.siguienteScene` apunta a `"CuestionarioPrac1-2"`,
   una escena que no existe para Práctica 1 (solo existe el patrón `CuestionarioPracX-N`
   para Práctica 2 y 3). El flujo real de Práctica 1 es `Feedback` → `Feedback2` directo.
2. `Feedback2.unity` tiene un componente `QuestionarioPage` huérfano (GameObject
   `ContinuarCuestionario`, preguntas 3 y 4) sin ningún botón conectado a
   `OnClickContinuar`. El único botón de la escena (`SubmitPrac1`) solo dispara
   `QuestionarioFinal.OnClickEnviar`, que hoy solo conoce la pregunta 5.

## Alcance

Todo el flujo de cuestionario de Práctica 1: `Feedback.unity` (preguntas 1-2) y
`Feedback2.unity` (preguntas 3-5). No incluye Práctica 2/3 (tienen su propio flujo
`CuestionarioPracX-N` ya funcional con texto/dropdown, fuera de este cambio).

## Diseño

### 1. `RatingButtonSelector` (script nuevo)

Componente en el GameObject contenedor de cada grupo de 5 botones
(`BotonEntorno`, `OpcionesP1C1` en Feedback; `OpcionesP1C3`, `OpcionesP1C4`,
`OpcionesP1C5` en Feedback2).

```csharp
public class RatingButtonSelector : MonoBehaviour
{
    public Button[] botones; // 5, orden = valor 1..5
    public int ValorSeleccionado { get; private set; } = 0;

    public void Seleccionar(int valor) { ... } // conectado desde OnClick(int) de cada botón
}
```

- `Seleccionar(valor)` guarda `ValorSeleccionado` y actualiza el color del `Image`
  (`targetGraphic`) de cada botón: el elegido a un color de resaltado, los demás a blanco
  normal.
- Cada botón se conecta en el Inspector: `Btn1.OnClick → RatingButtonSelector.Seleccionar(1)`,
  `Btn2 → Seleccionar(2)`, etc. (mismo patrón que `SeleccionarPractica.Seleccionar(int)`
  ya usado en el proyecto).
- Sin selección, `ValorSeleccionado == 0`.

### 2. `QuestionarioPage` — nuevo tipo de pregunta

- `TipoPregunta` gana un valor: `Escala`.
- Nuevos campos por pregunta: `public RatingButtonSelector botonesPregunta1/2`.
- `ObtenerRespuesta(...)` añade la rama `Escala`: retorna
  `botones.ValorSeleccionado > 0 ? botones.ValorSeleccionado.ToString() : ""`.
  Una respuesta vacía sigue bloqueando "Continuar" igual que hoy (mensaje
  "Responde ambas preguntas antes de continuar.").
- El valor guardado en PlayerPrefs (`q_N_respuesta`) es el número como texto ("3"),
  mismo formato que hoy usan texto/dropdown — no cambia `QuestionarioFinal.ConstruirJson`.

### 3. `QuestionarioFinal` — fusión de preguntas 3, 4 y 5

- Se agregan campos `numeroPregunta3/textoPregunta3/tipoPregunta3/botonesPregunta3` y
  lo mismo para pregunta 4 (mismo patrón que pregunta 5 ya existente).
- `OnClickEnviar()` valida y guarda las 3 preguntas (3, 4, 5) antes de enviar/navegar.
- Se elimina el componente `QuestionarioPage` huérfano del GameObject
  `ContinuarCuestionario` en `Feedback2.unity` (o el GameObject completo si no cumple
  otra función).

### 4. Arreglos de escena

- `Feedback.unity`: `QuestionarioPage.siguienteScene` → `"Feedback2"`.
- `Feedback.unity`: `textoPregunta1` → `"¿Qué tan fácil fue navegar y moverte dentro del
  entorno virtual?"`, `textoPregunta2` → `"¿Las instrucciones en pantalla fueron claras y
  fáciles de seguir?"` (hoy tienen texto de otra pregunta, copiado por error — este texto
  es la clave que se guarda junto a la respuesta y se envía a Supabase).
- Ambas escenas: añadir `RatingButtonSelector` a cada grupo de 5 botones, asignar
  `botones[0..4]`, conectar `OnClick(int)` de cada botón, y asignar las referencias en
  `QuestionarioPage`/`QuestionarioFinal` (`tipoPreguntaN = Escala`,
  `botonesPreguntaN = <selector>`).

## Testing

No hay suite automatizada en este proyecto (validación manual vía Play Mode, por
convención del repo). Plan de verificación manual:

1. Play Mode en `Feedback.unity`: seleccionar una opción en cada pregunta, confirmar
   resaltado visual, pulsar "Continuar" → debe navegar a `Feedback2` (no a una escena
   inexistente) y loguear `q_1`/`q_2` guardados correctamente en PlayerPrefs.
2. Intentar pulsar "Continuar" sin seleccionar una opción → debe bloquear con el mensaje
   de error existente.
3. Play Mode en `Feedback2.unity`: seleccionar las 3 preguntas, pulsar "Enviar" → debe
   guardar `q_3`, `q_4`, `q_5` y (rol alumno) enviarlos a Supabase junto con `q_1`/`q_2`
   ya guardados, navegando a `EnvioExitoso`.
4. Confirmar en el JSON enviado (log de consola) que las 5 preguntas usan el texto
   correcto como clave.
