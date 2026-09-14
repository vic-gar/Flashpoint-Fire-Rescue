"""Reparto de objetivos entre los seis bomberos.

Guarda qué objetivo eligió cada bombero, en model.target_assignments.
prioritization.py lo consulta con others_on() e improved_strategy.py lo
actualiza en cada acción.

La idea viene del notebook de juegos del curso: cada bombero elige
respondiendo a lo que ya eligieron los demás (mejor respuesta), para que
no se amontonen sobre la misma víctima cuando hay alternativas. No se
calcula ningún equilibrio ni se aprende de partidas anteriores; el
reparto sale de que los seis aplican la misma regla en secuencia.
"""


# Castigo en AP equivalentes por cada otro bombero que ya va al mismo
# objetivo. Con 4, hace falta que la alternativa esté 4 AP más lejos
# para preferir compartir objetivo en vez de repartirse.
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
