import mesa


class FirefighterAgent(mesa.Agent):
    def __init__(self, model, firefighter_id, row, column):
        super().__init__(model)

        self.firefighter_id = firefighter_id

        self.row = row
        self.column = column

        self.action_points = 4

        self.carrying_victim = False

    def reset_action_points(self):
        self.action_points = 4

    def __repr__(self):
        return (
            f"FirefighterAgent("
            f"id={self.firefighter_id}, "
            f"row={self.row + 1}, "
            f"column={self.column + 1}, "
            f"AP={self.action_points})"
        )
        
    def move_to(self, row, column):
        board = self.model.board

        if not board.can_move_between(
            self.row,
            self.column,
            row,
            column
        ):
            print(
                f"Bombero {self.firefighter_id}: "
                f"movimiento inválido."
            )
            return False

        movement_cost = board.get_movement_cost(
            row,
            column
        )

        if self.action_points < movement_cost:
            print(
                f"Bombero {self.firefighter_id}: "
                f"AP insuficientes."
            )
            return False

        old_row = self.row
        old_column = self.column

        self.row = row
        self.column = column

        self.action_points -= movement_cost

        print(
            f"Bombero {self.firefighter_id}: "
            f"({old_row + 1},{old_column + 1}) "
            f"-> ({row + 1},{column + 1}) | "
            f"Costo: {movement_cost} AP | "
            f"Restantes: {self.action_points}"
        )

        return True
    
    def random_move(self):
        board = self.model.board

        neighbors = board.get_affordable_neighbors(
            self.row,
            self.column,
            self.action_points
        )

        if not neighbors:
            print(
                f"Bombero {self.firefighter_id}: "
                f"no tiene movimientos disponibles."
            )
            return False

        target_row, target_column = (
            self.model.random.choice(neighbors)
        )

        return self.move_to(
            target_row,
            target_column
        )
            
    def open_door(self, door):
        if self.action_points < 1:
            return False

        if door.is_open:
            return False

        door.is_open = True
        self.action_points -= 1

        print(
            f"Bombero {self.firefighter_id}: "
            f"abrió puerta entre "
            f"({door.row1 + 1},{door.column1 + 1}) y "
            f"({door.row2 + 1},{door.column2 + 1}) | "
            f"Costo: 1 AP | "
            f"Restantes: {self.action_points}"
        )

        return True
    
    def random_open_door(self):
        doors = self.model.board.get_adjacent_closed_doors(
            self.row,
            self.column
        )

        if not doors:
            return False

        door = self.model.random.choice(doors)

        return self.open_door(door)
    
    def get_random_actions(self):
        actions = []

        board = self.model.board

        # -----------------------------------------
        # Movimientos posibles
        # -----------------------------------------

        neighbors = board.get_affordable_neighbors(
            self.row,
            self.column,
            self.action_points
        )

        for row, column in neighbors:
            actions.append(
                ("move", row, column)
            )

        # -----------------------------------------
        # Puertas cerradas adyacentes
        # -----------------------------------------

        if self.action_points >= 1:
            doors = board.get_adjacent_closed_doors(
                self.row,
                self.column
            )

            for door in doors:
                actions.append(
                    ("open_door", door)
                )

        return actions
    
    def execute_random_action(self):
        actions = self.get_random_actions()

        if not actions:
            print(
                f"Bombero {self.firefighter_id}: "
                f"no tiene acciones válidas."
            )
            return False

        action = self.model.random.choice(actions)

        action_type = action[0]

        if action_type == "move":
            row = action[1]
            column = action[2]

            return self.move_to(
                row,
                column
            )

        if action_type == "open_door":
            door = action[1]

            return self.open_door(door)

        return False
    
    def random_turn(self):
        print(
            f"\n--- Turno aleatorio Bombero "
            f"{self.firefighter_id} ---"
        )

        while self.action_points > 0:
            executed = self.execute_random_action()

            if not executed:
                break