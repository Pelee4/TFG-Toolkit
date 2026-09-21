# Mi juego
Una línea de tagline que resuma el concepto central del juego en una frase memorable: "El equilibrio entre vida y muerte en un mundo sin sentido."

## 1. Visión General
- **Pitch:** Un juego de acción exploratorio, ambientado en un universo donde la violencia es la única forma de sobrevivir. El jugador interpreta a un joven explorador que debe superar obstáculos en una flota de ruinas para recuperar el artefacto que devolverá la energía a su aldea.
- **Género y plataforma:** Aventura de acción, desarrollo de videojuegos.
- **Público objetivo:** Personas que disfrutan con un juego de alta intensidad y baja emoción. El público busca una experiencia donde el ataque cuerpo a cuerpo es la única forma de sobrevivir.
- **Pilares de diseño:**
  - La violencia debe ser llena de sensación y emociones.
  - El artefacto central que actúa como el hilo conductor del juego.
  - Los personajes deben contar una historia en su propio derecho.

## 2. Bucle de Juego
El bucle principal es: Explorar (zonas inexploradas) → Combatir (con los enemigos) → Recompensar (recuperar el artefacto) → Mejorar (hacer más fuerte) → Repetir.

Este bucle funciona bien porque proporciona una estructura de juego consistente y permite al jugador experimentar con diferentes formas de mejorar sus habilidades mientras continúa explorando y combatiendo en un entorno cada vez más conocido.

## 3. Mecánicas
| **Mecánica** | **Input** | **Efecto** | **Duración** |
| -------------- | ---------- | ----------- | ------------- |
| Ataque cuerpo a cuerpo | Clic izquierdo | Activa un collider de daño frontal | 0,3s |
| Correr | Shift | x1,3 velocidad | 0s |
| Doble salto | Espacio (en el aire) | Segundo impulso vertical | 0s |
| Parry | Click derecho / L2 | Bloquear el daño al completo y aturde al enemigo por 1 segundo | 0,3s |
| Salto | Espacio | Impulso vertical para superar huecos y obstáculos | 0s |

### Relaciones entre Mecánicas
- **Salto** --[Activa]--\u003e **Doble salto**

## 4. Personajes

- **Guardián de piedra (Enemigo)**
  - Rol narrativo: Guardia del artefacto, impide el acceso a la sala.
  - Trasfondo: Centinela antiguo que custodia la sala del artefacto.
  - Motivación: Impedir el paso a cualquier intruso.
  - Estadísticas: Vida=60 Ataque=20 Defensa=15 Velocidad=3
  - Habilidades: Golpe pesado de área.
  - Mecánicas: Ataque cuerpo a cuerpo.

- **Kai (Protagonista)**
  - Rol narrativo: Explorador, buscando el artefacto.
  - Trasfondo: Recuperar la energía para su aldea.
  - Motivación: Recuperar el artefacto que devolverá la energía a su aldea.
  - Estadísticas: Vida=100 Ataque=15 Defensa=8 Velocidad=6
  - Habilidades: Movimiento ágil, salto preciso y combate cuerpo a cuerpo.
  - Mecánicas: Salto, Ataque cuerpo a cuerpo.

## 5. Narrativa y Diálogos
La estructura narrativa se basa en el recorrido de Kai por la flota de ruinas, intentando recuperar el artefacto que le devolverá energía a su aldea. El protagonista interactuará con varios árboles de diálogo según avance en el juego y tope obstáculos.

## 6. Dirección de Arte
Estilo: Abstract
Dirección artística: Juego 2D pixel art con una paleta cálida inspirada en 8-bit clásico.
Tono: Hola
Colores: Azul, Amarillo, Blanco

## 7. Estado de Desarrollo
- Mecánicas implementadas:
  - Ataque cuerpo a cuerpo
  - Correr
  - Doble salto
- En desarrollo:
  - Parry
- Ideas en proceso de diseño:
  - Nuevo árbol de diálogo
  - Salto

Este guión de diseño es un documento vivo que evoluciona con el proyecto.