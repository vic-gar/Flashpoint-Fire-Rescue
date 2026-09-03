from dataclasses import dataclass
from pathlib import Path


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
# =========================================================

@dataclass
class POIData:
    row: int
    column: int
    poi_type: str

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

    def __init__(self):
        self.cells = []
        self.pois = []
        self.fires = []
        self.doors = []
        self.exits = []

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
        self._load_pois(lines)
        self._load_fires(lines)
        self._load_doors(lines)
        self._load_exits(lines)

    # =====================================================
    # Celdas y paredes
    # =====================================================

    def _load_cells(self, lines):
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
    # =====================================================

    def _load_fires(self, lines):
        self.fires = []

        start = 9
        count = 10

        for i in range(count):
            values = lines[start + i].split()

            row = int(values[0]) - 1
            column = int(values[1]) - 1

            self.fires.append(
                FireData(
                    row=row,
                    column=column
                )
            )

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
            
    def is_inside(self, row, column):
        return (
            0 <= row < self.ROWS
            and 0 <= column < self.COLUMNS
        )

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
    
    def can_move_between(self, row1, column1, row2, column2):
        if not self.is_inside(row2, column2):
            return False

        delta_row = row2 - row1
        delta_column = column2 - column1

        if abs(delta_row) + abs(delta_column) != 1:
            return False

        current = self.cells[row1][column1]
        target = self.cells[row2][column2]

        # Arriba
        if delta_row == -1:
            door = self.get_door_between(
                row1,
                column1,
                row2,
                column2
            )

            if door is not None:
                return door.is_open

            return not current.wall_up

        # Abajo
        if delta_row == 1:
            door = self.get_door_between(
                row1,
                column1,
                row2,
                column2
            )

            if door is not None:
                return door.is_open

            return not current.wall_down

        # Izquierda
        if delta_column == -1:
            door = self.get_door_between(
                row1,
                column1,
                row2,
                column2
            )

            if door is not None:
                return door.is_open

            return not current.wall_left

        # Derecha
        if delta_column == 1:
            door = self.get_door_between(
                row1,
                column1,
                row2,
                column2
            )

            if door is not None:
                return door.is_open

            return not current.wall_right

        return False
    
    def get_valid_neighbors(self, row, column):
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
    
    def has_fire(self, row, column):
        for fire in self.fires:
            if fire.row == row and fire.column == column:
                return True

        return False
    
    def get_movement_cost(self, row, column):
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

            if door is not None and not door.is_open:
                doors.append(door)

        return doors