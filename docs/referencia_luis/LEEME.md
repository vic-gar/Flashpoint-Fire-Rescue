# Material previo de Luis (referencia, no se ejecuta)

Copia sin cambios de los seis archivos que Luis compartió en el ZIP
"Algoritmos Reto". Se guardan aquí para que quede claro de dónde salen
las ideas de `Server/strategies/` y qué se cambió al integrarlas con el
tablero real de `Server/model/board.py`.

Ninguno de estos archivos forma parte del programa ni de las pruebas del
proyecto. Están fuera de `Server/` a propósito: si estuvieran dentro, un
`pytest` desde esa carpeta recogería `test_algorithms.py` y fallaría.

## Estado de cada archivo

**astar_pathfinding.py** (REUTILIZADO EN PARTE)
A* correcto, siguiendo el notebook de Path Planning del curso. Se conservó
el algoritmo (heapq, costo g, heurística Manhattan, reconstrucción por
padres) en `Server/strategies/astar.py`. No se pudo usar tal cual porque
modela paredes y fuego como tipos de celda, y en Flash Point las paredes y
puertas están en las aristas entre celdas. Detalle a tener en cuenta:
`_reconstruct_path` devuelve `[inicio, fin]` cuando no hay ruta, lo que
esconde el fallo; la versión nueva devuelve `None`.

**prioritization_heuristic.py** (IDEA REUTILIZADA, CÓDIGO REESCRITO)
Se conservó la idea de puntuar cada objetivo con varios factores y
quedarse con el mejor, en `Server/strategies/prioritization.py`. El
original no corre: su función principal se cae con
`TypeError: unhashable type: 'Victim'`. Además medía distancia Manhattan
en vez del costo real de la ruta y no sabía qué objetivos ya tomaron los
demás bomberos.

**best_response_coordination.py** (IDEA REUTILIZADA, CÓDIGO REESCRITO)
Se conservó la idea de que cada bombero elige respondiendo a lo que ya
eligieron los demás, en `Server/strategies/coordination.py`. El original
trabajaba con identificadores abstractos (no con celdas del tablero),
ajustaba la utilidad con un historial de frecuencias que en nuestro
modelo no existe, y recorría los agentes con `range(max_agent_id + 1)`,
lo que asume ids consecutivos desde cero. La versión nueva usa el estado
real de la partida y nada más.

**replanification_detector.py** (SUSTITUIDO)
Detectaba cambios entre dos estados para decidir cuándo replanificar.
Emitía "objetivo alcanzado" con cualquier movimiento y `should_replan`
no miraba la ruta. En la estrategia final no hace falta un detector:
A* se recalcula en cada acción sobre el tablero tal como está, y lo que
se conserva es el contador `model.replanifications`, que cuenta los
cambios de objetivo.

**run_simulations.py** (NO USAR)
NO USAR PARA MÉTRICAS DEL PROYECTO: este script contiene resultados
generados artificialmente y se conserva únicamente como material previo
del equipo. Las cifras salen de `random.randint()` con rangos fijos, no de
partidas simuladas, y además no importa (referencia un paquete
`flashpoint_algorithms` que no existe). Toda métrica del proyecto sale de
`Server/run_batch.py`.

**test_algorithms.py** (NO FORMA PARTE DE LA SUITE)
16 pruebas escritas para los archivos originales. Al correrlas contra
estos mismos archivos dan 2 fallos y 2 errores (el `TypeError` de arriba,
un umbral de rendimiento y el `should_replan`). Las pruebas vigentes del
proyecto son las de `Server/tests/`.

## Cómo se citan desde el código

Los módulos `astar.py`, `prioritization.py` y `coordination.py` de
`Server/strategies/` traen al inicio una nota `ARCHIVO DE REFERENCIA:`
que apunta al archivo de esta carpeta del que parten.
