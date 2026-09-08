# Declaración de uso de IA generativa (borrador para el README y el reporte)

Borrador preparado el 7 de septiembre de 2026. El equipo debe revisarlo,
completar lo marcado como PENDIENTE DE CONFIRMAR y decidir la redacción
final antes de entregarlo. No exagera ni oculta: describe lo que consta en
el repositorio y en las sesiones de trabajo.

## Herramienta

Se usó Claude (Anthropic) en sesiones de trabajo dirigidas por Carlos,
con acceso a la carpeta local del repositorio y al repositorio oficial
`tc2008b` como referencia. Lo que la IA produce se revisa antes de darlo
por bueno. Estado real al 7 de septiembre: Carlos validó a mano la
conexión Python-Unity (Fase 4); el motor de reglas, la estrategia
mejorada, las pruebas y los experimentos se corrieron en el entorno de
Claude y están PENDIENTES de que el equipo los corra en su máquina. La
documentación técnica principal está dentro del código (.py y .cs), no
en este archivo.

## Qué se desarrolló con ayuda de IA

Se lista por componente. "Con IA" quiere decir que el código o texto se
desarrolló con apoyo de la herramienta a partir de indicaciones del
equipo. Su validación manual por el equipo se documenta únicamente
cuando realmente se haya realizado.

- Motor de reglas en Mesa (`Server/model/`, `Server/agents/`): con IA,
  sobre la base del modelo y el tablero que el equipo ya tenía en el
  repositorio. Las reglas se tomaron del reglamento Family de Flash Point
  y del documento del reto. PENDIENTE DE CONFIRMAR: qué partes del modelo
  base escribió Víctor a mano, para citarlas como tal.
- Pruebas (`Server/tests/`, 109 pruebas: 38 de reglas, 29 del servidor,
  27 de la estrategia y 15 del RNG): con IA. Los casos se eligieron a
  partir de errores encontrados al correr el modelo (por ejemplo, el daño
  que rebasaba los 24 marcadores y la regla de no terminar el turno sobre
  fuego). Las 109 se ejecutaron en el entorno de Claude; Carlos aún debe
  verificarlas en su PC.
- Estrategia mejorada (`Server/strategies/`): con IA, a partir de los
  algoritmos que Luis escribió primero (A*, priorización, mejor respuesta,
  replaneación). Sus archivos originales están en `docs/referencia_luis/`
  con una nota de qué se conservó y qué se reescribió y por qué.
- Servidor HTTP (`Server/server.py`): con IA, siguiendo la plantilla
  oficial del curso (`tc2008b/template-project/tc2008B_server.py`).
- Cliente de Unity (`SimulationClient.cs`, `SimulationState.cs`) y la
  actualización de la escena a partir del estado del servidor
  (`BoardManager.ApplyState`): con IA. El tablero, los prefabs y la
  lectura de `final.txt` en Unity son trabajo previo del equipo en el
  repositorio. PENDIENTE DE CONFIRMAR con Víctor la atribución exacta.
- Experimentos (`Server/run_batch.py`) y análisis de resultados: el script
  con IA; las partidas se corrieron de verdad y los CSV son la salida
  directa del script.
- Documentación (`ESTADO_ACTUAL.md`, `Server/README.md`, este archivo,
  comentarios en el código): con IA, revisada por el equipo.

## Qué hizo el equipo

- Decidir qué se construía, en qué orden y qué se dejaba fuera.
- Escribir la primera versión de los algoritmos de la estrategia (Luis)
  y del tablero en Unity (Víctor).
- Correr el proyecto en su máquina, validar Python con Unity y reportar
  lo que fallaba (Carlos), incluido el incidente con la escena de Unity y
  su restauración.
- Revisar y aceptar o rechazar cada cambio propuesto por la IA, y fijar
  las reglas de trabajo (sin commits ni push automáticos, sin borrar
  trabajo de otros sin revisarlo, sin resultados inventados).
- Detectar que las decisiones al azar y los eventos del juego compartían
  un solo generador aleatorio, lo que hacía injusta la comparación
  pareada, y pedir que se corrigiera antes de usar los resultados.
- Interpretar los resultados y redactar las conclusiones del reporte.
  PENDIENTE: esto todavía no se ha hecho.

## Qué NO se hizo con IA

- Ninguna cifra del reporte fue escrita ni estimada por la IA. Todas
  salen de `python run_batch.py 100 comparar` y se pueden regenerar.
- No se usa ningún resultado generado artificialmente. El script previo
  que sí generaba números al azar se conserva como material histórico y
  está marcado para no usarse.
- Las decisiones de diseño (por ejemplo, colapso a 24 marcadores, o
  apagar el fuego adyacente antes de ir por una víctima) se tomaron
  viendo datos de partidas reales, y quedan explicadas en el código.

## Texto corto propuesto para el README

Los dos textos siguientes describen el estado esperado al entregar.
Solo se pueden usar cuando el equipo de verdad haya corrido las pruebas
y los experimentos en su máquina; hoy eso sigue pendiente.

> Este proyecto se desarrolló con apoyo de Claude (Anthropic) como
> herramienta de programación y redacción. El equipo definió el diseño,
> escribió las primeras versiones de los algoritmos y del tablero en
> Unity, revisó y validó todo el código generado, y corrió los
> experimentos cuyos resultados se reportan. Ninguna métrica fue
> producida por la IA. El detalle está en `docs/USO_DE_IA.md`.

## Texto propuesto para el reporte

> Uso de IA generativa. Durante el desarrollo se utilizó Claude como
> asistente para escribir código, pruebas y documentación a partir de las
> especificaciones del equipo. El motor de reglas, el servidor HTTP, el
> cliente de Unity y la estrategia mejorada se escribieron en sesiones
> asistidas, partiendo del trabajo previo del equipo (modelo base y
> tablero en Unity, y los algoritmos iniciales de la estrategia). El
> equipo revisó cada componente, lo ejecutó en su entorno y validó la
> integración entre Python y Unity. Los resultados experimentales
> provienen de partidas simuladas reales ejecutadas con el script del
> repositorio y pueden reproducirse con las mismas semillas. Las
> decisiones de diseño, la interpretación de los resultados y las
> conclusiones son responsabilidad del equipo.

## Recordatorio

Si la materia o el Tec piden un formato específico de declaración
(por ejemplo, un apartado obligatorio o una cita concreta), hay que
ajustar el texto a ese formato. PENDIENTE DE CONFIRMAR con el profesor.
