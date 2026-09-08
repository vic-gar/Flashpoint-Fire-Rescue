"""Estrategia mejorada: A* + priorización + coordinación + replaneación.

Es la estrategia del criterio 2 de la rúbrica. Se llama una vez por
acción: el modelo la invoca repetidamente durante el turno hasta que el
bombero se queda sin AP o la estrategia devuelve None. Solo puede
devolver acciones del catálogo firefighter.get_legal_actions(), así que
no puede romper las reglas.

Responsabilidad de este archivo: el ORDEN de decisión. Las piezas viven
aparte: astar.py (rutas), prioritization.py (qué objetivo), y
coordination.py (reparto entre bomberos). Es determinista: con el mismo
tablero toma la misma decisión, no usa ningún generador aleatorio.

Origen: desarrollada con apoyo de Claude sobre las ideas de los
algoritmos de Luis (ver docs/referencia_luis/LEEME.md). El orden de las
reglas salió de medir partidas reales con run_batch.py; la regla 3 fue
la decisión más importante y está explicada abajo. El equipo debe poder
explicar cada regla y por qué está en ese lugar.

Orden de decisión en cada llamada:

  1. Si carga una víctima, va por la ruta más corta a una salida. Si el
     fuego le cierra el paso, apaga el fuego de al lado.
  2. Si está parado sobre una víctima revelada, la carga (0 AP).
  3. Control del incendio: si tiene fuego adyacente y le alcanza para
     apagarlo del todo, lo apaga; si tiene humo adyacente, lo apaga.
     Esta fue una de las reglas que más mejoró el comportamiento
     durante las pruebas exploratorias: reducir fuego y humo disminuye
     el riesgo de explosiones y flashover. Cada celda con fuego sube la
     probabilidad de explosión en la tirada de dados, y el humo junto al
     fuego se convierte en fuego en el flashover. Apagar humo cuesta
     1 AP y evita un fuego, es la acción más barata del juego por
     riesgo eliminado.
  4. Elige objetivo: la víctima o POI con mejor puntuación (ver
     prioritization.py), tomando en cuenta qué objetivos ya eligieron
     los demás bomberos (ver coordination.py).
  5. Si el siguiente paso de la ruta tiene fuego, lo apaga antes de
     avanzar en lugar de atravesarlo.
  6. Avanza un paso por la ruta, o abre la puerta que le estorba.
  7. Si no le alcanzan los AP para el paso, los usa en apagar algo
     adyacente; si tampoco, termina el turno y los guarda.
  8. Sin objetivos alcanzables, se dedica al fuego más cercano.

Replaneación: la ruta no se guarda entre llamadas. En cada acción se
vuelve a correr A* sobre el tablero tal como está, así que fuego nuevo,
puertas destruidas o POI que desaparecen se toman en cuenta solos. Lo
que sí se recuerda es el objetivo elegido, para no cambiarlo por
diferencias mínimas; cuando cambia se cuenta como una replaneación en
model.replanifications.

NO MODIFICAR SIN REVISAR: los pesos viven en prioritization.py y
coordination.py. Se probaron variantes en dos bloques de 30 semillas y
las que ganaban en un bloque perdían en el otro; los valores actuales
son los que se comportaron igual en ambos.
"""

from strategies import astar
from strategies.coordination import get_assignments
from strategies.prioritization import choose_target


def make_improved_strategy(use_coordination=True):
    """Construye la estrategia. Con use_coordination=False cada bombero
    ignora a los demás al elegir objetivo; sirve para medir cuánto
    aporta coordinar (criterio de innovación)."""

    def decide(model, firefighter):
        board = model.board
        legal = firefighter.get_legal_actions()

        if not legal:
            return None

        assignments = get_assignments(model)
        assignments.cleanup(board)

        position = (firefighter.row, firefighter.column)

        # 1. Si el bombero carga una víctima, la prioridad es llegar a
        # una salida. A* con carrying=True: cada paso cuesta 2 AP y no
        # se puede pasar por fuego. Si no hay ruta o no alcanzan los
        # AP, usa lo que queda en apagar el fuego que le estorba.
        if firefighter.carrying_victim:
            exits = [(e.row, e.column) for e in board.exits]
            path, _ = astar.find_path(board, position, exits, carrying=True)

            action = _step_along(board, path, legal)

            if action is not None:
                return action

            return _extinguish_adjacent(board, legal)

        # 2. Víctima revelada bajo los pies.
        if ("pick_up",) in legal:
            return ("pick_up",)

        # 3. Control del incendio alrededor del bombero.
        action = _extinguish_adjacent(board, legal, only_full_fire=True)

        if action is not None:
            return action

        # 4. Elegir objetivo. La ruta se recalcula en cada llamada
        # porque el fuego puede cambiar después de cada turno
        # (replaneación); solo el objetivo elegido se recuerda.
        target = choose_target(
            board,
            firefighter,
            assignments,
            use_coordination
        )

        if target is None:
            return _fight_fire(model, firefighter, legal)

        cell, kind, path = target

        _record_choice(model, firefighter, assignments, cell)

        # 5. Fuego en el siguiente paso: apagarlo antes que cruzarlo.
        # Cruzar cuesta 2 AP y deja fuego atrás; apagar cuesta 2 AP y
        # además baja la probabilidad de explosión.
        if len(path) >= 2 and board.has_fire(*path[1]):
            extinguish = ("extinguish", path[1][0], path[1][1], True)

            if extinguish in legal:
                return extinguish

        # 6. Avanzar por la ruta.
        action = _step_along(board, path, legal)

        if action is not None:
            return action

        # 7. No alcanza para el paso: aprovechar los AP que quedan.
        return _extinguish_adjacent(board, legal)

    decide.strategy_name = (
        "mejorada" if use_coordination else "mejorada_sin_coordinacion"
    )

    return decide


# =========================================================
# Ayudantes
# =========================================================

def _step_along(board, path, legal):
    """Siguiente acción de una ruta si el bombero la puede pagar.

    Si no está en el catálogo de acciones legales es porque no le
    alcanzan los AP, y entonces se devuelve None.
    """
    action = astar.next_step_action(board, path)

    if action is not None and action in legal:
        return action

    return None


def _extinguish_adjacent(board, legal, only_full_fire=False):
    """Apaga algo en la celda propia o adyacente.

    Preferencia: fuego apagado del todo (2 AP), luego humo (1 AP), y
    al final bajar fuego a humo (1 AP) cuando no alcanza para más. Con
    only_full_fire=True se salta esa última opción, porque dejar humo
    detrás mientras se avanza hacia un objetivo no vale el AP.
    """
    fire_full = []
    smoke = []
    fire_partial = []

    for action in legal:
        if action[0] != "extinguish":
            continue

        row, column, full = action[1], action[2], action[3]

        if board.has_fire(row, column):
            if full:
                fire_full.append(action)
            else:
                fire_partial.append(action)
        elif board.has_smoke(row, column):
            smoke.append(action)

    if fire_full:
        return fire_full[0]

    if smoke:
        return smoke[0]

    if fire_partial and not only_full_fire:
        return fire_partial[0]

    return None


def _fight_fire(model, firefighter, legal):
    """Sin objetivos: ir hacia el fuego más cercano y apagarlo."""
    board = model.board

    action = _extinguish_adjacent(board, legal)

    if action is not None:
        return action

    fires = [(f.row, f.column) for f in board.fires]

    if not fires:
        return None

    position = (firefighter.row, firefighter.column)
    path, _ = astar.find_path(board, position, fires, carrying=False)

    # Se avanza hasta quedar junto al fuego, no encima de él.
    if path is not None and len(path) > 2:
        return _step_along(board, path[:-1], legal)

    return None


def _record_choice(model, firefighter, assignments, cell):
    """Guarda el objetivo elegido y cuenta los cambios de plan."""
    previous = assignments.get(firefighter.firefighter_id)

    if previous is not None and previous != cell:
        model.replanifications += 1

    assignments.claim(firefighter.firefighter_id, cell)


improved_strategy = make_improved_strategy(use_coordination=True)
improved_strategy_no_coordination = make_improved_strategy(use_coordination=False)
