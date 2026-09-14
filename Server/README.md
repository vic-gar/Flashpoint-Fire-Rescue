# Server: simulación de Flash Point en Mesa

Aquí vive todo el lado Python del proyecto: las reglas del juego, los
agentes bombero, las estrategias, el servidor HTTP que usa Unity y las
pruebas. Se corre siempre desde esta carpeta (`cd Server`).

Requisitos: Mesa 3.5.1, que es la versión que fija `requirements.txt` y
la que pide el notebook del curso. Mesa 3.5.1 exige **Python 3.12 o
superior** (pip rechaza la instalación en 3.11). Probado con Python 3.14
y con Python 3.13. Las 150 pruebas pasan en los dos, y
`run_batch.py 100 comparar` da los mismos resultados lógicos en ambos
(0 / 38 / 44).

Instalación:

    pip install -r requirements.txt

En Windows la suite tarda varios minutos y en Linux unos segundos. La
diferencia está en las pruebas del servidor, que abren conexiones HTTP a
"localhost"; no afecta a ningún resultado.

## Mapa de archivos

    main.py                  una partida completa en consola, turno por turno
    server.py                servidor HTTP para Unity (puerto 3000)
    run_batch.py             partidas en lote y CSV con métricas reales
    requirements.txt         dependencias de Python
    data/final.txt           tablero oficial del reto (6x8, paredes, POI, fuego, puertas, salidas)

    model/board.py           tablero: celdas, paredes, puertas, salidas, fuego, humo, POI, daño
    model/fire_phase.py      fase del fuego: dados, humo, explosiones, ondas de choque, flashover
    model/flashpoint_model.py  modelo Mesa: turnos, condiciones de fin, get_state(), get_board_config()
    agents/firefighter_agent.py  bombero: catálogo de acciones legales y su ejecución

    strategies/__init__.py       registro por nombre: aleatoria, mejorada, mejorada_sin_coordinacion
    strategies/random_strategy.py  solución aleatoria (criterio 1)
    strategies/astar.py            A* sobre el tablero real
    strategies/prioritization.py   puntuación y elección de objetivo
    strategies/coordination.py     reparto de objetivos entre bomberos
    strategies/improved_strategy.py  estrategia mejorada (criterio 2)

    tests/test_rules.py      38 pruebas de reglas
    tests/test_server.py     29 pruebas del servidor y del contrato JSON con Unity
    tests/test_strategy.py   27 pruebas de A*, priorización, coordinación y estrategia
    tests/test_rng.py        15 pruebas de la separación de generadores aleatorios
    tests/test_multigrid.py  41 pruebas del espacio MultiGrid de Mesa


## El espacio: MultiGrid y Board conviven

El modelo tiene dos cosas que suenan parecidas y hacen trabajos
distintos. Conviene no confundirlas:

    model.grid    MultiGrid de Mesa. Solo posición de los bomberos.
    model.board   Board propio. Paredes, puertas, salidas, fuego, humo,
                  POI y daño estructural.

`MultiGrid(width=8, height=6, torus=False)`. Se eligió MultiGrid y no
SingleGrid porque el reto pide que "puede haber más de un agente por
celda", el mismo criterio que usa el profesor en el notebook MoneyModel
para elegir esta clase.

Dos detalles que cuestan caro si se olvidan:

- **Mesa ordena las coordenadas como `(x, y)` = `(columna, fila)`**, al
  revés que el resto del proyecto. `FirefighterAgent.row` y `.column`
  son propiedades que leen `self.pos` y hacen la traducción, así que la
  posición tiene una sola fuente de verdad y no puede desincronizarse.
  Para mover a un bombero sin gastar AP se usa `set_cell(row, column)`.
- **Mesa devuelve la vecindad ordenada por `(x, y)`**, que en nuestras
  coordenadas es izquierda, arriba, abajo, derecha. El proyecto la
  recorre como arriba, abajo, izquierda, derecha, y ese orden llega
  hasta el catálogo de acciones legales y de ahí a lo que elige la
  estrategia aleatoria. `FlashPointModel.MOVEMENT_DIRECTIONS` fija el
  orden del proyecto. Cambiarlo altera los resultados de los
  experimentos sin cambiar ninguna regla.

`FlashPointModel.movement_neighbors(row, column)` junta las dos mitades:
pide la vecindad al grid con `moore=False` (sin diagonales) y filtra con
`board.can_move_between` (paredes y puertas). `moore=False` sale del
reglamento del juego, que dice que las celdas adyacentes son las de
arriba, abajo, izquierda y derecha y que las diagonales no son
adyacentes.

## Aleatoriedad: tres generadores

El modelo tiene tres `random.Random` derivados de la misma semilla
(`FlashPointModel._create_random_generators`):

    fire_random      dados del fuego (fila d6, columna d8)
    poi_random       barajado del mazo de POI y dados de reposición
    strategy_random  decisiones al azar de las estrategias

Con la misma semilla, la aleatoria y la mejorada reciben exactamente los
mismos dados del fuego y el mismo mazo, sin importar cuántas decisiones
al azar tome cada una. Es lo que hace justa la comparación pareada de
`run_batch.py`. El `self.random` de Mesa no se usa; `tests/test_rng.py`
verifica que no se toque en toda una partida. Cualquier aleatoriedad
nueva debe usar uno de los tres generadores, nunca `random.random()`
directo.

## Cómo funciona una estrategia

Una estrategia es una función `decide(model, firefighter)` que devuelve
una acción del catálogo `firefighter.get_legal_actions()` o `None` para
terminar el turno guardando AP. El modelo la llama una y otra vez
durante el turno del bombero. Como solo puede elegir entre acciones
legales, ninguna estrategia puede romper las reglas.

Se elige por nombre en los tres puntos de entrada:

    python main.py 42 aleatoria
    python server.py 3000 aleatoria
    python run_batch.py 50 mejorada

Y desde Unity, en el inspector de `SimulationClient` (campo Strategy),
que la manda al servidor en el `POST /reset`.

## Comandos

    python -m unittest discover -s tests -v      las 150 pruebas
    python main.py 42                            una partida, semilla 42, mejorada
    python main.py 42 aleatoria mudo             sin detalle turno por turno
    python server.py                             servidor en http://localhost:3000
    python run_batch.py 100 comparar             las 3 estrategias, semillas 0 a 99

`comparar` deja cuatro CSV en esta carpeta: uno por estrategia con una
fila por partida y `resultados_comparacion.csv` con los promedios. Los
cuatro están versionados en el repositorio, porque son la evidencia del
criterio de rendimiento. Volver a correr `comparar` los reemplaza.

## Servidor

    GET  /         información y estrategias disponibles
    GET  /board    geometría fija: 48 celdas con paredes, 8 puertas, 4 salidas
    GET  /state    estado actual de la partida
    POST /step     avanza un turno y devuelve el estado
    POST /reset    reinicia; cuerpo opcional {"semilla": 7, "estrategia": "aleatoria"}

Prueba rápida sin Unity, con el servidor corriendo en otra terminal:

    curl http://localhost:3000/state
    curl -X POST http://localhost:3000/step
    curl -X POST http://localhost:3000/reset -H "Content-Type: application/json" -d "{\"estrategia\":\"aleatoria\",\"semilla\":7}"

## COMPATIBILIDAD CON UNITY

- El puerto por defecto es 3000 en `server.py` y en
  `Assets/Scripts/Framework/SimulationClient.cs`. El valor que manda en
  Unity es el del inspector; si se cambia uno hay que cambiar el otro.
- Los nombres de las llaves del JSON de `get_state()` y
  `get_board_config()` tienen que coincidir letra por letra con los
  campos de `Assets/Scripts/Data/SimulationState.cs`, porque
  `JsonUtility` no avisa cuando un nombre no coincide. La prueba
  `test_campos_del_json_coinciden_con_las_clases_de_csharp` en
  `tests/test_server.py` lee el archivo de C# y lo compara. Si se
  agrega un campo al JSON hay que agregarlo también en C#.
- Unity ignora las llaves que no conoce, así que agregar campos al
  JSON no rompe nada; quitarlos o renombrarlos sí.

## Consideraciones importantes

Estas cinco cosas están acopladas con los resultados de los
experimentos. Si se cambia alguna, hay que volver a correr
`run_batch.py 100 comparar` y comprobar que los números siguen saliendo.

- `strategies/random_strategy.py` es la línea base contra la que se
  compara todo lo demás.
- `data/final.txt` es el tablero oficial. El parser de `board.py` asume
  el orden y las cantidades del archivo: 3 POI, 10 fuegos, 8 puertas y
  4 salidas.
- `MAX_DAMAGE = 24` en `flashpoint_model.py`. El reto dice 24 marcadores
  en un lugar y "25 o más" en otro; se usa 24, como el reglamento
  oficial.
- Los pesos de `prioritization.py` y `coordination.py`. Se probaron
  variantes y las ganancias no se sostenían entre bloques de semillas.
- `FlashPointModel.MOVEMENT_DIRECTIONS` fija el orden en que se listan
  los vecinos, y con él el orden del catálogo de acciones legales.
  Cambiarlo mueve los resultados sin tocar ninguna regla.
