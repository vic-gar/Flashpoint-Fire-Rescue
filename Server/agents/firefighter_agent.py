"""Agente bombero: acciones posibles y su ejecución.

Responsabilidad: saber qué puede hacer un bombero con los AP que tiene
(get_legal_actions) y aplicar la acción elegida sobre el tablero
(execute_action). El bombero NO decide: la decisión la toma la
estrategia activa (strategies/), que recibe el catálogo y devuelve una
acción. Así la aleatoria y la mejorada juegan con las mismas reglas.

Se conecta con: FlashPointModel._take_actions (quien pide la decisión
y ejecuta), model/board.py (para consultar y cambiar el tablero) y
run_batch.py (que lee los contadores por bombero como métricas).

Origen: constructor, reset_action_points, move_to, open_door y la idea
del catálogo de acciones al azar son del agente original de Víctor. AP
guardados, víctimas, extinción, paredes, el catálogo completo de
acciones legales y las métricas se agregaron en la Fase 1 con apoyo de
Claude y se revisan con tests/test_rules.py.

Formato de las acciones (tuplas, la primera posición dice cuál es):
    ("move", fila, columna)
    ("pick_up",)
    ("open_door", puerta)
    ("extinguish", fila, columna, completo)   completo=True apaga del todo
    ("chop", fila, columna)                   pared hacia esa celda
"""

import mesa

from model.board import CLEAR, SMOKE, FIRE


class FirefighterAgent(mesa.Agent):
    """Bombero de Flash Point.

    Cada turno recibe 4 puntos de acción (AP) y los gasta en
    moverse, abrir puertas, apagar fuego o humo, cortar paredes
    y sacar víctimas del edificio.

    Los AP que no gasta se guardan para el siguiente turno, con
    un máximo acumulado de 4 tal como indica el reglamento.
    """

    # Costos del reglamento Family. NO MODIFICAR SIN REVISAR: cambian el
    # catálogo de acciones y con él los resultados de los experimentos.
    ACTION_POINTS_PER_TURN = 4
    MAX_SAVED_ACTION_POINTS = 4

    COST_OPEN_DOOR = 1
    COST_REMOVE_SMOKE = 1
    COST_FIRE_TO_SMOKE = 1
    COST_REMOVE_FIRE = 2
    COST_CHOP_WALL = 2
    COST_CARRY_MOVE = 2

    def __init__(self, model, firefighter_id, row, column):
        super().__init__(model)

        self.firefighter_id = firefighter_id

        # La posición del bombero vive en el MultiGrid de Mesa
        # (model.grid). row y column son propiedades que la leen, así
        # que no hay dos copias que se puedan desincronizar.
        # Mesa deja self.pos en None hasta que se coloca al agente.
        model.grid.place_agent(self, (column, row))

        # Arranca en cero porque el primer turno los asigna
        # reset_action_points, igual que todos los demás.
        self.action_points = 0

        self.carrying_victim = False

        # Métricas por agente, para poder comparar estrategias
        # después sin tener que reconstruirlas del log.
        self.cells_moved = 0
        self.action_points_spent = 0
        self.action_points_wasted = 0
        self.rescues = 0
        self.fires_extinguished = 0
        self.smokes_extinguished = 0
        self.walls_chopped = 0

    # =====================================================
    # Posición: una sola fuente de verdad
    #
    # Mesa guarda la posición en self.pos como (x, y), donde x es la
    # columna (ancho del grid) e y es la fila (alto). El resto del
    # proyecto trabaja con (fila, columna), así que row y column son
    # propiedades que traducen. Al ser derivadas de self.pos no pueden
    # quedar desincronizadas con el MultiGrid.
    #
    # NO MODIFICAR SIN REVISAR: el orden (x, y) = (columna, fila) es el
    # de Mesa, verificado contra mesa.space en la versión instalada.
    # =====================================================

    @property
    def row(self):
        return self.pos[1]

    @row.setter
    def row(self, value):
        self.set_cell(value, self.pos[0])

    @property
    def column(self):
        return self.pos[0]

    @column.setter
    def column(self, value):
        self.set_cell(self.pos[1], value)

    def set_cell(self, row, column):
        """Mueve al bombero a una celda dentro del MultiGrid.

        move_agent de Mesa quita al agente de su celda anterior y lo
        coloca en la nueva, así que la ocupación del grid queda
        correcta sin trabajo extra. No cuesta AP ni valida reglas: es
        solo el cambio de posición. Las reglas las aplican move_to y
        la fase del fuego.
        """
        self.model.grid.move_agent(self, (column, row))

    def reset_action_points(self):
        """Arranca el turno sumando los AP guardados del anterior."""
        saved = min(
            max(self.action_points, 0),
            self.MAX_SAVED_ACTION_POINTS
        )

        self.action_points = self.ACTION_POINTS_PER_TURN + saved

    def __repr__(self):
        return (
            f"FirefighterAgent("
            f"id={self.firefighter_id}, "
            f"row={self.row + 1}, "
            f"column={self.column + 1}, "
            f"AP={self.action_points})"
        )

    # =====================================================
    # Movimiento
    # =====================================================

    def get_move_cost(self, row, column):
        """Costo de moverse a una celda adyacente.

        Cargando una víctima el movimiento siempre cuesta 2 AP y
        no se puede entrar a una celda con fuego.
        """
        board = self.model.board

        if self.carrying_victim:
            if board.has_fire(row, column):
                return None

            return self.COST_CARRY_MOVE

        return board.get_movement_cost(row, column)

    def move_to(self, row, column):
        board = self.model.board

        if not board.can_move_between(
            self.row,
            self.column,
            row,
            column
        ):
            return False

        cost = self.get_move_cost(row, column)

        if cost is None:
            return False

        if self.action_points < cost:
            return False

        # Un solo movimiento en el MultiGrid en vez de dos asignaciones.
        self.set_cell(row, column)

        self._spend(cost)
        self.cells_moved += 1

        self._on_enter_cell()

        return True

    def _on_enter_cell(self):
        """Efectos automáticos al llegar a una celda.

        Entrar a una celda con un POI lo revela sin costo, y
        llegar a una salida cargando una víctima la rescata.
        """
        board = self.model.board

        poi = board.get_poi_at(self.row, self.column)

        if poi is not None and not poi.revealed:
            self._reveal_poi(poi)

        if self.carrying_victim and board.is_exit(self.row, self.column):
            self._rescue_victim()

    def _reveal_poi(self, poi):
        poi.revealed = True

        if poi.is_false_alarm:
            # Una falsa alarma se retira del tablero de inmediato.
            self.model.board.remove_poi(poi)
            self.model.false_alarms_revealed += 1

        else:
            self.model.victims_revealed += 1

    def _rescue_victim(self):
        self.carrying_victim = False

        self.rescues += 1
        self.model.victims_rescued += 1

    # =====================================================
    # Cargar víctimas
    # =====================================================

    def can_pick_up_victim(self):
        if self.carrying_victim:
            return False

        poi = self.model.board.get_poi_at(self.row, self.column)

        return (
            poi is not None
            and poi.revealed
            and poi.is_victim
        )

    def pick_up_victim(self):
        """Cargar una víctima revelada no cuesta AP.

        En el reglamento el costo está en moverse cargándola,
        no en levantarla.
        """
        if not self.can_pick_up_victim():
            return False

        poi = self.model.board.get_poi_at(self.row, self.column)

        self.model.board.remove_poi(poi)
        self.carrying_victim = True

        # Si el bombero ya estaba en una salida, la víctima queda
        # rescatada sin necesidad de dar un paso más.
        if self.model.board.is_exit(self.row, self.column):
            self._rescue_victim()

        return True

    # =====================================================
    # Puertas
    # =====================================================

    def open_door(self, door):
        if self.action_points < self.COST_OPEN_DOOR:
            return False

        if door.is_open or door.is_destroyed:
            return False

        door.is_open = True
        self._spend(self.COST_OPEN_DOOR)

        return True

    # =====================================================
    # Apagar fuego y humo
    # =====================================================

    def get_extinguishable_cells(self):
        """Celda propia y adyacentes accesibles con fuego o humo."""
        board = self.model.board

        cells = []

        if board.get_state(self.row, self.column) != CLEAR:
            cells.append((self.row, self.column))

        # Vecindad ortogonal del MultiGrid filtrada por paredes y
        # puertas. Apagar a distancia usa la misma adyacencia que
        # moverse, igual que en el reglamento.
        for row, column in self.model.movement_neighbors(self.row, self.column):
            if board.get_state(row, column) != CLEAR:
                cells.append((row, column))

        return cells

    def extinguish(self, row, column, full=True):
        """Apaga fuego o humo en la celda indicada.

        Quitar humo cuesta 1 AP. Sobre el fuego hay dos opciones:
        bajarlo a humo por 1 AP o apagarlo del todo por 2 AP.
        Conviene apagarlo del todo porque el humo se reaviva.
        """
        board = self.model.board

        if (row, column) != (self.row, self.column):
            if not board.can_move_between(
                self.row,
                self.column,
                row,
                column
            ):
                return False

        state = board.get_state(row, column)

        if state == SMOKE:
            if self.action_points < self.COST_REMOVE_SMOKE:
                return False

            board.set_state(row, column, CLEAR)
            self._spend(self.COST_REMOVE_SMOKE)
            self.smokes_extinguished += 1

            return True

        if state == FIRE:
            if full:
                if self.action_points < self.COST_REMOVE_FIRE:
                    return False

                board.set_state(row, column, CLEAR)
                self._spend(self.COST_REMOVE_FIRE)
                self.fires_extinguished += 1

                return True

            if self.action_points < self.COST_FIRE_TO_SMOKE:
                return False

            board.set_state(row, column, SMOKE)
            self._spend(self.COST_FIRE_TO_SMOKE)
            self.fires_extinguished += 1

            return True

        return False

    # =====================================================
    # Cortar paredes
    # =====================================================

    def chop_wall(self, row, column):
        """Coloca un marcador de daño en una pared adyacente.

        Con dos marcadores la pared queda destruida y se puede
        atravesar. Cada marcador acerca al edificio al colapso.
        """
        board = self.model.board

        if self.action_points < self.COST_CHOP_WALL:
            return False

        if not board.has_wall_between(self.row, self.column, row, column):
            return False

        if board.is_wall_destroyed(self.row, self.column, row, column):
            return False

        if not board.add_wall_damage(self.row, self.column, row, column):
            return False

        self._spend(self.COST_CHOP_WALL)
        self.walls_chopped += 1

        return True

    # =====================================================
    # Catálogo de acciones legales
    # =====================================================

    def get_legal_actions(self):
        """Todas las acciones que el bombero puede pagar ahora.

        Es la base tanto de la estrategia aleatoria como de la
        mejorada: ninguna estrategia debe poder elegir una acción
        que no esté en esta lista. Por eso las reglas del juego se
        cumplen aunque la estrategia esté mal pensada.

        El orden de la lista es fijo (movimientos, cargar, puertas,
        extinción, paredes) para que la estrategia aleatoria sea
        reproducible con la misma semilla.
        """
        board = self.model.board

        actions = []

        # Candidatos de movimiento: MultiGrid con moore=False (sin
        # diagonales) y Board filtrando paredes y puertas.
        for row, column in self.model.movement_neighbors(self.row, self.column):
            cost = self.get_move_cost(row, column)

            if cost is None or cost > self.action_points:
                continue

            # El reglamento no deja terminar el turno sobre una
            # celda con fuego. Entrar solo tiene sentido si además
            # quedan AP para volver a salir.
            if board.has_fire(row, column):
                if self.action_points < cost + 1:
                    continue

            actions.append(("move", row, column))

        if self.can_pick_up_victim():
            actions.append(("pick_up",))

        if self.action_points >= self.COST_OPEN_DOOR:
            for door in board.get_adjacent_closed_doors(self.row, self.column):
                actions.append(("open_door", door))

        # Sobre fuego con 2 AP o más solo se ofrece apagarlo del todo;
        # bajarlo a humo queda para cuando solo queda 1 AP.
        for row, column in self.get_extinguishable_cells():
            state = board.get_state(row, column)

            if state == SMOKE and self.action_points >= self.COST_REMOVE_SMOKE:
                actions.append(("extinguish", row, column, True))

            elif state == FIRE and self.action_points >= self.COST_REMOVE_FIRE:
                actions.append(("extinguish", row, column, True))

            elif state == FIRE and self.action_points >= self.COST_FIRE_TO_SMOKE:
                actions.append(("extinguish", row, column, False))

        if self.action_points >= self.COST_CHOP_WALL:
            for row, column in board.get_adjacent_walls(self.row, self.column):
                actions.append(("chop", row, column))

        return actions

    def execute_action(self, action):
        """Ejecuta una acción del catálogo y dice si tuvo efecto."""
        kind = action[0]

        if kind == "move":
            return self.move_to(action[1], action[2])

        if kind == "pick_up":
            return self.pick_up_victim()

        if kind == "open_door":
            return self.open_door(action[1])

        if kind == "extinguish":
            return self.extinguish(action[1], action[2], action[3])

        if kind == "chop":
            return self.chop_wall(action[1], action[2])

        return False

    # =====================================================
    # Interno
    # =====================================================

    def _spend(self, cost):
        self.action_points -= cost
        self.action_points_spent += cost
