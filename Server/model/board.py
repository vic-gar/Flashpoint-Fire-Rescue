"""Tablero de Flash Point: celdas, paredes, puertas, salidas, fuego, humo y POI.

Guarda el estado físico del edificio y responde preguntas sobre él
(¿hay pared entre estas dos celdas?, ¿se puede pasar?, ¿qué celdas
tienen fuego?). No decide nada: las reglas del turno están en
flashpoint_model.py y fire_phase.py, y las acciones en
firefighter_agent.py.

data/final.txt es el mismo archivo que Unity lee desde Assets/Resources,
así que los dos lados construyen el mismo edificio. El parser asume las
secciones en este orden: 6 filas de paredes, 3 POI, 10 fuegos, 8 puertas
y 4 salidas. Cambiar el orden obliga a cambiar los dos lectores.

Coordenadas: filas 0 a 5 y columnas 0 a 7 en todo el código. El archivo
viene con base 1 y aquí se resta 1 al leerlo.
"""

from dataclasses import dataclass
from pathlib import Path


# =========================================================
# Estados posibles de una celda
#
# Una celda puede estar despejada, con humo o con fuego.
# El humo se convierte en fuego cuando queda junto a fuego
# o cuando le cae encima otro marcador de humo.
# =========================================================

CLEAR = 0
SMOKE = 1
FIRE = 2


# =========================================================
# Datos de una celda
# =========================================================

@dataclass
class CellData:
    row: int
    column: int

    wall_up: bool
    wall_left: bool
    wall_down: bool
    wall_right: bool


# =========================================================
# Punto de interés
#
# Un POI empieza boca abajo. Cuando un bombero entra a su
# celda se revela y se sabe si era víctima o falsa alarma.
# =========================================================

@dataclass
class POIData:
    row: int
    column: int
    poi_type: str

    revealed: bool = False

    @property
    def is_victim(self):
        return self.poi_type == "v"

    @property
    def is_false_alarm(self):
        return self.poi_type == "f"


# =========================================================
# Fuego
# =========================================================

@dataclass
class FireData:
    row: int
    column: int


# =========================================================
# Puerta
# =========================================================

@dataclass
class DoorData:
    row1: int
    column1: int
    row2: int
    column2: int
    is_open: bool = False
    is_destroyed: bool = False


# =========================================================
# Entrada / salida
# =========================================================

@dataclass
class ExitData:
    row: int
    column: int


# =========================================================
# Tablero
# =========================================================

class Board:
    ROWS = 6
    COLUMNS = 8

    # Una pared queda destruida cuando acumula dos marcadores
    # de daño. A partir de ahí bomberos y fuego pasan por ahí.
    DAMAGE_TO_DESTROY_WALL = 2

    # El juego trae 24 marcadores de daño. Cuando se acaban no se
    # coloca ninguno más y el edificio colapsa.
    TOTAL_DAMAGE_MARKERS = 24

    def __init__(self):
        self.cells = []
        self.pois = []
        self.doors = []
        self.exits = []

        # Estado dinámico de cada celda: CLEAR, SMOKE o FIRE.
        self.cell_states = []

        # Marcadores de daño por pared. La llave es la arista
        # entre dos celdas, normalizada para que dé igual el
        # orden en que se consulte.
        self.wall_damage = {}

        # Total de marcadores de daño colocados en la partida.
        # Cuando se agotan, el edificio colapsa.
        self.damage_markers = 0

    def load_from_file(self, file_path):
        path = Path(file_path)

        if not path.exists():
            raise FileNotFoundError(
                f"No se encontró el archivo: {file_path}"
            )

        with open(path, "r", encoding="utf-8") as file:
            lines = [
                line.strip()
                for line in file.readlines()
                if line.strip()
            ]

        self._load_cells(lines)
        self._load_states(lines)
        self._load_pois(lines)
        self._load_doors(lines)
        self._load_exits(lines)

    # =====================================================
    # Celdas y paredes
    # =====================================================

    def _load_cells(self, lines):
        # Cada celda viene como 4 dígitos: arriba, izquierda, abajo,
        # derecha. Un 1 significa pared de ese lado.
        self.cells = []

        for row in range(self.ROWS):
            wall_codes = lines[row].split()

            if len(wall_codes) != self.COLUMNS:
                raise ValueError(
                    f"La fila {row + 1} no contiene "
                    f"{self.COLUMNS} celdas."
                )

            current_row = []

            for column in range(self.COLUMNS):
                code = wall_codes[column]

                if len(code) != 4:
                    raise ValueError(
                        f"Código de pared inválido: {code}"
                    )

                cell = CellData(
                    row=row,
                    column=column,
                    wall_up=code[0] == "1",
                    wall_left=code[1] == "1",
                    wall_down=code[2] == "1",
                    wall_right=code[3] == "1"
                )

                current_row.append(cell)

            self.cells.append(current_row)

    # =====================================================
    # POI
    # =====================================================

    def _load_pois(self, lines):
        self.pois = []

        start = 6
        count = 3

        for i in range(count):
            values = lines[start + i].split()

            row = int(values[0]) - 1
            column = int(values[1]) - 1
            poi_type = values[2]

            self.pois.append(
                POIData(
                    row=row,
                    column=column,
                    poi_type=poi_type
                )
            )

    # =====================================================
    # Fuego inicial
    #
    # El archivo trae las celdas que arrancan con fuego.
    # A partir de ahí el estado de cada celda vive en
    # cell_states y cambia durante la partida.
    # =====================================================

    def _load_states(self, lines):
        self.cell_states = [
            [CLEAR for _ in range(self.COLUMNS)]
            for _ in range(self.ROWS)
        ]

        start = 9
        count = 10

        for i in range(count):
            values = lines[start + i].split()

            row = int(values[0]) - 1
            column = int(values[1]) - 1

            self.cell_states[row][column] = FIRE

    # =====================================================
    # Puertas
    # =====================================================

    def _load_doors(self, lines):
        self.doors = []

        start = 19
        count = 8

        for i in range(count):
            values = lines[start + i].split()

            row1 = int(values[0]) - 1
            column1 = int(values[1]) - 1
            row2 = int(values[2]) - 1
            column2 = int(values[3]) - 1

            self.doors.append(
                DoorData(
                    row1=row1,
                    column1=column1,
                    row2=row2,
                    column2=column2
                )
            )

    # =====================================================
    # Entradas / salidas
    # =====================================================

    def _load_exits(self, lines):
        self.exits = []

        start = 27
        count = 4

        for i in range(count):
            values = lines[start + i].split()

            row = int(values[0]) - 1
            column = int(values[1]) - 1

            self.exits.append(
                ExitData(
                    row=row,
                    column=column
                )
            )

    # =====================================================
    # Consultas básicas
    # =====================================================

    def is_inside(self, row, column):
        return (
            0 <= row < self.ROWS
            and 0 <= column < self.COLUMNS
        )

    def is_exit(self, row, column):
        for exit_point in self.exits:
            if exit_point.row == row and exit_point.column == column:
                return True

        return False

    # =====================================================
    # Estado de las celdas: humo y fuego
    # =====================================================

    def get_state(self, row, column):
        if not self.is_inside(row, column):
            return CLEAR

        return self.cell_states[row][column]

    def has_fire(self, row, column):
        return self.get_state(row, column) == FIRE

    def has_smoke(self, row, column):
        return self.get_state(row, column) == SMOKE

    def is_clear(self, row, column):
        return self.get_state(row, column) == CLEAR

    def set_state(self, row, column, state):
        if self.is_inside(row, column):
            self.cell_states[row][column] = state

    @property
    def fires(self):
        """Celdas que en este momento tienen fuego.

        Se calcula cada vez a partir de cell_states (48 celdas, es
        barato) para que nunca haya dos copias del estado que se
        puedan desincronizar. Lo mismo para smokes.
        """
        result = []

        for row in range(self.ROWS):
            for column in range(self.COLUMNS):
                if self.cell_states[row][column] == FIRE:
                    result.append(
                        FireData(row=row, column=column)
                    )

        return result

    @property
    def smokes(self):
        """Celdas que en este momento tienen humo."""
        result = []

        for row in range(self.ROWS):
            for column in range(self.COLUMNS):
                if self.cell_states[row][column] == SMOKE:
                    result.append(
                        FireData(row=row, column=column)
                    )

        return result

    def count_fires(self):
        return len(self.fires)

    # =====================================================
    # Paredes y daño estructural
    #
    # Cada pared se identifica por la arista entre las dos
    # celdas que separa. Las paredes del borde del edificio
    # también se pueden dañar, así que su "vecino" queda
    # fuera del tablero y eso es válido como identificador.
    # =====================================================

    def _wall_key(self, row1, column1, row2, column2):
        # La pared entre A y B es la misma que entre B y A: se ordenan
        # las dos celdas para que la llave del diccionario sea única.
        return tuple(
            sorted([(row1, column1), (row2, column2)])
        )

    def get_wall_damage(self, row1, column1, row2, column2):
        key = self._wall_key(row1, column1, row2, column2)

        return self.wall_damage.get(key, 0)

    def is_wall_destroyed(self, row1, column1, row2, column2):
        damage = self.get_wall_damage(
            row1,
            column1,
            row2,
            column2
        )

        return damage >= self.DAMAGE_TO_DESTROY_WALL

    def has_wall_between(self, row1, column1, row2, column2):
        """Indica si hay pared entre dos celdas adyacentes.

        Se revisan los dos lados de la arista. El archivo de
        entrada debería ser simétrico, pero si no lo fuera
        basta con que cualquiera de las dos celdas declare la
        pared para que exista.
        """
        delta_row = row2 - row1
        delta_column = column2 - column1

        current = None

        if self.is_inside(row1, column1):
            current = self.cells[row1][column1]

        target = None

        if self.is_inside(row2, column2):
            target = self.cells[row2][column2]

        if delta_row == -1:
            side = current.wall_up if current else False
            other = target.wall_down if target else False

        elif delta_row == 1:
            side = current.wall_down if current else False
            other = target.wall_up if target else False

        elif delta_column == -1:
            side = current.wall_left if current else False
            other = target.wall_right if target else False

        elif delta_column == 1:
            side = current.wall_right if current else False
            other = target.wall_left if target else False

        else:
            return False

        return side or other

    def add_wall_damage(self, row1, column1, row2, column2):
        """Coloca un marcador de daño en una pared.

        Devuelve True si el marcador se colocó. Si la pared ya
        estaba destruida no se coloca nada, tal como en las
        reglas del juego. Tampoco se coloca si ya se agotaron
        los 24 marcadores disponibles.
        """
        if self.damage_markers >= self.TOTAL_DAMAGE_MARKERS:
            return False

        if self.is_wall_destroyed(row1, column1, row2, column2):
            return False

        key = self._wall_key(row1, column1, row2, column2)

        self.wall_damage[key] = self.wall_damage.get(key, 0) + 1
        self.damage_markers += 1

        return True

    # =====================================================
    # Puertas
    # =====================================================

    def get_door_between(self, row1, column1, row2, column2):
        for door in self.doors:
            same_direction = (
                door.row1 == row1
                and door.column1 == column1
                and door.row2 == row2
                and door.column2 == column2
            )

            opposite_direction = (
                door.row1 == row2
                and door.column1 == column2
                and door.row2 == row1
                and door.column2 == column1
            )

            if same_direction or opposite_direction:
                return door

        return None

    def has_door_between(self, row1, column1, row2, column2):
        return (
            self.get_door_between(
                row1,
                column1,
                row2,
                column2
            )
            is not None
        )

    def destroy_door(self, door):
        """Una explosión revienta la puerta.

        Una puerta destruida se comporta como una pared
        destruida: deja pasar bomberos y fuego.
        """
        door.is_destroyed = True
        door.is_open = True

    # =====================================================
    # Movimiento
    # =====================================================

    def can_move_between(self, row1, column1, row2, column2):
        """Regla única de paso entre dos celdas vecinas.

        La usan el movimiento de los bomberos, la extinción a distancia
        de una celda, el flashover y A*, así que aquí vive la verdad
        sobre qué separa a dos celdas. Orden de decisión: fuera del
        tablero no se pasa; si hay puerta manda la puerta; si no, manda
        la pared y su daño.
        """
        if not self.is_inside(row2, column2):
            return False

        if not self.is_inside(row1, column1):
            return False

        delta_row = row2 - row1
        delta_column = column2 - column1

        if abs(delta_row) + abs(delta_column) != 1:
            return False

        # Una puerta manda sobre la pared: si hay puerta, lo
        # que decide es si está abierta o destruida.
        door = self.get_door_between(
            row1,
            column1,
            row2,
            column2
        )

        if door is not None:
            return door.is_open or door.is_destroyed

        # Sin puerta, pasa si no hay pared o si la pared ya
        # acumuló daño suficiente para quedar destruida.
        if not self.has_wall_between(row1, column1, row2, column2):
            return True

        return self.is_wall_destroyed(
            row1,
            column1,
            row2,
            column2
        )

    def get_valid_neighbors(self, row, column):
        """Celdas vecinas a las que se puede pasar ahora mismo.

        Es la base del catálogo de movimientos del bombero y de los
        vecinos de A* (que además considera puertas cerradas).
        """
        directions = [
            (-1, 0),
            (1, 0),
            (0, -1),
            (0, 1),
        ]

        neighbors = []

        for delta_row, delta_column in directions:
            new_row = row + delta_row
            new_column = column + delta_column

            if self.can_move_between(
                row,
                column,
                new_row,
                new_column
            ):
                neighbors.append(
                    (new_row, new_column)
                )

        return neighbors

    def get_movement_cost(self, row, column):
        """Costo en AP de entrar a una celda.

        Entrar a una celda con fuego cuesta 2 AP. El humo no
        encarece el movimiento, solo el fuego.
        """
        if self.has_fire(row, column):
            return 2

        return 1

    def get_affordable_neighbors(self, row, column, action_points):
        neighbors = self.get_valid_neighbors(
            row,
            column
        )

        affordable = []

        for new_row, new_column in neighbors:
            cost = self.get_movement_cost(
                new_row,
                new_column
            )

            if cost <= action_points:
                affordable.append(
                    (new_row, new_column)
                )

        return affordable

    def get_adjacent_closed_doors(self, row, column):
        directions = [
            (-1, 0),
            (1, 0),
            (0, -1),
            (0, 1),
        ]

        doors = []

        for delta_row, delta_column in directions:
            new_row = row + delta_row
            new_column = column + delta_column

            if not self.is_inside(new_row, new_column):
                continue

            door = self.get_door_between(
                row,
                column,
                new_row,
                new_column
            )

            if door is None:
                continue

            if door.is_destroyed:
                continue

            if not door.is_open:
                doors.append(door)

        return doors

    def get_adjacent_walls(self, row, column):
        """Paredes que se pueden cortar desde esta celda.

        Devuelve la celda del otro lado de cada pared que
        todavía no está destruida, incluyendo las paredes
        exteriores del edificio.
        """
        directions = [
            (-1, 0),
            (1, 0),
            (0, -1),
            (0, 1),
        ]

        walls = []

        for delta_row, delta_column in directions:
            new_row = row + delta_row
            new_column = column + delta_column

            if self.get_door_between(
                row,
                column,
                new_row,
                new_column
            ) is not None:
                continue

            if not self.has_wall_between(
                row,
                column,
                new_row,
                new_column
            ):
                continue

            if self.is_wall_destroyed(
                row,
                column,
                new_row,
                new_column
            ):
                continue

            walls.append((new_row, new_column))

        return walls

    # =====================================================
    # Puntos de interés
    # =====================================================

    def get_poi_at(self, row, column):
        for poi in self.pois:
            if poi.row == row and poi.column == column:
                return poi

        return None

    def remove_poi(self, poi):
        if poi in self.pois:
            self.pois.remove(poi)

    def count_hidden_pois(self):
        """POI que siguen boca abajo sobre el tablero."""
        return len(
            [poi for poi in self.pois if not poi.revealed]
        )
