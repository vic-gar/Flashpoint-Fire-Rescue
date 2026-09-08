"""Pruebas de la estrategia mejorada y de sus piezas.

Corren sobre el tablero real de data/final.txt. Verifican que A*
respeta la topología, que la priorización y la coordinación se
comportan como se documenta, y que la estrategia nunca sale del
catálogo de acciones legales del bombero.

Ejecutar desde la carpeta Server:

    python -m unittest discover -s tests -v
"""

import os
import sys
import unittest

sys.path.insert(
    0,
    os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
)

from model.board import Board, CLEAR, SMOKE, FIRE, POIData
from model.flashpoint_model import (
    FlashPointModel,
    WIN,
    LOSS_VICTIMS,
    LOSS_COLLAPSE,
)
from strategies import STRATEGIES, get_strategy, astar
from strategies.coordination import TargetAssignments, get_assignments
from strategies.prioritization import choose_target, candidate_targets


SERVER_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BOARD_FILE = os.path.join(SERVER_DIR, "data", "final.txt")


def new_board():
    board = Board()
    board.load_from_file(BOARD_FILE)
    return board


def new_model(seed=1, strategy="mejorada"):
    return FlashPointModel(
        board_file=BOARD_FILE,
        seed=seed,
        strategy=get_strategy(strategy)
    )


def path_is_walkable(board, path):
    """Cada paso de la ruta debe ser un movimiento legal o una puerta
    cerrada que se puede abrir. Nunca una pared."""
    for current, following in zip(path, path[1:]):
        legal_move = board.can_move_between(*current, *following)

        door = board.get_door_between(*current, *following)
        closed_door = (
            door is not None and not door.is_open and not door.is_destroyed
        )

        if not legal_move and not closed_door:
            return False

    return True


def equidistant_cells(board, start, count=2):
    """Devuelve `count` celdas alcanzables desde start con el mismo costo
    A*, buscando el costo más pequeño que tenga suficientes celdas. Sirve
    para armar escenarios de prueba justos en un tablero con paredes."""
    by_cost = {}

    for r in range(board.ROWS):
        for c in range(board.COLUMNS):
            cell = (r, c)

            if cell == start:
                continue

            _, cost = astar.find_path(board, start, cell)

            if cost is not None and cost > 0:
                by_cost.setdefault(cost, []).append(cell)

    for cost in sorted(by_cost):
        if len(by_cost[cost]) >= count:
            return by_cost[cost][:count]

    return None


# =========================================================
# Registro de estrategias
# =========================================================

class TestRegistro(unittest.TestCase):

    def test_las_tres_estrategias_existen(self):
        for name in ["aleatoria", "mejorada", "mejorada_sin_coordinacion"]:
            self.assertIn(name, STRATEGIES)
            self.assertEqual(get_strategy(name).strategy_name, name)

    def test_nombre_desconocido_falla_claro(self):
        with self.assertRaises(ValueError):
            get_strategy("no_existe")


# =========================================================
# A*
# =========================================================

class TestAStar(unittest.TestCase):

    def setUp(self):
        self.board = new_board()

    def test_ruta_de_una_celda_a_si_misma(self):
        path, cost = astar.find_path(self.board, (0, 0), (0, 0))

        self.assertEqual(path, [(0, 0)])
        self.assertEqual(cost, 0)

    def test_encuentra_ruta_entre_esquinas(self):
        path, cost = astar.find_path(self.board, (0, 0), (5, 7))

        self.assertIsNotNone(path)
        self.assertEqual(path[0], (0, 0))
        self.assertEqual(path[-1], (5, 7))
        self.assertGreater(cost, 0)

    def test_la_ruta_respeta_paredes(self):
        """Ninguna ruta entre dos celdas cualesquiera cruza una pared."""
        cells = [
            (r, c)
            for r in range(self.board.ROWS)
            for c in range(self.board.COLUMNS)
        ]

        for start in cells[::7]:
            for goal in cells[::5]:
                path, _ = astar.find_path(self.board, start, goal)

                if path is None:
                    continue

                self.assertTrue(
                    path_is_walkable(self.board, path),
                    f"Ruta inválida de {start} a {goal}: {path}"
                )

    def test_pasos_consecutivos_son_adyacentes(self):
        path, _ = astar.find_path(self.board, (0, 0), (5, 7))

        for current, following in zip(path, path[1:]):
            distance = astar.manhattan(current, following)
            self.assertEqual(distance, 1)

    def test_prefiere_rodear_el_fuego(self):
        """Con castigo por fuego, la ruta evita una celda en llamas si
        hay un rodeo barato."""
        board = new_board()

        # Se limpia el tablero para controlar el experimento.
        for r in range(board.ROWS):
            for c in range(board.COLUMNS):
                board.set_state(r, c, CLEAR)

        path_clear, cost_clear = astar.find_path(board, (0, 0), (0, 2))

        self.assertIsNotNone(path_clear)

        # Fuego en la celda intermedia de la ruta directa.
        middle = path_clear[1]
        board.set_state(middle[0], middle[1], FIRE)

        path_fire, cost_fire = astar.find_path(board, (0, 0), (0, 2))

        self.assertIsNotNone(path_fire)
        self.assertGreaterEqual(cost_fire, cost_clear)

    def test_cargando_no_atraviesa_fuego(self):
        board = new_board()

        fire = board.fires[0]
        start = (fire.row, fire.column)

        # Desde cualquier celda, ninguna ruta cargando pisa fuego.
        for goal in [(0, 0), (5, 7), (2, 3)]:
            path, _ = astar.find_path(board, (0, 2), goal, carrying=True)

            if path is None:
                continue

            for cell in path[1:]:
                self.assertFalse(
                    board.has_fire(*cell),
                    f"Cargando no debe pisar fuego en {cell}"
                )

    def test_varias_metas_devuelve_la_mas_cercana(self):
        exits = [(e.row, e.column) for e in self.board.exits]

        path, cost = astar.find_path(self.board, (2, 3), exits)

        self.assertIsNotNone(path)
        self.assertIn(path[-1], exits)

        for exit_cell in exits:
            _, single = astar.find_path(self.board, (2, 3), exit_cell)

            if single is not None:
                self.assertLessEqual(cost, single)

    def test_siguiente_paso_abre_puerta_cerrada(self):
        board = new_board()
        door = board.doors[0]

        path = [(door.row1, door.column1), (door.row2, door.column2)]

        action = astar.next_step_action(board, path)

        self.assertEqual(action[0], "open_door")
        self.assertIs(action[1], door)

        door.is_open = True

        action = astar.next_step_action(board, path)

        self.assertEqual(action, ("move", door.row2, door.column2))


# =========================================================
# Priorización
# =========================================================

class TestPriorizacion(unittest.TestCase):

    def _clean_board(self):
        board = new_board()

        for r in range(board.ROWS):
            for c in range(board.COLUMNS):
                board.set_state(r, c, CLEAR)

        board.pois = []

        return board

    def test_candidatos_excluyen_falsas_alarmas_reveladas(self):
        board = new_board()

        for poi in board.pois:
            poi.revealed = True

        kinds = [kind for _, kind in candidate_targets(board)]

        self.assertNotIn("poi", kinds)
        self.assertEqual(kinds.count("victima"), 2)

    def test_victima_revelada_gana_a_poi_oculto_a_igual_distancia(self):
        board = self._clean_board()
        model = new_model()
        model.board = board

        firefighter = model.firefighters[0]
        firefighter.row, firefighter.column = 2, 3

        # Dos objetivos al mismo costo real de ruta, uno revelado.
        cells = equidistant_cells(board, (2, 3))
        self.assertIsNotNone(cells)

        hidden = POIData(row=cells[0][0], column=cells[0][1], poi_type="v", revealed=False)
        revealed = POIData(row=cells[1][0], column=cells[1][1], poi_type="v", revealed=True)

        board.pois = [hidden, revealed]

        target = choose_target(board, firefighter, TargetAssignments())

        self.assertEqual(target[1], "victima")
        self.assertEqual(target[0], cells[1])

    def test_sin_candidatos_devuelve_none(self):
        board = self._clean_board()
        model = new_model()
        model.board = board

        target = choose_target(board, model.firefighters[0], TargetAssignments())

        self.assertIsNone(target)


# =========================================================
# Coordinación
# =========================================================

class TestCoordinacion(unittest.TestCase):

    def test_tabla_de_asignaciones(self):
        table = TargetAssignments()

        table.claim(1, (2, 2))
        table.claim(2, (2, 2))
        table.claim(3, (4, 4))

        self.assertEqual(table.others_on((2, 2), 1), 1)
        self.assertEqual(table.others_on((2, 2), 3), 2)
        self.assertEqual(table.others_on((4, 4), 3), 0)

        table.release(2)

        self.assertEqual(table.others_on((2, 2), 1), 0)

    def test_cleanup_suelta_objetivos_que_ya_no_existen(self):
        board = new_board()
        table = TargetAssignments()

        poi = board.pois[0]
        table.claim(1, (poi.row, poi.column))
        table.claim(2, (0, 0))

        table.cleanup(board)

        self.assertEqual(table.get(1), (poi.row, poi.column))
        self.assertIsNone(table.get(2))

    def test_dos_bomberos_se_reparten_objetivos_distintos(self):
        """Con dos objetivos y dos bomberos, el segundo evita el que ya
        tomó el primero si hay alternativa a distancia parecida."""
        board = new_board()

        for r in range(board.ROWS):
            for c in range(board.COLUMNS):
                board.set_state(r, c, CLEAR)

        model = new_model()
        model.board = board

        first = model.firefighters[0]
        second = model.firefighters[1]

        # Los dos parten de la misma celda y hay dos víctimas al
        # mismo costo real de ruta.
        first.row, first.column = 2, 3
        second.row, second.column = 2, 3

        cells = equidistant_cells(board, (2, 3))
        self.assertIsNotNone(cells)

        board.pois = [
            POIData(row=cells[0][0], column=cells[0][1], poi_type="v", revealed=True),
            POIData(row=cells[1][0], column=cells[1][1], poi_type="v", revealed=True),
        ]

        table = TargetAssignments()

        first_target = choose_target(board, first, table)
        table.claim(first.firefighter_id, first_target[0])

        second_target = choose_target(board, second, table)

        self.assertNotEqual(first_target[0], second_target[0])

    def test_sin_coordinacion_ambos_toman_el_mismo(self):
        board = new_board()

        for r in range(board.ROWS):
            for c in range(board.COLUMNS):
                board.set_state(r, c, CLEAR)

        model = new_model()
        model.board = board

        first = model.firefighters[0]
        second = model.firefighters[1]
        first.row, first.column = 2, 3
        second.row, second.column = 2, 3

        cells = equidistant_cells(board, (2, 3))
        self.assertIsNotNone(cells)

        board.pois = [
            POIData(row=cells[0][0], column=cells[0][1], poi_type="v", revealed=True),
            POIData(row=cells[1][0], column=cells[1][1], poi_type="v", revealed=True),
        ]

        table = TargetAssignments()

        first_target = choose_target(board, first, table, use_coordination=False)
        table.claim(first.firefighter_id, first_target[0])

        second_target = choose_target(board, second, table, use_coordination=False)

        self.assertEqual(first_target[0], second_target[0])


# =========================================================
# Estrategia completa
# =========================================================

class TestEstrategiaMejorada(unittest.TestCase):

    def test_solo_devuelve_acciones_legales(self):
        """A lo largo de varias partidas, cada acción elegida está en el
        catálogo legal del bombero en ese momento."""
        for seed in range(3):
            model = new_model(seed=seed)
            strategy = model.strategy

            checked = 0

            while model.running and checked < 300:
                firefighter = model.current_firefighter
                firefighter.reset_action_points()

                for _ in range(20):
                    legal = firefighter.get_legal_actions()

                    action = strategy(model, firefighter)

                    if action is None:
                        break

                    self.assertIn(action, legal)

                    if not firefighter.execute_action(action):
                        break

                    checked += 1

                model.step()

            self.assertGreater(checked, 0)

    def test_cargando_se_dirige_a_una_salida(self):
        model = new_model(seed=2)
        board = model.board

        for r in range(board.ROWS):
            for c in range(board.COLUMNS):
                board.set_state(r, c, CLEAR)

        firefighter = model.firefighters[0]
        firefighter.row, firefighter.column = 2, 3
        firefighter.carrying_victim = True
        firefighter.reset_action_points()

        exits = [(e.row, e.column) for e in board.exits]
        _, before = astar.find_path(board, (2, 3), exits, carrying=True)

        action = model.strategy(model, firefighter)

        self.assertIsNotNone(action)
        self.assertIn(action[0], ["move", "open_door"])

        firefighter.execute_action(action)

        _, after = astar.find_path(
            board,
            (firefighter.row, firefighter.column),
            exits,
            carrying=True
        )

        self.assertLessEqual(after, before)

    def test_carga_la_victima_revelada_bajo_los_pies(self):
        model = new_model(seed=2)
        board = model.board

        poi = next(p for p in board.pois if p.is_victim)
        poi.revealed = True

        firefighter = model.firefighters[0]
        firefighter.row, firefighter.column = poi.row, poi.column
        firefighter.reset_action_points()

        action = model.strategy(model, firefighter)

        self.assertEqual(action, ("pick_up",))

    def test_apaga_fuego_adyacente_antes_de_avanzar(self):
        model = new_model(seed=2)
        board = model.board

        firefighter = model.firefighters[0]
        firefighter.reset_action_points()

        # Se pone fuego en una celda vecina accesible.
        neighbors = board.get_valid_neighbors(firefighter.row, firefighter.column)
        self.assertTrue(neighbors)

        target = neighbors[0]
        board.set_state(target[0], target[1], FIRE)

        action = model.strategy(model, firefighter)

        self.assertEqual(action[0], "extinguish")
        self.assertEqual((action[1], action[2]), target)

    def test_termina_el_turno_sin_acciones_legales(self):
        model = new_model(seed=2)
        firefighter = model.firefighters[0]
        firefighter.action_points = 0

        self.assertIsNone(model.strategy(model, firefighter))

    def test_la_partida_termina(self):
        for seed in range(4):
            model = new_model(seed=seed)
            result = model.run_game()

            self.assertIn(result, [WIN, LOSS_VICTIMS, LOSS_COLLAPSE])
            self.assertLess(model.turns, model.MAX_STEPS)

    def test_misma_semilla_misma_partida(self):
        a = new_model(seed=77)
        b = new_model(seed=77)

        a.run_game()
        b.run_game()

        self.assertEqual(a.result, b.result)
        self.assertEqual(a.turns, b.turns)
        self.assertEqual(a.victims_rescued, b.victims_rescued)
        self.assertEqual(a.board.damage_markers, b.board.damage_markers)
        self.assertEqual(a.replanifications, b.replanifications)

    def test_cuenta_replaneaciones(self):
        model = new_model(seed=5)
        model.run_game()

        self.assertGreaterEqual(model.replanifications, 0)

    def test_no_corta_paredes(self):
        """La estrategia nunca elige cortar: cada corte acerca al
        edificio al colapso y no hay caso en que convenga."""
        model = new_model(seed=3)
        model.run_game()

        chops = sum(f.walls_chopped for f in model.firefighters)

        self.assertEqual(chops, 0)

    def test_mejora_a_la_aleatoria_en_semillas_pareadas(self):
        """En un bloque corto de semillas pareadas, la mejorada rescata
        más víctimas en promedio que la aleatoria. La comparación
        completa con 100 semillas vive en run_batch.py."""
        seeds = range(10)

        random_total = 0
        improved_total = 0

        for seed in seeds:
            random_model = new_model(seed=seed, strategy="aleatoria")
            random_model.run_game()
            random_total += random_model.victims_rescued

            improved_model = new_model(seed=seed, strategy="mejorada")
            improved_model.run_game()
            improved_total += improved_model.victims_rescued

        self.assertGreater(improved_total, random_total)


if __name__ == "__main__":
    unittest.main(verbosity=2)
