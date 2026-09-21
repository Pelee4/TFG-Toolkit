# Mi juego

> **Versión:** 0.1  
> **Autor:**   
> **Fecha:** 22/06/2026  

## Índice

1. Mecánicas
2. Personajes
3. Narrativa y diálogos
4. Guía de estilo visual
5. Relaciones entre sistemas
6. Notas de diseño

---

## Mecánicas

### Correr

**Estado:** 🔧 En desarrollo  

**Descripción:** El personaje corre  

| Campo | Valor |
|-------|-------|
| Input | Shift |
| Efecto | x1,3 velocidad |
| Duración | Instantáneo / Permanente |

---

### Dash

**Estado:** 🔧 En desarrollo  

**Descripción:** El jugador utiliza un dash hacia delante al pulsar Espacio	  

| Campo | Valor |
|-------|-------|
| Input | Space |
| Efecto | x2 velocidad |
| Duración | 0,5s |

**Script generado:**

```csharp
```csharp
using UnityEngine;

public class Dash : MonoBehaviour
{
    public float dashSpeed = 5f; // Velocidad del dash
    private Rigidbody rb;
    private bool isDashing = false;

    void Start()
    {
        rb = GetComponent\u003cRigidbody\u003e();
    }

    void Update()
    {
        // Verifica si se ha pulsado la tecla de espacio para iniciar el dash
        if (Input.GetKeyDown(KeyCode.Space) \u0026\u0026 !isDashing)
        {
            StartCoroutine(DashCoroutine());
        }
    }

    IEnumerator DashCoroutine()
    {
        isDashing = true;
        rb.velocity = transform.forward * dashSpeed; // Aplica velocidad al personaje en la dirección del frente

        yield return new WaitForSeconds(0.5f); // Espera durante 0.5 segundos

        rb.velocity = Vector3.zero; // Detiene el movimiento
        isDashing = false;
    }
}
```

Este script implementa la mecánica de dash para un personaje en Unity utilizando C#. Cuando se presiona la tecla de espacio, el jugador realiza un dash hacia delante durante 0.5 segundos con una velocidad doble.
```

---

### Parry

**Estado:** 💡 Idea  

**Descripción:** El jugador bloquea en el momento exacto del impacto y reduce todo el daño  

| Campo | Valor |
|-------|-------|
| Input | Click derecho / L2 |
| Efecto | Bloquear el daño al completo, y aturde al enemigo 1s |
| Duración | 0,3s |

---

## Personajes

### Caballero Oscuro

**Rol:** 👑 Jefe  

**Descripción:** Enemigo inicial del juego, con una capa negra con capucha, sin dejar ver su rostro.  

**Historia:** No tiene  


**Estadísticas base:**

| Stat | Valor |
|------|-------|
| Vida | 50 |
| Ataque | 20 |
| Defensa | 5 |
| Velocidad | 2 |

**Habilidades:** Golpe melee sencillo  

**Mecánicas asociadas:**

- Correr

---

### Josemi

**Rol:** 🦸 Protagonista  

**Descripción:** Es un hombre de estatura media, con poco pelo, gafas, y fuerte  

**Historia:** Fue un hombre que peleo en la primera guerra mundial, dejandole secuelas de por vida  

**Motivación:** Acabar con las guerras en el mundo  

**Estadísticas base:**

| Stat | Valor |
|------|-------|
| Vida | 100 |
| Ataque | 10 |
| Defensa | 10 |
| Velocidad | 20 |

**Habilidades:** Golpea con su espada de multiples formas.  

**Mecánicas asociadas:**

- Correr
- Dash

---

## Narrativa y diálogos

### Nuevo árbol

**Estructura:** 8 nodos, 7 conexiones  

**Líneas de diálogo:**

> **Caballero Oscuro:** "Hola"

> **Caballero Oscuro:** "Chao"

**Puntos de elección:**

- Hola
- Adios

---

## Guía de estilo visual

**Estilo artístico:** PixelArt  

Describe la dirección artística general del juego.
Ej: 'Juego 2D pixel art con una paleta cálida inspirada en 8-bit clásico.'

**Tono y sentimiento:** Describe el sentimiento general que debe transmitir.
Ej: 'Aventura épica, misterio, nostalgia'  

**Inspiraciones:** Menciona referencias artísticas.
Ej: 'Zelda, Super Metroid, Hollow Knight'  

**Paleta de colores:**

| Color | Hex | Uso |
|-------|-----|-----|
| Azul | #2B3AB2 | Botones de confirmacion |

**Tipografías:**

| Nombre | Tamaño | Estilo | Uso |
|--------|--------|--------|-----|
| Legacy | 13pt | Bold | Errores |

**Guía de UI:** Notas sobre botones, iconos, elementos de UI.
Ej: 'Bordes redondeados de 4px, sombras suaves'  

**Restricciones técnicas:** Limitaciones de resolución, paleta limitada, etc.
Ej: 'Paleta de 16 colores máximo, resolución 320x180'  

---

## Relaciones entre sistemas

### Nuevo Grafo

| Mecánica origen | Relación | Mecánica destino |
|-----------------|----------|------------------|
| Correr | Activa | Dash |

**Análisis IA del sistema:**

1. Mecánicas sin ninguna conexión: No hay mecánica huérfana detectada en el grafo proporcionado.
2. Posibles ciclos de dependencia: Existe un ciclo de dependencia entre Correr y Dash, lo cual podría causar problemas si no se maneja correctamente. Por ejemplo, el jugador podría estar corriendo y darse cuenta de que no puede realizar un dash porque está corriendo.
3. Inconsistencias en las relaciones: No hay inconsistencias evidentes en las relaciones entre mecánicas descritas.
4. Sugerencias de nuevas conexiones o mecánicas:
   - Agregar una nueva mecánica como "Salto", que podría activarse mientras corre para añadir velocidad adicional.
   - Crear una relación entre Dash y otro estado como "Dash Parado" para permitir que el jugador dash en lugar de correr si está parado.

Estas sugerencias pueden complementar el diseño, proporcionando más opciones al jugador y mejorando la experiencia de juego.

---

## Notas de diseño pendientes

### [Nota] Equilibrar la ventana de parry según el playtest

*— Agente IA, 22/06/2026 11:43*

### [Nota] Parry

El parry debe sentirse arriesgado pero recompensante.

*— Jorge, 22/06/2026 11:40*

### [Idea] Mecánica Dash

La mecánica Dash permite al jugador impulsar rápidamente en una dirección. La velocidad del dash es x2 la normal y dura 0,5 segundos.

*— Diseñador, 31/05/2026 13:05*

---


*Documento generado automáticamente por TFG Toolkit el 22/06/2026 16:39*
