# TFG Toolkit

Un plugin para el editor de Unity que reúne en un mismo sitio la documentación de diseño, el prototipado y parte de la implementación de un videojuego.

Este repositorio contiene el código del Trabajo de Fin de Grado del Grado en Ingeniería Multimedia de la Universidad de Alicante (Escuela Politécnica Superior), desarrollado por Jorge Pelegrín.

Memoria del TFG: [memoria.pdf](https://github.com/user-attachments/files/32468788/memoria.pdf)

## Por qué existe

Cuando se desarrolla un juego, el diseño suele vivir fuera del motor: las mecánicas en un documento, las tareas en Trello o HacknPlan, los diálogos en Twine, las relaciones entre sistemas en Miro. Mientras tanto, el código y las escenas están en Unity. Cada vez que algo cambia hay que acordarse de actualizarlo en varios sitios, y con el tiempo la documentación deja de reflejar lo que realmente existe.

La idea de este proyecto es llevar esa parte conceptual al propio editor, de forma que la documentación sea parte del proyecto de Unity y esté a un clic de las escenas y los scripts. Todo se guarda como ScriptableObjects, así que los datos son assets normales de Unity que se pueden versionar con Git junto al resto del proyecto.

## Qué incluye

El Toolkit se abre desde una ventana principal que da acceso al resto de herramientas:

- **Fichas de mecánicas y de personajes.** Ventanas de dos paneles (lista y detalle) para documentar cada mecánica y cada personaje como un asset.
- **Notas de diseño.** Registro de ideas y decisiones, con tipo, autor, fecha y estado (pendiente o resuelta). Distingue las notas escritas por una persona de las generadas por el agente de IA.
- **Tablero del proyecto.** Un Kanban con las fichas repartidas por estado (Idea, En desarrollo, Implementada), un dashboard con contadores y progreso, y pestañas para configurar columnas y añadir campos personalizados sin tocar código.
- **Editor de grafos de mecánicas.** Un lienzo de nodos para ver cómo se relacionan las mecánicas entre sí.
- **Editor de diálogos.** Árboles de conversación con cinco tipos de nodo (diálogo, elección, condición, evento y fin de rama), enlazados a las fichas de personajes.
- **Guía de estilo visual.** Paleta de colores, tipografías e inspiraciones del proyecto, en un único asset con vista propia.
- **Biblioteca de prototipos.** Una colección de prefabs base que se pueden arrastrar a la Scene View para montar una escena de prueba rápidamente, y a los que se pueden asociar mecánicas documentadas.
- **Agente de IA.** Un chat integrado en el editor que conoce el estado del proyecto (fichas, notas, diálogos, escena activa) y puede actuar sobre él: crear fichas y notas, cambiar el estado de una mecánica, generar un script a partir de una ficha o manipular la escena.
- **Exportador de GDD.** Genera el Game Design Document a partir de las fichas, en Markdown y/o HTML. También puede pedirle al agente que redacte un GDD con texto enlazado en lugar de un volcado directo.

Cada herramienta funciona de forma independiente: no hace falta usarlas todas ni en un orden concreto.

## Requisitos

- **Unity 6000.3.16f1** (Unity 6). Es la versión con la que se ha desarrollado y probado. Con otras versiones puede funcionar, pero no se ha comprobado; los editores de grafos usan `GraphView`, que es una API experimental y puede cambiar entre versiones.
- **Ollama** (v0.3 o superior) si se quiere usar el agente de IA en local. No es necesario para el resto de herramientas.

El Toolkit no depende de paquetes externos: las llamadas a los modelos y el procesado de sus respuestas se hacen con las APIs nativas de Unity.

## Instalación

Este repositorio no es un proyecto de Unity completo, es únicamente la carpeta del plugin. Eso significa que se instala copiándola dentro de un proyecto que ya tengas.

1. Descarga o clona el repositorio.
2. Abre (o crea) tu proyecto de Unity, con la versión indicada arriba.
3. Copia la carpeta `TFGToolkit` dentro de la carpeta `Assets` de tu proyecto. Copia también los archivos `.meta`, tanto los que están dentro como el que acompaña a la carpeta si existe (`TFGToolkit.meta`).
4. Vuelve a Unity y espera a que termine de importar y compilar los scripts.

El resultado tiene que quedar así:

```
TuProyecto/
└── Assets/
    ├── TFGToolkit/
    │   ├── Core/
    │   ├── Data/
    │   ├── Editor/
    │   ├── GDD/
    │   ├── Scripts/
    │   └── (archivos .meta)
    └── ...
```

Hay tres cosas importantes que conviene respetar:

**No cambies el nombre de la carpeta ni la muevas.** Debe llamarse exactamente `TFGToolkit` y estar directamente dentro de `Assets`. Algunas rutas (por ejemplo, la carpeta de destino por defecto del exportador, `Assets/TFGToolkit/GDD`) parten de esa ubicación.

**No borres los archivos `.meta`.** Unity guarda en ellos el identificador (GUID) de cada archivo, y los assets de datos se enlazan entre sí y con los scripts a través de esos identificadores. Si faltan, Unity genera otros nuevos y las referencias se rompen.

**Mantén el nombre de la carpeta `Editor`.** Unity solo excluye de la build final el código que está en una carpeta con ese nombre. Es lo que hace que las ventanas del Toolkit no se incluyan en tu juego.

Si ya tienes tu propia carpeta `Editor` en la raíz de `Assets`, no hay conflicto: esa y la de `TFGToolkit/Editor` conviven sin problema.

### Comprobar que ha ido bien

Tras la importación, la consola de Unity no debería mostrar errores de compilación. Después, abre la ventana principal del Toolkit desde la barra de menús del editor (`[ruta del menú: rellenar]`) y abre cualquiera de las herramientas. Si se abre, la instalación es correcta.

## Configurar el agente de IA

El agente es opcional. Por defecto usa un modelo local con Ollama, de forma que la documentación del proyecto no sale de tu equipo.

1. Instala Ollama desde [ollama.com](https://ollama.com) y deja el servicio en ejecución.
2. Descarga un modelo Qwen 2.5 Coder. El Toolkit se desarrolló con la variante de 1.5b, pensada para equipos con poca VRAM (con una GTX 1650 de 4 GB), y también admite la de 7b si tu equipo lo permite. Por ejemplo: `ollama pull qwen2.5-coder:1.5b`
3. En Unity, abre la configuración del Toolkit (`ToolkitConfig`) y comprueba que el modelo indicado coincide con el que has descargado.

También existen conectores para Gemini y Anthropic, pensados como alternativa cuando no se dispone de un equipo capaz de ejecutar modelos en local. Están menos probados que el de Ollama. Si los usas, ten en cuenta que en ese caso el contexto del proyecto se envía a un servicio externo, y que la clave de la API se guarda en la configuración del Toolkit: **no subas esa clave a ningún repositorio público.**

Una limitación a tener en cuenta: los modelos pequeños tienen un límite de tokens reducido, así que el contexto que se le da al agente se recorta y el historial de conversación se limita a los últimos 10 mensajes. Con modelos pequeños, sus respuestas son útiles pero limitadas.

## Cómo está organizado

```
TFGToolkit/
├── Core/      ScriptableObjects (mecánicas, personajes, diálogos), modelos de datos
│              y capa de acceso a los modelos de IA
├── Editor/    Todas las ventanas del Toolkit, incluido el chat del agente
├── Data/      Assets generados y usados por las herramientas
├── GDD/       Destino por defecto de los documentos exportados
└── Scripts/   Destino de los scripts generados a partir de las fichas
```

La separación entre `Core` y `Editor` no es un capricho: Unity obliga a que el código del editor esté en una carpeta llamada `Editor` para no compilarlo en la build. Además, las ventanas no se llaman entre sí, sino que leen y escriben sus datos en los assets de `Data/`. Gracias a eso se puede añadir una herramienta nueva sin modificar las que ya existen.

### Decisiones de diseño que merece la pena conocer

- **IMGUI y UIElements a la vez.** Las ventanas de formulario están hechas con IMGUI, y los dos editores de grafos con UIElements y `GraphView`, que no se puede usar con IMGUI. Cuando se llegó a los grafos ya había varias ventanas terminadas, así que se optó por convivir con los dos sistemas en lugar de reescribirlo todo.
- **Sin bloquear el editor.** La primera versión del agente esperaba la respuesta del modelo con un bucle y `Task.Delay`, lo que congelaba Unity mientras llegaba la respuesta. Se sustituyó por una arquitectura basada en callbacks, con gestión de timeout y de errores.
- **Acciones extensibles.** Cada cosa que el agente puede hacer es una clase que implementa la interfaz `IAgentAction` (`ActionName`, `Description` y `Execute`). Añadir una acción nueva consiste en crear la clase y registrarla con una línea; el `ActionDispatcher` se ocupa del resto.

## Estado del proyecto

El Toolkit se desarrolló y validó como parte de un TFG, no como un producto cerrado. Todas las herramientas funcionan y han pasado una batería de pruebas, aunque en algunas pruebas el resultado fue parcial y está documentado en el capítulo de validación de la memoria. Está probado en una única versión de Unity y con modelos locales pequeños.

## Autor

Jorge Pelegrín, Grado en Ingeniería Multimedia, Universidad de Alicante.

## Licencia

`[elegir licencia: por ejemplo MIT, o añadir un archivo LICENSE]`
