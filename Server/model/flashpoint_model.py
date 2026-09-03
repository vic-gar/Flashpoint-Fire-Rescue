import mesa

from model.board import Board
from agents.firefighter_agent import FirefighterAgent


class FlashPointModel(mesa.Model):
    NUM_FIREFIGHTERS = 6

    def __init__(self, board_file="data/final.txt", rng=None):
        super().__init__(rng=rng)

        self.board = Board()
        self.board.load_from_file(board_file)

        self.firefighters = []

        self.current_firefighter_index = 0

        self.running = True

        self._create_firefighters()

    def _create_firefighters(self):
        if len(self.board.exits) < 4:
            raise ValueError(
                "Se requieren al menos 4 entradas/salidas "
                "para inicializar los bomberos."
            )

        starting_positions = [
            self.board.exits[0],
            self.board.exits[0],
            self.board.exits[1],
            self.board.exits[1],
            self.board.exits[2],
            self.board.exits[3],
        ]

        for i in range(self.NUM_FIREFIGHTERS):
            position = starting_positions[i]

            firefighter = FirefighterAgent(
                model=self,
                firefighter_id=i + 1,
                row=position.row,
                column=position.column
            )

            self.firefighters.append(firefighter)

    @property
    def current_firefighter(self):
        return self.firefighters[
            self.current_firefighter_index
        ]

    def start_turn(self):
        firefighter = self.current_firefighter

        firefighter.reset_action_points()

        print(
            f"\nInicio turno Bombero "
            f"{firefighter.firefighter_id}"
        )

        print(
            f"Posición: "
            f"({firefighter.row + 1}, "
            f"{firefighter.column + 1})"
        )

        print(
            f"AP disponibles: "
            f"{firefighter.action_points}"
        )

    def end_turn(self):
        firefighter = self.current_firefighter

        print(
            f"Fin turno Bombero "
            f"{firefighter.firefighter_id}"
        )

        self.current_firefighter_index += 1

        if (
            self.current_firefighter_index
            >= len(self.firefighters)
        ):
            self.current_firefighter_index = 0

        self.start_turn()