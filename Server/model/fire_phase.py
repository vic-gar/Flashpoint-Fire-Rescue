"""Fase de avance del fuego de Flash Point: Fire Rescue.

Al terminar el turno de un bombero se tira un dado de seis caras
para la fila y uno de ocho para la columna. La celda que sale es
el objetivo y lo que pasa ahí depende de lo que ya hubiera:

    celda despejada  -> aparece humo
    celda con humo   -> el humo se convierte en fuego
    celda con fuego  -> explosión

Después de la explosión se revisa el flashover: todo humo que
quede junto a fuego se convierte en fuego, y se repite hasta que
ya no queda humo tocando fuego.

Al final se resuelven las consecuencias: los bomberos que quedaron
en una celda con fuego son derribados y los POI que quedaron en
una celda con fuego se pierden.

FlashPointModel.step() la llama una vez por turno, después de las
acciones del bombero. Trabaja directamente sobre model.board y sobre
los contadores del modelo.

Las reglas salen del reglamento oficial del juego y se verifican en
tests/test_rules.py. Cualquier cambio aquí cambia los resultados de
run_batch.py.
"""

from model.board import CLEAR, SMOKE, FIRE


DIRECTIONS = [
    (-1, 0),
    (1, 0),
    (0, -1),
    (0, 1),
]


def roll_target(model, generator=None):
    """Tira los dados y devuelve la celda objetivo.

    El dado rojo de seis caras da la fila y el negro de ocho da
    la columna.

    RNG: por defecto usa model.fire_random, el generador que solo
    consume el fuego. Reponer POI usa los mismos dados pero pasa su
    propio generador (model.poi_random) para no correr la secuencia
    del fuego. Ninguna estrategia debe tirar con estos generadores.
    """
    if generator is None:
        generator = model.fire_random

    row = generator.randint(0, model.board.ROWS - 1)
    column = generator.randint(0, model.board.COLUMNS - 1)

    return (row, column)


def advance_fire(model, target=None):
    """Ejecuta la fase completa de avance del fuego.

    Si no se da una celda objetivo se tiran los dados. Las pruebas
    pasan la celda a mano para provocar un caso concreto.

    Devuelve un resumen de lo que ocurrió para poder registrarlo
    en las métricas y mostrarlo en el log de la partida.
    """
    board = model.board

    if target is None:
        target = roll_target(model)

    # Se guarda la celda que salió para poder comparar dos partidas
    # con la misma semilla (ver tests/test_rng.py).
    model.fire_targets.append(target)

    row, column = target

    summary = {
        "target": target,
        "explosion": False,
        "new_smoke": 0,
        "new_fire": 0,
        "walls_damaged": 0,
        "doors_destroyed": 0,
        "flashovers": 0,
        "knocked_down": [],
        "pois_lost": [],
    }

    state = board.get_state(row, column)

    if state == CLEAR:
        board.set_state(row, column, SMOKE)
        summary["new_smoke"] += 1

    elif state == SMOKE:
        board.set_state(row, column, FIRE)
        summary["new_fire"] += 1

    else:
        _resolve_explosion(model, row, column, summary)
        summary["explosion"] = True

    summary["flashovers"] = _resolve_flashover(model)

    _resolve_knock_downs(model, summary)
    _resolve_lost_pois(model, summary)

    return summary


# =========================================================
# Explosiones
# =========================================================

def _resolve_explosion(model, row, column, summary):
    """Una explosión se propaga en las cuatro direcciones.

    En cada dirección ocurre exactamente una de tres cosas:
    se agrega fuego, se daña una pared o se destruye una puerta.
    Si la celda vecina ya tenía fuego se genera una onda de choque
    que sigue avanzando en esa misma dirección.
    """
    for delta_row, delta_column in DIRECTIONS:
        _propagate(
            model,
            row,
            column,
            delta_row,
            delta_column,
            summary
        )


def _propagate(model, row, column, delta_row, delta_column, summary):
    board = model.board

    current_row = row
    current_column = column

    while True:
        next_row = current_row + delta_row
        next_column = current_column + delta_column

        # Una puerta en el camino se destruye y detiene la onda.
        door = board.get_door_between(
            current_row,
            current_column,
            next_row,
            next_column
        )

        if door is not None and not door.is_destroyed:
            board.destroy_door(door)
            summary["doors_destroyed"] += 1
            return

        # Una pared recibe un marcador de daño y detiene la onda.
        blocked = (
            board.has_wall_between(
                current_row,
                current_column,
                next_row,
                next_column
            )
            and not board.is_wall_destroyed(
                current_row,
                current_column,
                next_row,
                next_column
            )
        )

        if blocked:
            if board.add_wall_damage(
                current_row,
                current_column,
                next_row,
                next_column
            ):
                summary["walls_damaged"] += 1
            return

        # Fuera del tablero sin pared que dañar: la onda se pierde.
        if not board.is_inside(next_row, next_column):
            return

        state = board.get_state(next_row, next_column)

        if state == FIRE:
            # Onda de choque: atraviesa el fuego y sigue.
            current_row = next_row
            current_column = next_column
            continue

        # Espacio abierto o con humo: aquí se detiene y prende.
        board.set_state(next_row, next_column, FIRE)
        summary["new_fire"] += 1
        return


# =========================================================
# Flashover
# =========================================================

def _resolve_flashover(model):
    """Todo humo junto a fuego se convierte en fuego.

    Se repite hasta que no quede humo tocando fuego, porque cada
    conversión puede encender al humo de al lado.
    """
    board = model.board

    rounds = 0

    while True:
        converted = []

        for row in range(board.ROWS):
            for column in range(board.COLUMNS):
                if not board.has_smoke(row, column):
                    continue

                if _touches_fire(board, row, column):
                    converted.append((row, column))

        if not converted:
            return rounds

        for row, column in converted:
            board.set_state(row, column, FIRE)

        rounds += 1


def _touches_fire(board, row, column):
    """El humo solo se enciende si el fuego puede alcanzarlo.

    Una pared o una puerta cerrada aíslan al humo del fuego que
    está del otro lado, así que se usa la misma conectividad que
    para el movimiento.
    """
    for delta_row, delta_column in DIRECTIONS:
        next_row = row + delta_row
        next_column = column + delta_column

        if not board.can_move_between(
            row,
            column,
            next_row,
            next_column
        ):
            continue

        if board.has_fire(next_row, next_column):
            return True

    return False


# =========================================================
# Consecuencias
# =========================================================

def _resolve_knock_downs(model, summary):
    """Un bombero que queda en una celda con fuego es derribado.

    Vuelve a la entrada más cercana y, si venía cargando una
    víctima, esa víctima se pierde.
    """
    board = model.board

    for firefighter in model.firefighters:
        if not board.has_fire(firefighter.row, firefighter.column):
            continue

        if firefighter.carrying_victim:
            firefighter.carrying_victim = False
            model.victims_lost += 1

        exit_point = _nearest_exit(board, firefighter)

        firefighter.row = exit_point.row
        firefighter.column = exit_point.column
        firefighter.action_points = 0

        summary["knocked_down"].append(firefighter.firefighter_id)
        model.knock_downs += 1


def _nearest_exit(board, firefighter):
    """Entrada más cercana en línea recta, como en el reglamento."""
    best = board.exits[0]
    best_distance = None

    for exit_point in board.exits:
        distance = (
            abs(exit_point.row - firefighter.row)
            + abs(exit_point.column - firefighter.column)
        )

        if best_distance is None or distance < best_distance:
            best = exit_point
            best_distance = distance

    return best


def _resolve_lost_pois(model, summary):
    """Un POI en una celda con fuego se pierde.

    Si todavía estaba boca abajo se revela primero, porque solo
    cuentan como víctima perdida los que resultan ser víctimas.
    """
    board = model.board

    for poi in list(board.pois):
        if not board.has_fire(poi.row, poi.column):
            continue

        poi.revealed = True

        board.remove_poi(poi)

        if poi.is_victim:
            model.victims_lost += 1
            summary["pois_lost"].append("v")
        else:
            model.false_alarms_revealed += 1
            summary["pois_lost"].append("f")
