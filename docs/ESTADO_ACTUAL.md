# Estado del proyecto AD2026: cierre técnico. Backend y integración con Unity terminados

Documento de continuidad. Se actualiza al cerrar cada sesión de trabajo con Claude.
Última actualización: 8 de septiembre de 2026 (cierre técnico: cámara validada a mano,
commits y subida de la rama `carlos`).

## LEER PRIMERO: dos reglas para todo el equipo

**Regla 1. El backend está congelado.** No modificar sin necesidad real:
`Server/model/`, `Server/agents/`, `Server/strategies/`, los tres generadores aleatorios,
`server.py`, `run_batch.py`, el contrato JSON con Unity y `MOVEMENT_DIRECTIONS`.
Si hay que tocarlo por un bug real, después hay que correr las dos cosas y comprobar que
los números no cambiaron:

    python -m unittest discover -s tests -v   ->  150/150
    python run_batch.py 100 comparar          ->  0 / 38 / 44

Si cualquiera de esos dos números cambia, el cambio rompió algo. Revertirlo antes de seguir.

**Regla 2. Una sola persona edita `MainScene.unity` a la vez.** Unity guarda la escena como
un YAML de 854 documentos y 480 KB. Git no sabe fusionarlo. Si dos ramas la editan en
paralelo, el conflicto se resuelve a mano línea por línea o se pierde trabajo. Mientras
alguien esté en la escena, los demás trabajan en scripts, prefabs y materiales.

**Unity es visualización y cliente, no motor.** La simulación sigue viniendo de Python. No
reemplazar la arquitectura por una simulación hecha dentro de Unity: el criterio 4 de la
rúbrica (15%) es precisamente el sistema cliente-servidor.
El diagnóstico inicial (gap analysis, rúbrica, métricas, assets, plan maestro) vive como
artifact publicado, titulado "Fire Rescue AD2026", en la galería de artifacts de Carlos.

## Dónde estamos

- Rama de trabajo: `carlos`. El 8 de septiembre se crearon los primeros commits propios
  sobre `0d81846` (el commit de Víctor, idéntico a `origin/pre-release-0.1.0.1-board`) y se
  subió la rama con `git push -u origin carlos`. Verificar el estado real con
  `git branch -vv` y `git log --oneline --decorate -8`.
- El equipo trabaja desde `carlos`. `main` y las ramas `pre-release` no se tocaron.
- Fuera del control de versiones a propósito: `docs/MainScene.antes-de-camara.unity.bak`
  (respaldo local de la escena antes de integrar la cámara) y los cambios de
  `Server/data/final.txt`, que son solo finales de línea CRLF sin cambio de contenido.
- Carpeta local: `C:\Users\carlo\OneDrive\Documentos\Tec\Quinto semestre\Modelacion de agentes\Flashpoint-Fire-Rescue`
- Entorno de Carlos: Unity 6000.5.7f1, Python 3.14, Mesa 3.5.1.

## Cronología

- 6 de septiembre (mañana): Fase 1, motor de reglas en Mesa.
- 6 de septiembre (noche): incidente de Unity y su restauración, preview en Edit mode,
  Fase 4 cliente-servidor en el puerto 8585.
- 7 de septiembre (madrugada y mañana): Carlos validó Python con Unity a mano.
- 7 de septiembre: migración al puerto 3000, Fase 3 estrategia mejorada, primeros
  experimentos, documentación.
- 7 de septiembre (revisión técnica): el equipo detectó que las decisiones al azar y los
  eventos del juego compartían un solo generador aleatorio. Se separaron los generadores,
  se agregaron 15 pruebas del mecanismo, se repitieron los experimentos y se documentó el
  código por dentro. Los resultados anteriores quedaron descartados como evidencia final.
- 7 de septiembre (tarde): Carlos corrió personalmente las 109 pruebas y
  `run_batch.py 100 comparar` en su máquina, y volvió a validar Unity con el servidor.
- 8 de septiembre: auditoría académica contra la rúbrica, el reto, el reglamento del juego y
  los notebooks del curso; integración de MultiGrid de Mesa; 41 pruebas nuevas.
- 8 de septiembre (cierre técnico): cámara superior desplazable escrita e integrada en
  `MainScene.unity`; Carlos la validó a mano en Play junto con el servidor; se cerró el
  backend, se organizaron los commits y se subió la rama `carlos` a GitHub.

## Reproducibilidad verificada entre máquinas

El 7 de septiembre Carlos corrió en su PC (Python 3.14, Mesa 3.5.1) las 109 pruebas y
`run_batch.py 100 comparar`. Los CSV que generó se compararon celda por celda contra los
generados en el entorno de Claude (Python 3.13, Mesa 3.5.1): **5,700 celdas lógicas, 0
diferencias**. Lo único que cambia es `tiempo_ms`, que depende de la máquina.

Es la evidencia más fuerte que tiene el proyecto para la parte de "resultados precisos y
reproducibles" que pide el reto dentro del criterio de rendimiento.

El 8 de septiembre Carlos volvió a validar en su PC después de integrar MultiGrid:

    Python 3.14, Mesa 3.5.1
    python -m unittest discover -s tests -v   ->  Ran 150 tests in 371.445s, OK
    python run_batch.py 100 comparar          ->  0 / 38 / 44

Las métricas lógicas salieron idénticas al baseline anterior a MultiGrid. Con esto no queda
ninguna cifra del backend validada solo en el entorno de Claude.

Observación sobre esos 371 segundos: en el contenedor Linux la misma suite tarda unos 3
segundos. La diferencia casi seguro está en `test_server.py`, que levanta servidores HTTP
reales y pide `http://localhost`. En Windows "localhost" resuelve primero a IPv6 (`::1`), el
servidor de Python escucha en IPv4, y cada petición paga el tiempo de espera antes de caer a
`127.0.0.1`. `[INFERENCIA]` No se pudo reproducir Windows para confirmarlo. Si molesta, la
corrección es cambiar "localhost" por "127.0.0.1" en `tests/test_server.py`, pero el backend
está congelado y esto no afecta a ningún resultado.

## FASE 1: motor de reglas en Mesa (COMPLETADA)

Humo, avance del fuego por dados, explosiones con ondas de choque, flashover, daño estructural,
colapso, revelar y reponer POI, cargar y rescatar víctimas, extinguir, cortar paredes, derribo
de bomberos, AP guardados con tope de 4, `model.step()` con el orden del reglamento, condiciones
de victoria y derrota, DataCollector y estrategia inyectable.

Archivos: `Server/model/board.py`, `Server/model/fire_phase.py`,
`Server/model/flashpoint_model.py`, `Server/agents/firefighter_agent.py`,
`Server/tests/test_rules.py` (38 pruebas).

Origen por archivo (está escrito al inicio de cada uno): el parser del tablero, las
estructuras de datos, las consultas de puertas y movimiento, el esqueleto del modelo y la base
del agente (constructor, movimiento, puertas, acciones al azar) son de Víctor. El resto de la
Fase 1 se desarrolló con apoyo de Claude sobre esa base.

Bugs de reglas corregidos en su momento: el daño llegaba a 27 con solo 24 marcadores; faltaba
la regla de no terminar el turno sobre fuego; `final.txt` trae 2 aristas de pared asimétricas
(se resuelve con OR conservador en `has_wall_between`).

`FlashPointModel` llama `super().__init__(rng=seed)` en vez de `seed=seed`, porque Mesa 3.5
marca `seed` como obsoleto (salía un FutureWarning en la máquina de Carlos).

## GENERADORES ALEATORIOS (CORREGIDO el 7 de septiembre)

Problema detectado por el equipo: la estrategia aleatoria elegía con `model.random`, el mismo
generador que tiraba los dados del fuego y barajaba los POI. Cada decisión al azar corría la
secuencia que después usaba el fuego, así que "semilla 0 aleatoria" y "semilla 0 mejorada" no
recibían los mismos dados. Se comprobó sobre el código viejo: con la semilla 0, la primera
tirada del fuego ya era distinta entre las dos estrategias.

Solución (`FlashPointModel._create_random_generators`): tres `random.Random` independientes,
cada uno con una semilla que reparte un generador maestro creado con la semilla original:

    fire_random      dados del fuego (fila d6, columna d8)
    poi_random       barajado del mazo de POI y dados de reposición
    strategy_random  decisiones al azar de las estrategias (random_strategy.py)

Fuego y POI van separados a propósito: reponer POI tira los dados un número de veces que
depende del tablero (repite si la celda ya tiene POI), y si compartieran generador la
estrategia movería de forma indirecta la secuencia del fuego. El `self.random` de Mesa queda
sin usar. Todos los usos de aleatoriedad en `Server/` están clasificados: `fire_phase.roll_target`
(fuego), `_build_poi_deck` y `_replenish_pois` (POI), `random_strategy` (estrategia). No hay
ningún `random.random()`, `shuffle`, `choice` ni `randint` fuera de esos.

El modelo guarda además `model.fire_targets`, el historial de celdas que salieron en cada
fase del fuego, para poder comparar dos partidas con la misma semilla.

`tests/test_rng.py` (15 pruebas) verifica el mecanismo real: misma semilla y misma estrategia
dan la misma partida (para las tres estrategias); los dados del fuego son idénticos entre
aleatoria y mejorada mientras hayan jugado el mismo número de fases; el mazo de POI es
idéntico; pedirle 30 decisiones a la estrategia aleatoria no cambia la siguiente tirada del
fuego ni la de POI (y un control negativo demuestra que tocar el generador del fuego sí la
cambia); la mejorada no consume el generador de estrategia; y el `random` de Mesa no se toca en
toda una partida con ninguna estrategia.

## FASE 3: estrategia mejorada (COMPLETADA Y VERIFICADA POR CARLOS)

Carpeta `Server/strategies/`, con la interfaz `decide(model, firefighter)`:

- `random_strategy.py`: la solución aleatoria del criterio 1 (idea de `execute_random_action`
  del agente original de Víctor). Ahora elige con `model.strategy_random`. NO BORRAR.
- `astar.py`: A* sobre la topología real de `Board` (paredes y puertas en las aristas, fuego con
  costo extra o intransitable si carga víctima, puertas cerradas con +1 AP). Varios objetivos a
  la vez; devuelve `None` si no hay ruta.
- `prioritization.py`: puntuación de cada víctima o POI: costo A* de la ruta + castigo por POI sin
  revelar (2) - urgencia si el fuego lo toca (3) + castigo por cada otro bombero que ya va ahí
  (4) - bonificación por ser el objetivo que ya traía (1.5). Menor es mejor.
- `coordination.py`: tabla compartida bombero -> objetivo en `model.target_assignments`. Cada
  bombero elige la mejor respuesta al reparto actual de los demás, en secuencia. Es "mejor
  respuesta", no se calcula ningún equilibrio ni hay aprendizaje entre partidas. Eso hay que
  decirlo tal cual en la presentación.
- `improved_strategy.py`: el orden de decisión. 1) si carga víctima, A* a la salida más cercana;
  2) cargar víctima revelada bajo los pies; 3) apagar fuego adyacente del todo o humo adyacente;
  4) elegir objetivo con priorización y coordinación; 5) si el siguiente paso tiene fuego,
  apagarlo antes de cruzar; 6) avanzar o abrir puerta; 7) con AP sobrantes, apagar algo; sin
  objetivos, ir al fuego más cercano. Es determinista: no usa ningún generador.
- `__init__.py`: registro por nombre: `aleatoria`, `mejorada`, `mejorada_sin_coordinacion`.

Replaneación: no se guarda la ruta. A* se recalcula en cada acción sobre el tablero actual. Se
recuerda el objetivo para no oscilar; cada cambio de objetivo suma 1 a `model.replanifications`.

La regla 3 fue la decisión que más cambió el resultado: la primera versión ganaba 6.7% de las
partidas (con el RNG viejo) y perdía más víctimas que la aleatoria porque pasaba de largo junto
al fuego.

Material de Luis: sus seis archivos originales están sin cambios en `docs/referencia_luis/` con
un `LEEME.md` que dice qué se conservó de cada uno y qué se reescribió. Los módulos nuevos citan
el archivo del que parten con `ARCHIVO DE REFERENCIA:`.

Selección de estrategia: por nombre en `main.py`, `server.py`, `run_batch.py` y en el inspector
de Unity (`Strategy`). El servidor arranca con `mejorada`; `POST /reset` con
`{"estrategia": "aleatoria"}` cambia a la aleatoria sin reiniciar el proceso.

## EXPERIMENTOS: 100 semillas pareadas (0 a 99), con los generadores separados

Nota: los primeros resultados (aleatoria 0%, sin coordinación 26%, mejorada 40%) se
descartaron como evidencia final porque el RNG de decisiones y el RNG del entorno estaban
acoplados. Los de abajo son los vigentes.

Comando: `python run_batch.py 100 comparar`. Cada estrategia juega las semillas 0 a 99 con el
mismo tablero, el mismo mazo de POI y los mismos dados del fuego. Corrió completo en unos 11
segundos. Promedios por partida:

| Métrica | aleatoria | mejorada sin coordinación | mejorada |
|---|---|---|---|
| Victorias | 0 de 100 (0%) | 38 de 100 (38%) | 44 de 100 (44%) |
| Derrotas por víctimas | 0 | 11 | 11 |
| Colapsos | 100 | 51 | 45 |
| Víctimas rescatadas | 0.05 | 5.05 | 5.20 |
| Víctimas perdidas | 0.52 | 1.74 | 1.70 |
| Daño estructural | 24.00 | 19.49 | 19.08 |
| Turnos | 20.76 | 40.20 | 39.96 |
| Derribos | 20.86 | 7.38 | 7.99 |
| Replaneaciones | 0.00 | 1.28 | 2.52 |
| AP gastados | 86.95 | 162.91 | 161.98 |
| AP desperdiciados (arriba del tope de 4) | 0.00 | 0.55 | 0.57 |
| Celdas recorridas | 34.94 | 96.00 | 92.22 |
| Fuegos apagados | 11.16 | 16.22 | 16.42 |
| Humos apagados | 2.20 | 12.27 | 11.92 |
| Paredes cortadas | 12.55 | 0.00 | 0.00 |
| Tiempo por partida (ms) | 4.56 | 50.19 | 45.35 |

Cambio respecto a los resultados descartados: aleatoria igual (0%); mejorada sin coordinación
de 26% a 38%; mejorada de 40% a 44%. El aporte de la coordinación pasó de +14 a +6 puntos.

Lecturas honestas:

- Mejorada contra aleatoria: 44 victorias contra 0, y 5.15 víctimas rescatadas más por partida
  (intervalo de confianza del 95% aproximado: 4.74 a 5.56). Con 100 semillas esto sobra.
- Coordinación (mejorada contra sin coordinación) con las 100 semillas oficiales: +6 victorias,
  17 semillas donde solo gana la mejorada contra 11 donde solo gana la sin coordinación
  (prueba de signos p = 0.35), +0.15 rescatadas (intervalo de -0.14 a 0.44). NO es
  estadísticamente significativo con 100 semillas. No presentarlo como si lo fuera.
- Evidencia complementaria (no está en los CSV del repo; se corrió aparte con
  `run_batch.compare(runs=500, seed_start=100)`, semillas 100 a 599): mejorada 43.4% contra sin
  coordinación 31.0%; 94 semillas donde solo gana la mejorada contra 32 (p < 0.0001); +0.35
  rescatadas (0.21 a 0.49); -0.85 de daño (-1.33 a -0.38). Juntando las 600 semillas: 43.5%
  contra 32.2%. La coordinación sí aporta, pero hacen falta más de 100 semillas para
  demostrarlo. DECISIÓN PENDIENTE: subir el experimento oficial a 500 semillas
  (`python run_batch.py 500 comparar`, un minuto, reemplaza los CSV).
- Las víctimas perdidas suben respecto a la aleatoria (1.70 contra 0.52) porque la mejorada
  revela y mueve víctimas; la aleatoria deja casi todo boca abajo y pierde el edificio antes.
- El 45% de colapsos sigue siendo el margen de mejora más claro.

Archivos generados en `Server/`: `resultados_aleatoria.csv`, `resultados_mejorada.csv`,
`resultados_mejorada_sin_coordinacion.csv` (una fila por partida) y
`resultados_comparacion.csv` (promedios). Se regeneran con el comando de arriba; ningún
número se editó a mano.

## MULTIGRID DE MESA (INTEGRADO el 8 de septiembre)

### Por qué se integró

MultiGrid **no aparece literalmente** ni en la rúbrica oficial ni en el documento del reto ni
en ninguna de las cinco presentaciones del curso; se revisaron los textos completos. Lo que sí
existe es esto:

- El reto dice literalmente: "Puede haber más de un agente por celda".
- El notebook del profesor `multi-agents/MoneyModel/MoneyModel (AD2026).ipynb` dice, como
  comentario junto al import: "Debido a que necesitamos que existan más de un agente por
  celda, elegimos ''MultiGrid''".
- El mismo notebook abre con "# Requiere Mesa == 3.5".

O sea que el criterio del propio profesor para elegir MultiGrid es exactamente la condición que
el reto impone a este proyecto. Es el argumento que conviene dar si preguntan, en vez de "lo
vimos en clase".

En el resto de los notebooks (GameOfLife, RobotSweep, Segregation) el profesor usa `SingleGrid`,
y lo justifica al revés: "Debido a que necesitamos que existe un solo agente por celda,
elegimos ''SingleGrid''".

### Arquitectura: dos objetos, dos responsabilidades

    model.grid    MultiGrid(width=8, height=6, torus=False). Posición de los bomberos.
    model.board   Board propio. Paredes, puertas, salidas, fuego, humo, POI y daño.

Board no se tocó. Sigue siendo el dueño de la topología del juego, porque en Flash Point las
paredes y las puertas están en las aristas entre celdas y eso ningún grid de Mesa lo modela.

### Una sola fuente de verdad para la posición

`FirefighterAgent.row` y `.column` dejaron de ser atributos y ahora son propiedades que leen
`self.pos`, la posición que mantiene Mesa dentro del MultiGrid. Así no hay dos copias que se
puedan desincronizar, y todo el código que ya existía (`firefighter.row = x`, las pruebas, el
JSON, A*) sigue funcionando sin cambios.

Mesa ordena las coordenadas como `(x, y)` = `(columna, fila)`, al revés que el proyecto. La
traducción vive solo en esas propiedades.

### moore=False y torus=False

`FlashPointModel.movement_neighbors(row, column)` pide la vecindad al grid con `moore=False`
(vecindad de Von Neumann: arriba, abajo, izquierda y derecha) y la filtra con
`board.can_move_between`.

`moore=False` no es una preferencia del equipo. El reglamento oficial del juego dice
literalmente: "Adjacent spaces are those spaces that are up, down, left, or right from a space.
**Diagonal spaces are not Adjacent.** Closed Doors and Walls prevent spaces from being Adjacent
unless the Wall segment is Destroyed". El reto exige seguir ese reglamento.

`torus=False` porque el tablero es un edificio: una esquina no conecta con el extremo opuesto.

Nota para la presentación: el notebook MoneyModel del profesor usa `moore=True` y `torus=True`.
Eso es correcto para un modelo de intercambio de dinero, donde dar la vuelta al mundo no
significa nada raro. Copiarlo aquí rompería dos reglas del juego a la vez.

### El bug que casi se cuela

La primera versión de la integración pedía los vecinos al grid y los usaba en el orden en que
Mesa los devuelve. Mesa ordena por `(x, y)`, que en nuestras coordenadas es izquierda, arriba,
abajo, derecha; el proyecto los recorre como arriba, abajo, izquierda, derecha. El conjunto de
vecinos era idéntico en las 48 celdas, pero el **orden** cambiaba en 24 de ellas.

Ese orden llega al catálogo de acciones legales y de ahí a qué elige la estrategia aleatoria con
una semilla dada. Resultado: **1,164 métricas cambiaron** y la tasa de victoria de la mejorada
subía de 44% a 47% sin que ninguna regla hubiera cambiado. Se corrigió fijando el orden del
proyecto en `FlashPointModel.MOVEMENT_DIRECTIONS`, y después de eso los experimentos volvieron a
dar exactamente los mismos números que antes de MultiGrid.

Vale la pena contarlo: es un ejemplo concreto de una mejora aparente que era un artefacto.

## PRUEBAS: 150, todas pasan

    cd Server
    python -m unittest discover -s tests -v

- `test_rules.py`: 38 pruebas de reglas.
- `test_server.py`: 29 pruebas. Levantan el servidor real en un puerto libre y hacen peticiones
  HTTP. Incluyen puerto por defecto 3000, estrategia por defecto, cambio de estrategia por
  `/reset`, 400 con estrategia desconocida, reproducibilidad por semilla, y el contrato con
  Unity: una prueba lee `SimulationState.cs` y compara sus campos con las llaves del JSON.
- `test_strategy.py`: 27 pruebas de A*, priorización, coordinación y estrategia mejorada.
- `test_rng.py`: 15 pruebas de la separación de generadores (ver arriba).
- `test_multigrid.py`: 41 pruebas del espacio de Mesa. Comprueban que el modelo tiene un
  MultiGrid de 8x6 con torus=False, que los seis bomberos están colocados y sincronizados
  durante toda una partida, que dos bomberos pueden compartir celda, que el derribo también
  actualiza el grid, que `moore=False` da vecindad ortogonal y `torus=False` no da la vuelta,
  que las paredes y las puertas siguen bloqueando, que A* sigue sin diagonales, y que el orden
  de los vecinos coincide con el de Board en las 48 celdas. Incluyen dos controles negativos
  (con `moore=True` sí hay diagonales, con `torus=True` sí hay vuelta) para demostrar que las
  pruebas pueden fallar.

## DOCUMENTACIÓN DENTRO DEL CÓDIGO (hecha el 7 de septiembre)

Cada archivo importante tiene al inicio qué responsabilidad tiene, con qué se conecta, de dónde
viene (qué es de Víctor, qué de Luis, qué se desarrolló con apoyo de Claude) y sus advertencias:
`board.py`, `fire_phase.py`, `flashpoint_model.py`, `firefighter_agent.py`, los cinco módulos de
`strategies/`, `server.py`, `run_batch.py`, `main.py`, `SimulationState.cs` y
`SimulationClient.cs`. Marcas usadas: `NO BORRAR`, `NO MODIFICAR SIN REVISAR`,
`ARCHIVO DE REFERENCIA`, `PENDIENTE DE CONFIRMAR`, `COMPATIBILIDAD CON UNITY`. La atribución a
Claude va solo en los módulos donde de verdad hubo apoyo importante, y nunca sobre código
original de Víctor o Luis. `docs/USO_DE_IA.md` sigue siendo borrador y no sustituye esto.

## PREVIEW EN EDIT MODE (COMPLETADO, verificado por Carlos)

`BoardManager` genera el tablero en la pestaña Scene sin entrar a Play, con los botones
"Generate Preview" y "Clear Preview" del inspector (`BoardPreviewMarker.cs` y
`Editor/BoardManagerEditor.cs`). `BoardManager.Start()` llama a `ClearBoard()` antes de
construir, así que aunque la escena se guarde con el preview puesto no aparecen duplicados.

Aviso vigente: `MainScene.unity` está guardada con el preview activo (490 KB, 137 GameObjects).
Antes de commitear conviene pulsar "Clear Preview" y guardar.

## FASE 4: cliente-servidor (COMPLETADA Y VALIDADA MANUALMENTE)

Carlos corrió `python server.py` y la escena de Unity en Play el 7 de septiembre, todavía en el
puerto 8585, y confirmó con capturas que los bomberos se mueven y el fuego avanza con lo que
manda Python. La partida de la validación terminó en `derrota_colapso` en el turno 18 con daño
24, que es exactamente lo que dicta la regla de colapso.

    GET  /         información, estrategia activa y estrategias disponibles
    GET  /board    geometría fija: 48 celdas con sus paredes, 8 puertas, 4 salidas
    GET  /state    estado actual de la partida (incluye "estrategia")
    POST /step     avanza un turno y devuelve el estado resultante
    POST /reset    reinicia; acepta {"semilla": n, "estrategia": "aleatoria" | "mejorada"}

Cliente: `SimulationState.cs` (clases del JSON) y `SimulationClient.cs` (inspector: `Port` 3000,
`Strategy` "mejorada", `Seed` 42; manda estrategia y semilla en el `POST /reset`).
`BoardManager.ApplyState()` actualiza bomberos, fuegos, humos y POI sin reconstruir el tablero.

Puerto: 3000 por defecto en `server.py`, `SimulationClient.cs`, pruebas y documentación.
COMPATIBILIDAD CON UNITY: el valor que manda en Unity es el del inspector.

`SimulationClient` **sí está guardado en `MainScene.unity`**, sobre el GameObject `Systems`.
Verificado el 8 de septiembre leyendo la escena documento por documento: una sola instancia,
`Systems` tiene exactamente dos componentes (Transform y SimulationClient) y no lleva el
marcador de preview, así que un `Clear Preview` no lo borra.

Corrección de un reporte anterior: se llegó a afirmar que `SimulationClient` estaba sobre un
objeto de preview del tablero. Era falso, salió de una búsqueda de texto que capturó la línea
`m_GameObject` de un documento YAML anterior.

## CÁMARA SUPERIOR DESPLAZABLE (INTEGRADA Y VALIDADA el 8 de septiembre)

El reto pide, textualmente, "una vista superior en 2D utilizando modelos en 3D, permitiendo
desplazar la cámara por todo el escenario".

Archivos:

    Assets/Scripts/Framework/TopDownCameraController.cs        341 líneas, nuevo
    Assets/Scripts/Framework/TopDownCameraController.cs.meta    GUID d2ebcec8bdd94012ab443f8a328ee622
    Assets/Scenes/MainScene.unity                               componente agregado a Main Camera
    docs/MainScene.antes-de-camara.unity.bak                    respaldo local, NO versionado

Controles: WASD o flechas para desplazar, rueda del mouse para zoom (entre 3 y 14 de altura),
tecla F para volver al centro del tablero. El enfoque no se puede sacar más de 2 celdas del
borde.

Decisiones técnicas:

- Usa el Input System nuevo (`Keyboard.current`, `Mouse.current`) porque `ProjectSettings` tiene
  `activeInputHandler: 1`. Con esa configuración `Input.GetAxis` lanza excepción en runtime.
- Lee `rows`, `columns` y `cellSize` del `BoardManager` de la escena, solo lectura. Si el
  tablero cambiara de tamaño, los límites se ajustan solos.
- Funciona igual en cámara en perspectiva y en ortográfica.
- Dibuja gizmos en la pestaña Scene (rectángulo cian con el tablero, naranja con el área
  recorrible) al seleccionar la cámara, sin dar Play.
- Es una capa puramente visual: no habla con el servidor, no modifica `BoardManager` ni
  `SimulationClient`, y si se borra el script la simulación sigue funcionando igual.

Validación manual de Carlos el 8 de septiembre: MainScene abre, el componente aparece en
`Setup -> Main Camera` y es editable desde el Inspector, la cámara funciona en Play con WASD,
flechas y zoom, la simulación Python-Unity siguió corriendo al mismo tiempo, el servidor
respondió `POST /step` con HTTP 200 durante toda la partida, y la partida terminó normalmente
en `derrota_colapso`, turno 51, 6 rescatadas, 3 perdidas, daño 24. Sin errores rojos en Console.

Pendiente menor: el campo `Recenter Key` se serializó con el valor numérico 20 esperando que
corresponda a `F`. No se pudo verificar en documentación oficial. El script tiene una red (si el
campo llega vacío usa `F`), así que no bloquea nada; si en el Inspector se lee otra tecla, se
cambia ahí con el desplegable.

## ESTADO DE MainScene.unity (verificado el 8 de septiembre)

Leída documento por documento, no con búsqueda de texto:

    854 documentos YAML, 854 fileIDs únicos (0 duplicados)
    137 GameObjects, 137 Transforms, sin nombres duplicados
    1 Camera  -> Main Camera (Transform, Camera, AudioListener, TopDownCameraController)
    1 SimulationClient -> Systems, port 3000, strategy mejorada, seed 42,
                          autoStart true, resetOnConnect true
    1 BoardManager -> Board
    0 componentes duplicados
    128 objetos con BoardPreviewMarker (el preview del tablero)

Jerarquía: `Managers` (vacío), `Setup` (Main Camera, Directional Light), `Environment -> Board`,
`Canvases` (vacío), `Systems`, `Firefighters` (6 hijos).

El preview del tablero se conserva a propósito, para que el equipo pueda trabajar desde la
pestaña Scene sin dar Play. `Main Camera`, `Systems` y `Board` no llevan el marcador, así que un
`Clear Preview` deja 9 objetos y no pierde ni la cámara ni el cliente.

## Cómo correrlo

    cd Server
    python server.py                         escucha en http://localhost:3000, estrategia mejorada
    python server.py 3000 aleatoria          con la aleatoria
    python main.py 42                        una partida en consola
    python run_batch.py 100 comparar         los experimentos

En Unity: GameObject vacío bajo `Systems` con `SimulationClient`, Port = 3000, Strategy =
mejorada, Seed = 42, y Play con el servidor corriendo.

## Decisiones pendientes

- Umbral de colapso: el reto se contradice a sí mismo en el mismo párrafo. Dice "24 contadores
  de daño" y también "el edificio colapsa al acumular 25 puntos de daño o más". El reglamento
  oficial del juego, que el reto obliga a seguir, dice: "The game ends immediately as the
  building collapses when all 24 Damage markers have been placed on the board", y lista "24
  DAMAGE COUNTERS" entre los componentes. Se implementó 24 (`MAX_DAMAGE`). La evidencia apunta
  claramente a 24; conviene confirmarlo con el profesor pero no cambiar el código sin eso.
- Tamaño del experimento oficial: 100 semillas (actual) o 300 (recomendadas para poder afirmar
  el aporte de la coordinación).
- RESUELTO: los CSV de resultados se versionan en el repo. Son la evidencia del criterio 3 y
  pesan menos de 10 KB cada uno.
- RESUELTO: `docs/referencia_luis/` se queda en el repo, con su `LEEME.md`.
- Atribución exacta del trabajo de Víctor y Luis para `docs/USO_DE_IA.md` (ya está por archivo
  en los encabezados del código; falta que ellos lo confirmen).

## Archivos que NO se borran ni se modifican sin revisar

- `Assets/Scripts/Data.cs`, `Domain.cs`, `Framework.cs`, `Utils.cs` (por indicación de Carlos).
- `Server/strategies/random_strategy.py` (criterio 1 y control de los experimentos).
- `Server/data/final.txt` (tablero oficial; el parser asume su orden y cantidades).
- `Server/requirements.txt` (UTF-16, `pip install -r` no lo lee; no se reguarda sin autorización).
- `docs/referencia_luis/run_simulations.py`: NO USAR PARA MÉTRICAS DEL PROYECTO, contiene
  resultados generados artificialmente y se conserva únicamente como material previo del equipo.
- El repositorio `tc2008b` es solo referencia académica, nunca se modifica.

## BACKEND CONGELADO (8 de septiembre)

Por decisión del equipo, a partir del 8 de septiembre **no se modifica nada del backend salvo
que aparezca un bug real**. Congelados:

`board.py`, `flashpoint_model.py` (incluido el MultiGrid y `MOVEMENT_DIRECTIONS`),
`firefighter_agent.py`, `fire_phase.py`, los cinco módulos de `strategies/`, los tres
generadores aleatorios, `server.py`, `run_batch.py`, el contrato JSON con Unity y el puerto
3000.

Razón: el backend está validado extremo a extremo en dos máquinas y cualquier cambio obliga a
repetir toda la cadena de validación. El trabajo que queda es visual y de redacción.

Único pendiente del backend: confirmar con el profesor si el umbral de colapso es 24 o 25.

## Riesgos abiertos

- Dos personas editando `MainScene.unity` en paralelo. Es el riesgo número uno ahora que la
  rama está subida. Ver la regla 2 arriba.
- El trabajo visual (UI, VFX, modelos, materiales, iluminación) son 12 a 18 horas reales. Si
  recae en una sola persona, no sale a tiempo.
- Que alguien toque el backend "para mejorarlo" y mueva los resultados sin darse cuenta.
- Narrativa y presentación son 15% de la rúbrica y todavía no tienen dueño con nombre.
- 45% de colapsos con la mejorada.
- El aporte de la coordinación no se sostiene estadísticamente con 100 semillas. Con 300 sí:
  la tasa de discordancia observada es 25.7%, hacen falta unos 38 pares discordantes para 80%
  de potencia y eso son alrededor de 148 semillas.
- `MAX_DAMAGE` 24 vs 25: la evidencia del reglamento apunta a 24, falta confirmarlo. Fecha de
  entrega desconocida.
- Los nombres del JSON y los de C# tienen que moverse juntos; la prueba de contrato avisa, pero
  solo si se corren las pruebas.
- RESUELTO el 8 de septiembre: Carlos corrió las 150 pruebas y los experimentos en su PC.
- RESUELTO el 8 de septiembre: el proyecto ya usa `mesa.space.MultiGrid` para la posición de
  los bomberos, conviviendo con `Board`. Ver la sección de MultiGrid.
- RESUELTO el 8 de septiembre: `MainScene.unity` tiene `SimulationClient` en `Systems` y la
  cámara en `Main Camera`, ambos guardados en la escena y validados en Play.
- RESUELTO el 8 de septiembre: la rama `carlos` está subida a GitHub.

## Reglas de trabajo vigentes

- Trabajar sobre la rama `carlos`.
- Commits permitidos en `carlos`. Sin `merge`, `rebase`, `reset`, `switch`, `checkout` a otra
  rama ni `push --force` sin autorización explícita de Carlos.
- Nunca empujar a `main` ni a las ramas `pre-release` de Víctor.
- No eliminar ni reemplazar trabajo existente sin analizarlo y sin autorización.
- No inventar resultados ni métricas. Todo número sale de `run_batch.py`.
- No ocultar ni exagerar el uso de IA. La documentación principal va dentro del código.

## Estado de la rúbrica

- Criterio 1, solución aleatoria (10%): completo. Sigue todas las reglas Family, se puede
  correr desde consola, servidor y Unity.
- Criterio 2, estrategia mejorada (20%): completo a nivel de código y datos (44% contra 0%),
  corrido y verificado por Carlos. Falta explicarlo en el reporte.
- Criterio 3, rendimiento (20%): el texto literal de la rúbrica pide que "la simulación sea
  altamente eficiente, rápida y precisa" y que "todos los agentes funcionen de manera
  coordinada", y el reto añade "resultados precisos y reproducibles". No menciona gráficas. Lo
  que tenemos: 30 a 50 ms por partida, coordinación implementada y medida, y reproducibilidad
  demostrada entre dos máquinas con 0 diferencias. Falta la redacción del análisis; las
  gráficas ayudan al criterio 2 más que a este.
- Criterio 4, cliente-servidor (15%): completo y validado a mano. Falta grabar la evidencia.
- Criterio 5, visualización 3D (15%): la parte técnica está cerrada (tablero desde archivo,
  preview en el editor, actualización por turno, cámara superior desplazable funcionando en
  Play). Falta todo lo visual: UI con contadores, humo distinto del fuego, modelos 3D,
  materiales e iluminación. Es el criterio más lejos de su máximo.
- Criterio 6, narrativa (10%): pendiente.
- Criterio 7, innovación (5%): la coordinación tiene evidencia con 600 semillas (43.5% contra
  32.2%), no con 100. Decidir el tamaño del experimento antes de presentarla. Recomendación:
  300 semillas.
- Criterio 8, presentación (5%): pendiente.

## Siguiente fase: todo lo visual y la entrega

Lo técnico está cerrado. El trabajo que queda, en orden:

1. UI en pantalla: turno, estrategia activa, rescatadas, perdidas, daño y resultado final.
2. Fuego y humo claramente distintos. Hoy el humo es el mismo prefab a media escala.
3. Modelos 3D de bomberos y víctimas.
4. Materiales, iluminación y estética del tablero.
5. Narrativa: qué historia cuenta la simulación y por qué importa socialmente.
6. Gráficas de los CSV y análisis escrito.
7. Experimento final, si se decide correrlo (300 semillas).
8. Reporte y declaración de uso de IA.
9. Video de evidencia de la demo funcionando.
10. Presentación y ensayo.

Lo visual va antes que narrativa y presentación porque esas dos se construyen sobre lo que se
ve en pantalla. La excepción: narrativa y gráficas no dependen de Unity, así que quien no esté
tocando la escena puede empezarlas en paralelo desde ya.

El proyecto está en torno al 68% ponderado por rúbrica. Es avance de trabajo, no calificación
prevista: que un criterio esté al 100% significa que ya hicimos lo que nos toca, no que el
profesor vaya a dar los puntos completos.

## Pendiente de confirmar con los profesores o el equipo

- Fecha y hora exactas de entrega final.
- Umbral de colapso: 24 o 25 marcadores de daño.
- Si el profesor da por buena la arquitectura MultiGrid para posición y Board para topología.
- Formato que pide la materia para la declaración de uso de IA.
- Qué pack de assets 3D se aprueba para los bomberos. Kenney queda descartado por decisión del
  equipo. Opciones CC0 verificadas: "Low poly human pack" (8tibGames, OpenGameArt) y "Animated
  Human Low Poly" (Quaternius, OpenGameArt, incluye animación `death` para las víctimas).
  El proyecto usa Built-in Render Pipeline, así que VFX Graph y los packs solo-URP quedan
  fuera; para fuego y humo se recomienda el Particle System integrado.
