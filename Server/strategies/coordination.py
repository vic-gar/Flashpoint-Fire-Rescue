"""Reparto de objetivos entre los seis bomberos.

Responsabilidad: llevar la tabla "qué objetivo eligió cada bombero" para
que, al elegir, cada uno tome en cuenta a los demás. La tabla vive en el
modelo (model.target_assignments) y la consulta prioritization.py a
través de others_on(); improved_strategy.py la actualiza en cada acción.

ARCHIVO DE REFERENCIA: docs/referencia_luis/best_response_coordination.py,
de Luis, basado en el notebook de juegos del curso. De ahí se conserva
la idea central: cada bombero elige su objetivo respondiendo a lo que ya
eligieron los demás (mejor respuesta), de modo que no se amontonen sobre
la misma víctima cuando hay alternativas. Reescrita con apoyo de Claude
porque la versión original trabajaba con identificadores abstractos y
con un historial de frecuencias que en nuestro modelo no existe; aquí se
usan las celdas reales del tablero y el estado actual de la partida.

Qué es exactamente lo que se implementa (para poder explicarlo bien):
  - Jugadores: los seis bomberos.
  - Opciones de cada uno: los POI y víctimas reveladas del tablero.
  - Utilidad de una opción: el negativo de su costo de planeación por
    la ruta A* (AP equivalentes), menos un castigo por cada OTRO
    bombero que ya la eligió.
  - Decisión: cada bombero, cuando le toca actuar, se queda con la
    opción de mayor utilidad dado lo que los demás tienen asignado.
Es una "mejor respuesta" al reparto actual, evaluada por turnos. No se
calcula ningún equilibrio ni se aprende de partidas anteriores; el
reparto emerge de que todos aplican la misma regla en secuencia.
"""


# Castigo en AP equivalentes por cada otro bombero que ya va al mismo
# objetivo. Con 4 hace falta que la alternativa esté 4 AP más lejos
# para preferir compartir objetivo en vez de repartirse.
# NO MODIFICAR SIN REVISAR: cambia los resultados de los experimentos.
CROWDING_PENALTY = 4


class TargetAssignments:
    """Tabla compartida bombero -> celda objetivo.

    Vive en el modelo (model.target_assignments) para que todos los
    bomberos vean el mismo reparto durante la partida.
    """

    def __init__(self):
        self.by_firefighter = {}

    def get(self, firefighter_id):
        return self.by_firefighter.get(firefighter_id)

    def claim(self, firefighter_id, cell):
        self.by_firefighter[firefighter_id] = cell

    def release(self, firefighter_id):
        self.by_firefighter.pop(firefighter_id, None)

    def others_on(self, cell, firefighter_id):
        """Cuántos bomberos distintos a este ya eligieron esa celda."""
        return sum(
            1
            for other_id, other_cell in self.by_firefighter.items()
            if other_id != firefighter_id and other_cell == cell
        )

    def cleanup(self, board):
        """Suelta los objetivos que ya no existen en el tablero.

        Un POI desaparece al rescatarse, perderse o resultar falsa
        alarma; el bombero que lo tenía asignado debe volver a elegir.
        """
        stale = [
            firefighter_id
            for firefighter_id, cell in self.by_firefighter.items()
            if board.get_poi_at(*cell) is None
        ]

        for firefighter_id in stale:
            self.release(firefighter_id)


def get_assignments(model):
    """Devuelve la tabla del modelo, creándola la primera vez."""
    if not hasattr(model, "target_assignments"):
        model.target_assignments = TargetAssignments()

    return model.target_assignments
