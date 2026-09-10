"""Búsqueda de rutas con A* sobre el tablero de Flash Point.

Dada una celda de inicio y una o varias metas, devuelve la ruta más
barata respetando paredes, puertas y fuego. El costo va en AP
equivalentes: los AP de moverse, uno más por cada puerta cerrada que hay
que abrir, y un castigo extra por cada celda con fuego que se atraviesa.
Ese castigo hace que el bombero prefiera un rodeo corto antes que cruzar
fuego. Si carga una víctima, el fuego es intransitable.

La heurística es la distancia Manhattan. Nunca sobreestima el costo real
porque el movimiento es en cuatro direcciones y cada paso cuesta al
menos 1 AP, así que la ruta que encuentra es la más barata.

Los vecinos salen de Board.can_move_between y no de una matriz de tipos
de celda, porque en Flash Point las paredes y las puertas están en las
aristas entre celdas.
"""

import heapq


DIRECTIONS = [
    (-1, 0),
    (1, 0),
    (0, -1),
    (0, 1),
]

# Estos dos valores cambian las rutas y con ellas los resultados.

# Entrar a fuego cuesta 2 AP reales. Se suma un castigo extra en la
# planeación para que el bombero prefiera un rodeo corto antes que
# cruzar fuego, sin prohibirlo del todo.
FIRE_PENALTY = 2

# Abrir una puerta cerrada cuesta 1 AP; A* la considera transitable con
# ese costo extra en vez de tratarla como pared.
DOOR_COST = 1


def manhattan(a, b):
    return abs(a[0] - b[0]) + abs(a[1] - b[1])


def planning_neighbors(board, cell, carrying):
    """Vecinos alcanzables desde una celda para efectos de planeación.

    Devuelve tuplas (vecino, costo). Usa las mismas reglas de movimiento
    que FirefighterAgent.get_move_cost, más el costo de abrir puertas
    cerradas, porque el plan puede incluir abrirlas.
    """
    row, column = cell
    result = []

    for delta_row, delta_column in DIRECTIONS:
        next_cell = (row + delta_row, column + delta_column)

        if not board.is_inside(*next_cell):
            continue

        extra = 0

        door = board.get_door_between(row, column, *next_cell)

        if door is not None and not door.is_open and not door.is_destroyed:
            extra = DOOR_COST
        elif not board.can_move_between(row, column, *next_cell):
            continue

        if board.has_fire(*next_cell):
            if carrying:
                # Regla del juego: no se puede cargar una víctima
                # hacia una celda con fuego.
                continue

            cost = 2 + FIRE_PENALTY
        else:
            cost = 2 if carrying else 1

        result.append((next_cell, cost + extra))

    return result


def find_path(board, start, goals, carrying=False):
    """Ruta de menor costo de planeación desde start hasta cualquiera de goals.

    goals puede ser una celda o una colección de celdas; con varias
    metas se detiene en la primera que alcanza, que es la más barata.

    Devuelve (ruta, costo) donde ruta es la lista de celdas desde start
    (incluida) hasta la meta (incluida), o (None, None) si no hay forma
    de llegar. El costo está en AP equivalentes (incluye FIRE_PENALTY y
    DOOR_COST), no en AP reales.
    """
    if isinstance(goals, tuple) and len(goals) == 2 and isinstance(goals[0], int):
        goals = [goals]

    goals = set(goals)

    if not goals:
        return None, None

    if start in goals:
        return [start], 0

    def heuristic(cell):
        return min(manhattan(cell, goal) for goal in goals)

    # La cola guarda (f, orden, celda). El contador "orden" desempata
    # de forma estable para que la ruta sea reproducible.
    counter = 0
    open_heap = [(heuristic(start), counter, start)]

    best_cost = {start: 0}
    parent = {start: None}
    closed = set()

    while open_heap:
        # Se saca la celda con menor f. Una celda puede estar varias
        # veces en la cola (cada vez que se le encontró un costo mejor);
        # las copias viejas se descartan al verla ya cerrada.
        _, _, current = heapq.heappop(open_heap)

        if current in closed:
            continue

        closed.add(current)

        # La primera meta que se cierra es la más barata de alcanzar.
        if current in goals:
            return _reconstruct(parent, current), best_cost[current]

        for neighbor, step_cost in planning_neighbors(board, current, carrying):
            if neighbor in closed:
                continue

            new_cost = best_cost[current] + step_cost

            # Relajación: solo se guarda si se llegó más barato que antes.
            if new_cost < best_cost.get(neighbor, float("inf")):
                best_cost[neighbor] = new_cost
                parent[neighbor] = current

                counter += 1
                heapq.heappush(
                    open_heap,
                    (new_cost + heuristic(neighbor), counter, neighbor)
                )

    # Se vació la cola sin tocar ninguna meta: no hay ruta. Se devuelve
    # None a propósito, para que quien llama sepa que falló y no reciba
    # una ruta incompleta que parezca válida.
    return None, None


def _reconstruct(parent, goal):
    """Sigue los punteros al padre desde la meta hasta el inicio."""
    path = []
    current = goal

    while current is not None:
        path.append(current)
        current = parent[current]

    path.reverse()

    return path


def next_step_action(board, path):
    """Convierte el siguiente paso de una ruta en una acción del bombero.

    Si entre la celda actual y la siguiente hay una puerta cerrada, la
    acción es abrirla; si no, es moverse. Devuelve None con rutas de
    una sola celda (ya está en la meta).
    """
    if path is None or len(path) < 2:
        return None

    current = path[0]
    following = path[1]

    door = board.get_door_between(*current, *following)

    if door is not None and not door.is_open and not door.is_destroyed:
        return ("open_door", door)

    return ("move", following[0], following[1])
