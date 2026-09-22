# Demo Tutorial — Introducción al Entorno VR
**FiberLabRV · Práctica Demo**

---

## Propósito

Escena corta (~3 min) que se ejecuta **antes de cualquier práctica real**.  
El alumno aprende las 4 mecánicas del visor sin presión de evaluación:

1. El punto de mira (retícula)
2. Seleccionar objetos mirándolos
3. Responder preguntas con botones
4. Desplazarse a puntos del laboratorio

---

## Flujo de pasos

---

### Paso 0 — Bienvenida

**Texto en pantalla:**
> "Bienvenido al laboratorio virtual de fibra óptica.  
> Antes de comenzar, aprenderás a moverte e interactuar dentro del visor.  
> Pulsa el botón del control para continuar."

- Solo texto de instrucción + `WaitForConfirm()` (Fire1)
- Sin objetos en pantalla todavía

---

### Paso 1 — La retícula (punto de mira)

**Texto en pantalla:**
> "El punto blanco en el centro de tu visión es tu retícula.  
> Cuando apuntas a algo con lo que puedes interactuar, **se pone verde**.  
> Mira hacia los objetos que están iluminados."

**En escena:**
- 2–3 cubos flotantes con `Highlightable` prendidos (color amarillo/naranja)
- Cada cubo tiene un `P1_CablePart` o un collider con capa interactuable
- El alumno solo necesita **mirar** cada cubo → retícula se vuelve verde
- No se requiere presionar nada — `WaitUntil` detecta que miró todos

**Qué aprende:** la retícula indica qué puedes tocar.

---

### Paso 2 — Seleccionar un objeto

**Texto en pantalla:**
> "Ahora apunta al objeto iluminado y **presiona el botón del control**  
> cuando el punto esté verde para seleccionarlo."

**En escena:**
- 1 objeto destacado (cubo o esfera con glow)
- Al apuntar: retícula verde
- Al presionar Fire1 mientras apunta: objeto cambia de color / efecto visual
- Confirmación: "¡Correcto! Así seleccionas objetos en el laboratorio."

**Qué aprende:** gaze + Fire1 = selección.

---

### Paso 3 — Responder con botones

**Texto en pantalla:**
> "A veces verás preguntas con botones de respuesta.  
> **Apunta a un botón** hasta que se ponga amarillo, luego **presiona el control**."

**En escena:**
- Panel con 3 botones: "Opción A", "Opción B", "Opción C"
- Solo "Opción A" es correcta (o cualquier — esto es demo, no evalúa)
- Al mirar un botón → se pone amarillo (hover)
- Al presionar Fire1 sobre cualquier botón → desaparece el panel
- Feedback: "¡Perfecto! Así contestas las preguntas del cuestionario."

**Qué aprende:** el sistema de menú con gaze + Fire1 sobre botones.

---

### Paso 4 — Desplazarse al punto marcado

**Texto en pantalla:**
> "Por último, practica moverte. Dirígete al punto marcado con luz en el suelo."

**En escena:**
- Un indicador visual en el suelo (flecha, halo, luz pulsante) a ~3–5 m del inicio
- `WaitUntil` con proximidad al punto (`Vector3.Distance <= 1.5f`)
- Al llegar: indicador se apaga, texto de confirmación

**Qué aprende:** navegación por proximidad (la misma mecánica del Paso 7 de prácticas reales).

---

### Paso 5 — Cierre

**Texto en pantalla:**
> "¡Listo! Ya sabes cómo moverte e interactuar en el laboratorio.  
>
> - Punto verde → puedes interactuar  
> - Fire1 → seleccionar / confirmar  
> - Botón amarillo → estás apuntando a él  
>
> Pulsa el control para comenzar tu práctica."

- Fire1 → `SceneManager.LoadScene("InicioRV")` (o la escena que corresponda)

---

## Mecánicas usadas (resumen técnico)

| Mecánica | Implementación |
|----------|---------------|
| Retícula color | `HandInteraction.ReticleOverride` + `hovered.SetHovered()` |
| Gaze sobre objeto | `Physics.Raycast` desde cámara |
| Selección Fire1 | `Input.GetButtonDown("Fire1")` |
| Botones con hover | `WaitForQuizSelection` (mismo que P1 Pasos 5/6) |
| Proximidad a punto | `WaitUntil` + `Vector3.Distance` |
| Avanzar entre pasos | `WaitForConfirm()` (Fire1 genérico) |

---

## Estructura de la escena sugerida

```
DemoTutorial (scene)
├── EventSystem
├── SceneGuard
├── Player (Camara)
│   └── HandInteraction
├── RealPlayer (Movimiento)
├── Canvas_Instrucciones        ← World Space, igual que PracticasRV
│   ├── TextInstrucciones
│   └── PanelRespuestas (botones Paso 3)
├── ObjetosPaso1                ← cubos con Highlightable
├── ObjetosPaso2                ← objeto interactuable
├── PuntoMeta (Paso 4)          ← Transform + indicador visual
└── DemoManager                 ← script InstructionManager_Demo.cs
```

---

## Script sugerido: `InstructionManager_Demo.cs`

Usar la misma arquitectura de `P1_InstructionManager`:

```csharp
public class InstructionManager_Demo : MonoBehaviour
{
    // Referencias
    public TextMeshProUGUI instructionText;
    public Highlightable[] objetosPaso1;     // cubos iluminados
    public Highlightable   objetoPaso2;      // objeto seleccionable
    public GameObject      panelRespuestas;  // panel 3 botones
    public Button[]        botones;          // 3 botones
    public Transform       puntoMeta;        // destino Paso 4
    public Transform       playerTransform;
    public float           radioMeta = 1.5f;
    public string          escenaSiguiente = "InicioRV";

    IEnumerator Start() => RunDemo();

    IEnumerator RunDemo()
    {
        yield return StartCoroutine(RunPaso0_Bienvenida());
        yield return StartCoroutine(RunPaso1_Reticula());
        yield return StartCoroutine(RunPaso2_Seleccion());
        yield return StartCoroutine(RunPaso3_Botones());
        yield return StartCoroutine(RunPaso4_Desplazamiento());
        yield return StartCoroutine(RunPaso5_Cierre());
    }
}
```

Cada `RunPasoX_` es un `IEnumerator` igual a los de `P1_InstructionManager`, reutilizando `WaitForConfirm()` y `WaitForQuizSelection()`.

---

## Notas de diseño

- **Sin evaluación**: ninguna respuesta es "incorrecta" aquí. Si el alumno elige mal en el Paso 3, se le dice "cualquier opción funciona — esto es solo práctica."
- **Duración estimada**: 2–4 minutos dependiendo del ritmo del alumno.
- **Reutilización total**: todos los scripts y prefabs ya existen — no se crea nada nuevo, solo una escena nueva con el manager de demo.
- **Posición en el flujo**: después de `InicioRV` (coloca el celular) y antes de `PracticasRV`.
