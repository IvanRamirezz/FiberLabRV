# Práctica 1 — Fibra Óptica (Documentación Técnica)

## Descripción general

Práctica educativa de realidad virtual para enseñar el **estándar de colores TIA-598-C** aplicado a cables de fibra óptica. El estudiante aprende a identificar búferes, colores de fibras individuales y calcular la posición global de una fibra dentro de un cable de 60 hilos (5 búferes × 12 fibras).

---

## Estándar de colores TIA-598-C

| Posición | Color       |
|----------|-------------|
| 1        | Azul        |
| 2        | Naranja     |
| 3        | Verde       |
| 4        | Café        |
| 5        | Gris        |
| 6        | Blanco      |
| 7        | Rojo        |
| 8        | Negro       |
| 9        | Amarillo    |
| 10       | Violeta     |
| 11       | Rosa        |
| 12       | Aguamarina  |

---

## Estructura del cable

- **5 búferes** (Cintas Milares) numerados del 1 al 5
- **12 fibras por búfer**, cada una con el color de la tabla TIA-598-C
- **60 fibras en total** con posición global secuencial (1–60)

### Fórmula de posición global

```
N = número de fibra global (1–60)

Búfer    = ⌈N ÷ 12⌉          (división con techo)
Posición = (N - 1) mod 12     (índice 0–11 → nombre del color)
```

**Ejemplos:**
- Fibra 14 → Búfer ⌈14÷12⌉ = **2**, Color (14-1) mod 12 = 1 → **Naranja**
- Fibra 37 → Búfer ⌈37÷12⌉ = **4**, Color (37-1) mod 12 = 0 → **Azul**
- Fibra 60 → Búfer ⌈60÷12⌉ = **5**, Color (60-1) mod 12 = 11 → **Aguamarina**

---

## Flujo de la práctica (6 pasos)

```
Inicio
  │
  ▼
[Paso 1] Exploración automática
  │  El sistema ilumina cada búfer (1→5) con highlight pulsante
  │  5 segundos por búfer — el estudiante solo observa
  │
  ▼
[Paso 2] Identificación de búferes
  │  El sistema pide identificar búferes en orden aleatorio
  │  El estudiante mira el búfer correcto y abre el menú (botón Interactuar)
  │  5 selecciones en total (una por búfer)
  │
  ▼
[Paso 3] Introducción a colores (con tabla visible)
  │  El sistema pide seleccionar las 12 fibras del Búfer 1 en orden
  │  La tabla de colores TIA-598-C está visible como referencia
  │  12 selecciones en orden 1→12
  │
  ▼
[Paso 4] Práctica guiada con tabla
  │  4 preguntas aleatorias: "¿Cuál es la fibra X del búfer Y?"
  │  La tabla de colores sigue visible
  │  Scoring: correctPhase4 / 4
  │
  ▼
[Paso 5] Prueba de memoria sin tabla
  │  5 preguntas aleatorias sin tabla de referencia
  │  El estudiante debe recordar los colores
  │  Scoring: correctPhase5 / 5
  │
  ▼
[Paso 6] Cálculo de posición global
  │  4 preguntas: dado el número global N, ¿a qué búfer y color pertenece?
  │  Se abre un panel de 2 fases: primero elige el búfer, luego el color
  │  Scoring: correctPhase6 / 4
  │
  ▼
Checkout (el jugador se dirige al punto de salida)
  │  Se detecta por proximidad (Update polling)
  ▼
Carga siguiente escena
```

**Puntuación total posible:** 13 respuestas correctas (4 + 5 + 4)

---

## Scripts — Mapa de responsabilidades

### Scripts propios de Práctica 1

| Script | Ubicación | Responsabilidad |
|--------|-----------|-----------------|
| `P1_Instrucciones.cs` | `Scripts/InstructionManagers/` | **Orquestador principal.** Controla el flujo completo de los 6 pasos, mantiene el estado, recibe callbacks de los menús y actualiza textos de instrucción/feedback |
| `P1_BufferMenu.cs` | `Scripts/InstructionManagers/` | Menú UI flotante con 12 botones de color + cancelar. Se posiciona frente al jugador, gestiona navegación con joystick y llama a `OnFiberSelected` |
| `P1_GlobalCalcPanel.cs` | `Scripts/InstructionManagers/` | Panel de cálculo global en 2 fases: primero 5 botones (búfer), luego 12 botones (color). Llama a `OnGlobalAnswerSubmitted` |
| `P1_BufferIdentity.cs` | `Scripts/Practica1/` | Componente marcador. Se asigna a cada búfer 3D en escena con su índice (1–5). Lo detecta `HandInteraction` al apuntar con la mirada |

### Scripts de soporte utilizados por Práctica 1

| Script | Responsabilidad dentro de P1 |
|--------|------------------------------|
| `HandInteraction.cs` | Detecta con raycast si el jugador mira un búfer (`P1_BufferIdentity`), muestra reticle magenta y abre `P1_BufferMenu` al presionar el botón de interacción |
| `MotionObjectController.cs` | Movimiento del jugador. Lo deshabilitan `P1_BufferMenu` y `P1_GlobalCalcPanel` mientras el menú está abierto para evitar que el jugador se mueva accidentalmente |
| `Iluminador3D.cs` | Efecto de highlight pulsante en los búferes 3D. Usado por `P1_BufferMenu.SetHighlight()` en el Paso 1 y en el feedback visual |
| `EnfoqueInterfaces.cs` | Modo enfoque (dimmer de mundo + UI de instrumento). No es central en P1 pero comparte el sistema de deshabilitación de movimiento |

---

## Detalle de cada script

### `P1_Instrucciones.cs` — Orquestador

**Variables de estado:**
```csharp
int currentStep;                  // Paso actual (0–6)
bool waitingForFiberSelection;    // Esperando que el jugador elija una fibra
bool waitingForBufferSelection;   // Esperando que el jugador elija un búfer
bool waitingForCalcAnswer;        // Esperando respuesta del panel de cálculo global
int targetBuffer;                 // Búfer objetivo de la pregunta actual (1–5)
int targetFiber;                  // Fibra objetivo de la pregunta actual (1–12)
int correctPhase4, 5, 6;          // Contadores de aciertos por fase
bool checkoutReached;             // Detección de llegada al punto de salida
```

**Callbacks públicos:**
```csharp
// Llamado por P1_BufferMenu cuando el jugador selecciona una fibra
void OnFiberSelected(int bufferIndex, int fiberIndex)

// Llamado por P1_GlobalCalcPanel cuando el jugador responde el cálculo global
void OnGlobalAnswerSubmitted(int bufferAnswer, int fiberAnswer)
```

**Referencias en Inspector:**
- `instructionText` — TextMeshProUGUI para instrucciones
- `feedbackText` — TextMeshProUGUI para feedback (verde/naranja)
- `playerTransform` — Transform del jugador (para posicionar menús)
- `checkoutPoint` — Transform del punto de salida
- `panelCalculo` — Referencia a `P1_GlobalCalcPanel`

---

### `P1_BufferMenu.cs` — Menú de selección de fibras

**Comportamiento:**
1. Se posiciona **0.2m frente al jugador** a la altura de los ojos (+0.05m)
2. Rota para mirar siempre al jugador
3. Crea dinámicamente **12 botones de color + 1 botón Cancelar**
4. Deshabilita `MotionObjectController` mientras está abierto
5. Al seleccionar: llama `P1_InstructionManager.Instance.OnFiberSelected(búfer, fibra)`
6. Al cancelar / cerrar: re-habilita el movimiento

**Navegación con joystick:**
- Eje `Vertical` → mover selección arriba/abajo (con wraparound)
- `Fire1` → confirmar selección
- `Fire2` → cancelar y cerrar menú
- Cooldown entre inputs: 0.25s
- Dead zone: 0.5

**Métodos públicos:**
```csharp
void Show(int bufferIndex)            // Abrir menú para un búfer específico
void SetInteractable(bool active)     // Habilitar/deshabilitar apertura del menú
void SetHighlight(int bufferIndex, bool on)  // Iluminar/apagar búfer 3D en escena
void Close()                          // Cerrar y limpiar
```

---

### `P1_GlobalCalcPanel.cs` — Panel de cálculo global

**Flujo interno de 2 fases:**
```
Show(globalN, correctBuffer, correctFiber)
  │
  ▼ Fase 1
5 botones: "Cinta Milar 1" … "Cinta Milar 5"
El jugador elige el búfer → OnBufferChosen(idx)
  │
  ▼ Fase 2
12 botones de color (Azul … Aguamarina)
El jugador elige el color → OnColorChosen(idx)
  │
  ▼
Callback: P1_InstructionManager.OnGlobalAnswerSubmitted(búfer, color)
```

**Hint de cálculo mostrado al jugador:**
```
Búfer    = ⌈N ÷ 12⌉
Posición = N mod 12
```

---

### `P1_BufferIdentity.cs` — Marcador de búfer

```csharp
public class P1_BufferIdentity : MonoBehaviour
{
    public int bufferIndex;  // Asignar en Inspector: 1, 2, 3, 4 o 5
}
```

Se adjunta a cada uno de los 5 GameObjects de búfer en la escena.  
`HandInteraction` lo detecta con raycast para saber qué búfer está mirando el jugador.

---

### `HandInteraction.cs` — Integración con P1

Fragmento clave de integración con Práctica 1:

```csharp
// En Update() → UpdateReticle()
if (currentGazedBuffer != null &&
    P1_BufferMenu.Instance != null &&
    P1_BufferMenu.Instance.enabled)
{
    // Cambia reticle a magenta para indicar búfer interactuable
    SetReticleColor(interactColor);
}

// Al presionar el botón de interacción (Jump/BotonInteractuar)
if (currentGazedBuffer != null)
    P1_BufferMenu.Instance.Show(currentGazedBuffer.bufferIndex);
```

**Estados del reticle en P1:**
| Color | Significado |
|-------|-------------|
| Blanco | Sin objeto relevante |
| Magenta | Mirando un búfer interactuable |
| Verde | Objeto agarrable |

---

## Diagrama de comunicación entre scripts

```
HandInteraction
    │  (raycast detecta P1_BufferIdentity)
    │  (botón interactuar)
    ▼
P1_BufferMenu.Show(bufferIndex)
    │  (jugador selecciona fibra con joystick + Fire1)
    ▼
P1_InstructionManager.OnFiberSelected(buffer, fiber)
    │  (evalúa respuesta, actualiza step)
    ▼
[Si Paso 6] P1_GlobalCalcPanel.Show(N, correctBuf, correctFib)
    │  (jugador elige búfer + color)
    ▼
P1_InstructionManager.OnGlobalAnswerSubmitted(buffer, fiber)
    │  (evalúa, avanza o termina)
    ▼
Update() detecta checkoutPoint → carga siguiente escena
```

---

## Arquitectura de escena compartida

**`Practica3.unity` es la escena única que sirve para TODAS las prácticas**, no solo para la Práctica 3. La escena es agnóstica a qué práctica se está ejecutando — el `InstructionManager` activo determina el flujo completo.

### Estrategia: una escena, múltiples managers

La escena contiene todos los assets físicos y de UI de todas las prácticas simultáneamente. Los objetos específicos de cada práctica arrancan **desactivados** y se habilitan dinámicamente por el manager correspondiente.

### GameObjects desactivados por defecto (m_IsActive: 0)

Estos pertenecen a Práctica 3 y permanecen ocultos durante Práctica 1:

| GameObject | Práctica | Propósito |
|------------|----------|-----------|
| `BERTester` | P3 | Equipo de medición BER |
| `Atenuador` | P3 | Atenuador óptico |
| `Fibra-8hilos` | P3 | Cable de 8 hilos para P3 |
| `OTDR` | P3 | Equipo OTDR avanzado |
| `PanelBERT`, `PanelTrace`, `PanelHome`… | P3 | Paneles de UI del BER Tester |
| `PauseMenuPanel` | Global | Menú de pausa (se activa al pausar) |

### GameObjects activados por defecto (m_IsActive: 1)

| GameObject | Práctica | Propósito |
|------------|----------|-----------|
| `Practica1` (contenedor raíz) | P1 | Contiene los 5 búferes con sus 12 fibras cada uno (`Fibra-12hilos`) |
| `Player (Camara)` | Global | Cámara del jugador |
| `RealPlayer (Movimiento)` | Global | CharacterController + movimiento |
| `Instrucciones` | Global | Panel de texto instrucciones/feedback |

### Selección de práctica

No hay un selector explícito dentro de la escena. La práctica que se ejecuta depende de **qué `InstructionManager` está habilitado** en la Hierarchy:

| Manager | Script | Controla |
|---------|--------|----------|
| Objeto con `P1_InstructionManager` | `P1_Instrucciones.cs` | Práctica 1 — Identificación de fibras |
| Objeto con `InstructionManagerPrac3` | `InstructionManager_P3.cs` | Práctica 3 — Calidad de enlace óptico / BER |

Para cambiar de práctica basta con **activar/desactivar el GameObject** del manager correspondiente en la Hierarchy.

### Flujo completo de carga

```
MenuPrincipal (escena 2D)
  │  Botón "Modo VR"     → GameModeManager.IsVRMode = true
  │  Botón "Modo Normal" → GameModeManager.IsVRMode = false
  ▼
SceneManager.LoadScene("Practica3")
  │
  ▼
GameModeInitializer.Start()
  ├─ VR Mode:     activa XR subsystems, desactiva CameraLookController
  └─ Normal Mode: desactiva XR subsystems, activa CameraLookController (joystick derecho)
  │
  ▼
InstructionManager activo ejecuta su flujo (P1 o P3)
  │
  ▼
Al llegar al checkoutPoint:
  ├─ P1 → SceneManager.LoadScene("Cuestionario_P1")
  └─ P3 → SceneManager.LoadScene("Cuestionario_P3")
```

### Descripción de Práctica 3 (referencia)

`InstructionManager_P3.cs` controla **19 pasos** divididos en 6 fases:

| Fase | Pasos | Descripción |
|------|-------|-------------|
| 1 | 1–4 | Reconocimiento del entorno — highlights del BERT, Atenuador y cables |
| 2 | 5–7 | Escenario desconectado — Quick Test y BER Test sin conexiones (espera "No Link") |
| 3 | 8–9 | Conexión de cables TX→Atenuador→RX con patchcords |
| 4 | 10–13 | Escenario nominal — atenuación baja (−5 dB), pruebas en PASS (BER < 10⁻¹²) |
| 5 | 14–17 | Escenario degradado — atenuación alta (−25 dB), pruebas en FAIL (BER elevado) |
| 6 | 18–19 | Checkout → carga `Cuestionario_P3` |

---

## Estado actual de la práctica

| Elemento | Estado |
|----------|--------|
| Scripts de lógica (P1_Instrucciones, P1_BufferMenu, P1_GlobalCalcPanel) | ✅ Completos |
| Marcador de búfer (P1_BufferIdentity) | ✅ Completo |
| Integración con sistema de movimiento | ✅ Completa |
| Integración con HandInteraction (raycast) | ✅ Completa |
| Sistema de highlight de búferes | ✅ Completo |
| Escena compartida (`Practica3.unity`) | ✅ Existe y funciona para P1 y P3 |
| Contenedor `Practica1` con búferes 3D | ✅ En escena, activo por defecto |
| `InstructionManager` de P1 en escena | ✅ Presente (habilitar su GameObject para activar P1) |
| Selección de práctica activa | ⚠️ Manual — activar/desactivar el manager en Hierarchy |

---

## Notas de implementación

- Los menús (`P1_BufferMenu`, `P1_GlobalCalcPanel`) **deshabilitan el movimiento** del jugador automáticamente al abrirse y lo restauran al cerrarse.
- El feedback visual dura **2.5 segundos** antes de avanzar al siguiente paso.
- Las preguntas de las fases 4, 5 y 6 son **aleatorias** (no repiten la misma pregunta consecutiva).
- El sistema de highlight usa **emisión pulsante** (seno) en los materiales de los búferes — requiere que los materiales tengan habilitado `Emission` en el shader.
- La detección de llegada al `checkoutPoint` se hace por **distancia en Update()**, no por trigger collider.
