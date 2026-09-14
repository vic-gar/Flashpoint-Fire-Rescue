"""Elección del objetivo que más conviene atender.

Puntúa cada víctima revelada y cada POI boca abajo del tablero y
devuelve el mejor junto con su ruta. Lo llama improved_strategy.py una
vez por acción; usa astar.py para el costo de cada ruta y
coordination.py para saber cuántos bomberos ya van al mismo sitio.

Es una heurística voraz: se puntúa cada objetivo y se toma el mejor.
Menor puntuación es mejor y todo va en AP equivalentes.

    costo de la ruta A* hasta el objetivo
  + castigo si es un POI sin revelar (podría ser falsa alarma)
  - bonificación si el fuego ya toca al objetivo
  + castigo por cada otro bombero que ya va al mismo objetivo
  - bonificación si es el objetivo que este bombero ya traía

La última línea evita que el bombero oscile entre dos POI igual de
lejanos.
"""

from strategies import astar
from strategies.coordination import CROWDING_PENALTY


# Cambiar estos pesos cambia los resultados de los experimentos.

# Un POI sin revelar vale menos que una víctima confirmada porque uno de
# cada tres marcadores del juego es falsa alarma.
HIDDEN_POI_PENALTY = 2

# Una víctima con fuego al lado se pierde en cuanto el fuego le caiga
# encima, así que se atiende antes que otra igual de lejana.
URGENCY_BONUS = 3

# Cuánto tiene que mejorar otro objetivo para abandonar el actual.
STICKINESS_BONUS = 1.5


def candidate_targets(board):
    """Lista de (celda, tipo) con tipo 'victima' o 'poi'."""
    targets = []

    for poi in board.pois:
        cell = (poi.row, poi.column)

        if poi.revealed and poi.is_victim:
            targets.append((cell, "victima"))
        elif not poi.revealed:
            targets.append((cell, "poi"))

    return targets


def fire_touches(board, cell):
    """Hay fuego en alguna celda vecina.

    Aproximación a propósito: no revisa paredes. Una pared frena la
    explosión pero recibe daño, y con dos marcadores desaparece, así
    que un fuego pegado a la pared sigue siendo un riesgo real.
    """
    row, column = cell

    for delta_row, delta_column in astar.DIRECTIONS:
        if board.has_fire(row + delta_row, column + delta_column):
            return True

    return False


def score_target(board, cell, kind, path_cost, others_here, is_current):
    """Aplica la regla de puntuación del encabezado. Menor es mejor."""
    score = path_cost

    if kind == "poi":
        score += HIDDEN_POI_PENALTY

    if fire_touches(board, cell):
        score -= URGENCY_BONUS

    score += CROWDING_PENALTY * others_here

    if is_current:
        score -= STICKINESS_BONUS

    return score


def choose_target(board, firefighter, assignments, use_coordination=True):
    """Devuelve (celda, tipo, ruta) del mejor objetivo o None si no hay.

    Calcula la ruta A* a cada candidato, lo puntúa y se queda con el de
    menor puntuación. Con use_coordination=False se ignora cuántos
    bomberos ya van a cada objetivo, que es la variante que sirve para
    medir por separado el aporte de la coordinación.
    """
    start = (firefighter.row, firefighter.column)
    current = assignments.get(firefighter.firefighter_id)

    best = None
    best_score = None

    for cell, kind in candidate_targets(board):
        path, cost = astar.find_path(board, start, cell, carrying=False)

        if path is None:
            continue

        others_here = 0

        if use_coordination:
            others_here = assignments.others_on(
                cell,
                firefighter.firefighter_id
            )

        score = score_target(
            board,
            cell,
            kind,
            cost,
            others_here,
            is_current=(cell == current)
        )

        # Desempate estable por posición para que la elección sea
        # reproducible entre corridas con la misma semilla.
        key = (score, cell)

        if best_score is None or key < best_score:
            best_score = key
            best = (cell, kind, path)

    return best
