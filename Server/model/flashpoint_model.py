"""Modelo Mesa de la partida: turnos, fuego, POI y fin del juego.

Responsabilidad: es el "dueño" de la partida. Junta el tablero
(model/board.py), los bomberos (agents/firefighter_agent.py), la fase
del fuego (model/fire_phase.py) y la estrategia activa (strategies/).
Un paso del modelo es el turno completo de un bombero.

Se conecta con:
  - server.py, que llama step() en cada POST /step y manda get_state()
    a Unity;
  - run_batch.py y main.py, que corren partidas completas con run_game();
  - las estrategias, que reciben (model, firefighter) y devuelven una
    acción del catálogo del bombero.

Origen: el esqueleto (constructor con rng, colocación de los seis
bomberos en las salidas y el bombero en turno) es de Víctor. Las reglas
del juego, la fase del fuego, los POI, las condiciones de fin, el
DataCollector, la estrategia intercambiable y la separación de RNG se
desarrollaron con apoyo de Claude y se revisan con tests/test_rules.py.

COMPATIBILIDAD CON UNITY: las llaves de get_state() y
get_board_config() deben coincidir con los campos de
Assets/Scripts/Data/SimulationState.cs. Hay una prueba que lo verifica.
"""

import random

import mesa

from mesa.datacollection import DataCollector
from mesa.space import MultiGrid

from model.board import Board, CLEAR, SMOKE, FIRE
from model import fire_phase


# Resultados posibles de una partida.
# COMPATIBILIDAD CON UNITY: SimulationClient.cs compara el texto
# "en_curso" para saber si la partida sigue.
RUNNING = "en_curso"
WIN = "victoria"
LOSS_VICTIMS = "derrota_victimas"
LOSS_COLLAPSE = "derrota_colapso"


class FlashPointModel(mesa.Model):
    """Simulación del juego de mesa Flash Point: Fire Rescue.

    Sigue las reglas del "Family Game Setup": seis bomberos,
    diez víctimas, cinco falsas alarmas y veinticuatro
    marcadores de daño.

    Un paso del modelo equivale al turno completo de un bombero:
    primero gasta sus puntos de acción, después avanza el fuego y
    al final se reponen los puntos de interés del tablero.

    Generadores aleatorios (ver _create_random_generators):
      fire_random      dados del fuego
      poi_random       mazo y colocación de POI
      strategy_random  decisiones al azar de las estrategias
    """

    NUM_FIREFIGHTERS = 6

    TOTAL_VICTIMS = 10
    TOTAL_FALSE_ALARMS = 5

    POIS_ON_BOARD = 3

    VICTIMS_TO_WIN = 7
    VICTIMS_TO_LOSE = 4

    # PENDIENTE DE CONFIRMAR: el documento del reto es ambiguo entre 24 y
    # 25 marcadores de daño. El reglamento del juego trae 24 contadores y
    # termina la partida cuando se agotan, así que se usa 24. Es una
    # constante para cambiarla si los profesores aclaran otra cosa.
    MAX_DAMAGE = 24

    # Tope de seguridad por si una estrategia nunca termina la partida.
    MAX_STEPS = 500

    def __init__(
        self,
        board_file="data/final.txt",
        strategy=None,
        seed=None,
        verbose=False
    ):
        # rng en vez de seed: es lo que pide Mesa 3.5 y lo que usaba Victor.
        # Mesa crea con esto self.random y self.rng. En este proyecto NO se
        # usan: toda la aleatoriedad pasa por los tres generadores de
        # _create_random_generators, para saber siempre quién tira qué.
        super().__init__(rng=seed)

        self.seed = seed
        self._create_random_generators(seed)

        self.board = Board()
        self.board.load_from_file(board_file)

        # Espacio oficial de Mesa para los bomberos. Se elige MultiGrid
        # y no SingleGrid porque el reto pide que "puede haber más de un
        # agente por celda", que es el mismo criterio que usa el
        # profesor en el notebook MoneyModel para elegir esta clase.
        #
        # Mesa ordena el grid como (ancho, alto) = (columnas, filas).
        # torus=False: el tablero es un edificio, no se sale por un lado
        # para aparecer en el opuesto.
        self.grid = MultiGrid(
            width=self.board.COLUMNS,
            height=self.board.ROWS,
            torus=False
        )

        self.verbose = verbose

        # La estrategia decide qué acción toma cada bombero.
        # Si no se indica ninguna se juega de forma aleatoria.
        self.strategy = strategy or random_strategy
        self.strategy_name = getattr(
            self.strategy,
            "strategy_name",
            "aleatoria"
        )

        self.firefighters = []
        self.current_firefighter_index = 0

        self.running = True
        self.result = RUNNING

        # Contadores de partida
        self.victims_rescued = 0
        self.victims_lost = 0
        self.victims_revealed = 0
        self.false_alarms_revealed = 0
        self.knock_downs = 0
        self.turns = 0
        self.replanifications = 0

        # Historial de celdas objetivo de cada fase del fuego. Sirve para
        # comprobar que dos partidas con la misma semilla reciben los
        # mismos dados y para depurar una partida.
        self.fire_targets = []

        self._build_poi_deck()
        self._create_firefighters()

        # DataCollector de Mesa (visto en clase): guarda por paso las
        # métricas de la partida y de cada bombero. run_batch.py toma
        # los valores finales de aquí y de los contadores de arriba.
        self.datacollector = DataCollector(
            model_reporters={
                "resultado": lambda m: m.result,
                "rescatadas": lambda m: m.victims_rescued,
                "perdidas": lambda m: m.victims_lost,
                "falsas_alarmas": lambda m: m.false_alarms_revealed,
                "danio": lambda m: m.board.damage_markers,
                "fuegos": lambda m: m.board.count_fires(),
                "humos": lambda m: len(m.board.smokes),
                "derribos": lambda m: m.knock_downs,
                "turnos": lambda m: m.turns,
            },
            agent_reporters={
                "celdas_recorridas": "cells_moved",
                "ap_gastados": "action_points_spent",
                "ap_desperdiciados": "action_points_wasted",
                "rescates": "rescues",
                "fuegos_apagados": "fires_extinguished",
                "paredes_cortadas": "walls_chopped",
            },
        )

        self.datacollector.collect(self)

    # =====================================================
    # Preparación
    # =====================================================

    def _create_random_generators(self, seed):
        """Tres generadores independientes, uno por propósito.

        RNG del entorno: fire_random (dados del fuego) y poi_random
        (mazo y colocación de POI). Solo controlan eventos del juego.
        RNG de estrategia: strategy_random. Solo lo usan las
        estrategias que deciden al azar (random_strategy.py).

        Se separan para que la comparación entre estrategias sea justa:
        con la misma semilla, la aleatoria y la mejorada reciben
        exactamente los mismos dados del fuego y el mismo mazo de POI,
        sin importar cuántas decisiones al azar tome cada una. Antes
        todo salía de un solo generador y cada decisión de la aleatoria
        corría la secuencia que después usaba el fuego.

        El fuego y los POI también van separados: reponer POI tira los
        dados un número de veces que depende del tablero (se repite si
        la celda ya tiene POI). Si compartieran generador, la estrategia
        cambiaría de forma indirecta la secuencia del fuego.

        Mecanismo: un generador maestro con la semilla original reparte
        una semilla a cada uno. Misma semilla, mismas tres secuencias.
        """
        master = random.Random(seed)

        self.fire_random = random.Random(master.getrandbits(32))
        self.poi_random = random.Random(master.getrandbits(32))
        self.strategy_random = random.Random(master.getrandbits(32))

    def _build_poi_deck(self):
        """Arma el mazo de POI que queda por salir al tablero.

        Del total de marcadores se descuentan los que el archivo
        de entrada ya colocó sobre el tablero, tal como pide el
        documento del reto.
        """
        victims_on_board = len(
            [poi for poi in self.board.pois if poi.is_victim]
        )

        false_alarms_on_board = len(
            [poi for poi in self.board.pois if poi.is_false_alarm]
        )

        remaining_victims = self.TOTAL_VICTIMS - victims_on_board
        remaining_false = self.TOTAL_FALSE_ALARMS - false_alarms_on_board

        self.poi_deck = (
            ["v"] * remaining_victims
            + ["f"] * remaining_false
        )

        # RNG del entorno: el orden del mazo no depende de la estrategia.
        self.poi_random.shuffle(self.poi_deck)

    def _create_firefighters(self):
        """Coloca los seis bomberos en las entradas del archivo.

        Reparto fijo que dejó Víctor: dos en cada una de las dos primeras
        entradas y uno en las otras dos. El reto no fija dónde arrancan;
        el reglamento solo pide que empiecen fuera del edificio.
        """
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

        # Import local para evitar una dependencia circular entre
        # el modelo y el agente.
        from agents.firefighter_agent import FirefighterAgent

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

    # =====================================================
    # Vecindad de movimiento
    # =====================================================

    # Orden en que se recorren las direcciones: arriba, abajo,
    # izquierda, derecha. Es el mismo que usa Board.get_valid_neighbors.
    #
    # NO MODIFICAR SIN REVISAR: el orden decide el orden del catálogo de
    # acciones legales, y de ahí depende qué elige la estrategia
    # aleatoria con la misma semilla. Cambiarlo cambia los resultados de
    # los experimentos sin cambiar ninguna regla. Mesa devuelve la
    # vecindad ordenada por (x, y), que sería izquierda, arriba, abajo,
    # derecha, y por eso aquí se reordena a la convención del proyecto.
    MOVEMENT_DIRECTIONS = [
        (-1, 0),
        (1, 0),
        (0, -1),
        (0, 1),
    ]

    def movement_neighbors(self, row, column):
        """Celdas a las que se puede mover desde (row, column).

        Dos pasos, y cada uno lo resuelve quien sabe:

        1. MultiGrid da la geometría con moore=False, es decir vecindad
           de Von Neumann: arriba, abajo, izquierda y derecha, sin
           diagonales. Con torus=False los bordes no dan la vuelta, así
           que una esquina no conecta con el extremo opuesto.
        2. Board decide si de verdad se puede pasar, porque en Flash
           Point las paredes y las puertas viven en las aristas entre
           celdas y eso el grid no lo sabe.

        moore=False no es una preferencia nuestra: el reglamento del
        juego define las celdas adyacentes como las de arriba, abajo,
        izquierda y derecha, y dice que las diagonales no son
        adyacentes.

        Devuelve tuplas (fila, columna) para que el resto del proyecto
        no tenga que pensar en el orden (x, y) de Mesa.
        """
        # Conjunto de celdas que el grid considera vecinas ortogonales.
        # Vienen como (x, y) = (columna, fila).
        allowed = set(
            self.grid.get_neighborhood(
                (column, row),
                moore=False,
                include_center=False
            )
        )

        neighbors = []

        for delta_row, delta_column in self.MOVEMENT_DIRECTIONS:
            next_row = row + delta_row
            next_column = column + delta_column

            # Si el grid no la lista, está fuera del tablero.
            if (next_column, next_row) not in allowed:
                continue

            if not self.board.can_move_between(
                row,
                column,
                next_row,
                next_column
            ):
                continue

            neighbors.append((next_row, next_column))

        return neighbors

    # =====================================================
    # Paso de simulación
    # =====================================================

    def step(self):
        """Turno completo de un bombero.

        El orden es el del reglamento: acciones del bombero,
        avance del fuego y reposición de puntos de interés.
        """
        if not self.running:
            return

        firefighter = self.current_firefighter

        self._take_actions(firefighter)

        if self._check_end_conditions():
            self.datacollector.collect(self)
            return

        fire_phase.advance_fire(self)

        if self._check_end_conditions():
            self.datacollector.collect(self)
            return

        self._replenish_pois()

        self.turns += 1

        self._advance_to_next_firefighter()

        self._check_end_conditions()

        self.datacollector.collect(self)

        if self.turns >= self.MAX_STEPS:
            self.running = False

    def _take_actions(self, firefighter):
        """El bombero gasta sus AP según la estrategia activa."""
        firefighter.reset_action_points()

        if self.verbose:
            print(
                f"\nTurno bombero {firefighter.firefighter_id} "
                f"en ({firefighter.row + 1},{firefighter.column + 1}) "
                f"con {firefighter.action_points} AP"
            )

        # Cada bombero tiene un tope de acciones por turno para
        # que una estrategia mal escrita no cuelgue la partida.
        max_actions = 20

        for _ in range(max_actions):
            if firefighter.action_points <= 0:
                break

            action = self.strategy(self, firefighter)

            if action is None:
                break

            executed = firefighter.execute_action(action)

            if not executed:
                break

            if self.verbose:
                print(f"   {self._describe(action)}")

        self._leave_fire_cell(firefighter)
        self._save_action_points(firefighter)

    def _leave_fire_cell(self, firefighter):
        """Un bombero no puede terminar su turno sobre el fuego.

        Si quedó en una celda con fuego se retira a una celda
        adyacente segura. Si no le alcanzan los AP para salir,
        se queda ahí y quedará expuesto al derribo cuando avance
        el fuego, que es la consecuencia natural de la regla.
        """
        board = self.board

        if not board.has_fire(firefighter.row, firefighter.column):
            return

        for row, column in board.get_valid_neighbors(
            firefighter.row,
            firefighter.column
        ):
            if board.has_fire(row, column):
                continue

            cost = firefighter.get_move_cost(row, column)

            if cost is None or cost > firefighter.action_points:
                continue

            firefighter.move_to(row, column)
            return

    def _save_action_points(self, firefighter):
        """Guarda AP para el próximo turno y cuenta los perdidos."""
        leftover = max(firefighter.action_points, 0)

        wasted = max(
            leftover - firefighter.MAX_SAVED_ACTION_POINTS,
            0
        )

        firefighter.action_points_wasted += wasted
        firefighter.action_points = leftover - wasted

    def _advance_to_next_firefighter(self):
        self.current_firefighter_index += 1

        if self.current_firefighter_index >= len(self.firefighters):
            self.current_firefighter_index = 0

    # =====================================================
    # Puntos de interés
    # =====================================================

    def _replenish_pois(self):
        """Repone el tablero hasta tener tres POI boca abajo.

        La celda destino no puede tener fuego ni humo: si los
        tiene, se retiran antes de colocar el marcador. Si ya hay
        un POI ahí se vuelve a tirar.
        """
        attempts = 0

        while (
            self.board.count_hidden_pois() < self.POIS_ON_BOARD
            and self.poi_deck
            and attempts < 100
        ):
            attempts += 1

            # Mismos dados que el fuego (d6 fila, d8 columna) pero con el
            # RNG de POI, para no mover la secuencia del fuego.
            row, column = fire_phase.roll_target(self, self.poi_random)

            if self.board.get_poi_at(row, column) is not None:
                continue

            # El marcador apaga el fuego o el humo de su celda.
            self.board.set_state(row, column, CLEAR)

            poi_type = self.poi_deck.pop()

            from model.board import POIData

            poi = POIData(
                row=row,
                column=column,
                poi_type=poi_type
            )

            self.board.pois.append(poi)

            # Si cae donde ya hay un bombero se revela de una vez.
            for firefighter in self.firefighters:
                same_cell = (
                    firefighter.row == row
                    and firefighter.column == column
                )

                if same_cell:
                    poi.revealed = True

                    if poi.is_false_alarm:
                        self.board.remove_poi(poi)
                        self.false_alarms_revealed += 1
                    else:
                        self.victims_revealed += 1

                    break

    # =====================================================
    # Fin de la partida
    # =====================================================

    def _check_end_conditions(self):
        if self.victims_rescued >= self.VICTIMS_TO_WIN:
            self.result = WIN
            self.running = False
            return True

        if self.victims_lost >= self.VICTIMS_TO_LOSE:
            self.result = LOSS_VICTIMS
            self.running = False
            return True

        if self.board.damage_markers >= self.MAX_DAMAGE:
            self.result = LOSS_COLLAPSE
            self.running = False
            return True

        return False

    def run_game(self):
        """Corre la partida completa y devuelve el resultado."""
        while self.running:
            self.step()

        return self.result

    # =====================================================
    # Utilidades
    # =====================================================

    def _describe(self, action):
        kind = action[0]

        if kind == "move":
            return f"mueve a ({action[1] + 1},{action[2] + 1})"

        if kind == "pick_up":
            return "carga una víctima"

        if kind == "open_door":
            return "abre una puerta"

        if kind == "extinguish":
            objetivo = f"({action[1] + 1},{action[2] + 1})"
            return f"apaga en {objetivo}"

        if kind == "chop":
            return f"corta pared hacia ({action[1] + 1},{action[2] + 1})"

        return kind

    def get_state(self):
        """Estado de la partida listo para enviarse a Unity.

        Es lo que server.py responde en GET /state y POST /step.
        Se arma aquí porque el modelo es el dueño del estado.

        COMPATIBILIDAD CON UNITY: cada llave debe coincidir letra por
        letra con un campo de SimulationState.cs (JsonUtility no avisa
        si un nombre no coincide, solo deja el campo vacío). Filas y
        columnas van desde 0; Unity las usa igual.
        """
        return {
            "resultado": self.result,
            "estrategia": self.strategy_name,
            "turno": self.turns,
            "bombero_actual": self.current_firefighter.firefighter_id,
            "rescatadas": self.victims_rescued,
            "perdidas": self.victims_lost,
            "danio": self.board.damage_markers,
            "bomberos": [
                {
                    "id": f.firefighter_id,
                    "fila": f.row,
                    "columna": f.column,
                    "ap": f.action_points,
                    "cargando": f.carrying_victim,
                }
                for f in self.firefighters
            ],
            "fuegos": [
                {"fila": f.row, "columna": f.column}
                for f in self.board.fires
            ],
            "humos": [
                {"fila": s.row, "columna": s.column}
                for s in self.board.smokes
            ],
            "pois": [
                {
                    "fila": p.row,
                    "columna": p.column,
                    "revelado": p.revealed,
                    "tipo": p.poi_type if p.revealed else "?",
                }
                for p in self.board.pois
            ],
            "puertas": [
                {
                    "fila1": d.row1,
                    "columna1": d.column1,
                    "fila2": d.row2,
                    "columna2": d.column2,
                    "abierta": d.is_open,
                    "destruida": d.is_destroyed,
                }
                for d in self.board.doors
            ],
        }

    def get_board_config(self):
        """Geometría fija del tablero, la que no cambia en la partida.

        Es lo que server.py responde en GET /board. Incluye las
        paredes de cada celda, las puertas y las salidas. Hoy Unity
        construye el tablero leyendo final.txt por su cuenta
        (BoardFileReader.cs de Víctor), así que este endpoint queda
        disponible por si más adelante el tablero cambia en el servidor.
        """
        return {
            "filas": self.board.ROWS,
            "columnas": self.board.COLUMNS,
            "celdas": [
                {
                    "fila": cell.row,
                    "columna": cell.column,
                    "arriba": cell.wall_up,
                    "izquierda": cell.wall_left,
                    "abajo": cell.wall_down,
                    "derecha": cell.wall_right,
                }
                for row in self.board.cells
                for cell in row
            ],
            "puertas": [
                {
                    "fila1": d.row1,
                    "columna1": d.column1,
                    "fila2": d.row2,
                    "columna2": d.column2,
                }
                for d in self.board.doors
            ],
            "salidas": [
                {"fila": e.row, "columna": e.column}
                for e in self.board.exits
            ],
        }


# La estrategia aleatoria vive en strategies/random_strategy.py junto
# con la mejorada. Se importa aquí para seguir siendo el valor por
# defecto del modelo y para que las pruebas la encuentren en este módulo.
from strategies.random_strategy import random_strategy  # noqa: E402
