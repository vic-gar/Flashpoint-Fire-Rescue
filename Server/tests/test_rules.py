"""Pruebas del motor de reglas de Flash Point.

Se ejecutan contra el tablero real de data/final.txt, no contra
tableros inventados, para que verifiquen el juego que realmente
vamos a entregar.

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

from model.board import Board, CLEAR, SMOKE, FIRE
from model.flashpoint_model import (
    FlashPointModel,
    WIN,
    LOSS_VICTIMS,
    LOSS_COLLAPSE,
)
from model import fire_phase


BOARD_FILE = os.path.join(
    os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
    "data",
    "final.txt"
)


def new_board():
    board = Board()
    board.load_from_file(BOARD_FILE)
    return board


def new_model(seed=1):
    return FlashPointModel(board_file=BOARD_FILE, seed=seed)


# =========================================================
# Carga del tablero
# =========================================================

class TestCargaDelTablero(unittest.TestCase):

    def setUp(self):
        self.board = new_board()

    def test_dimensiones(self):
        self.assertEqual(len(self.board.cells), 6)
        self.assertEqual(len(self.board.cells[0]), 8)

    def test_conteos_iniciales(self):
        self.assertEqual(self.board.count_fires(), 10)
        self.assertEqual(len(self.board.smokes), 0)
        self.assertEqual(len(self.board.pois), 3)
        self.assertEqual(len(self.board.doors), 8)
        self.assertEqual(len(self.board.exits), 4)
        self.assertEqual(self.board.damage_markers, 0)

    def test_pois_del_archivo(self):
        tipos = sorted(poi.poi_type for poi in self.board.pois)
        self.assertEqual(tipos, ["f", "v", "v"])

    def test_mazo_descuenta_los_del_archivo(self):
        model = new_model()

        # 10 victimas y 5 falsas alarmas en total, menos las 3 que
        # el archivo ya coloco sobre el tablero.
        self.assertEqual(len(model.poi_deck), 12)
        self.assertEqual(model.poi_deck.count("v"), 8)
        self.assertEqual(model.poi_deck.count("f"), 4)


# =========================================================
# Paredes, puertas y movimiento
# =========================================================

class TestMovimiento(unittest.TestCase):

    def setUp(self):
        self.board = new_board()

    def test_no_se_sale_del_tablero(self):
        self.assertFalse(self.board.can_move_between(0, 0, -1, 0))
        self.assertFalse(self.board.can_move_between(5, 7, 6, 7))

    def test_no_hay_movimiento_diagonal(self):
        self.assertFalse(self.board.can_move_between(2, 2, 3, 3))

    def test_una_pared_bloquea_en_ambos_sentidos(self):
        """El bloqueo tiene que ser simétrico.

        El archivo de entrada trae algunas aristas donde solo una
        de las dos celdas declara la pared. Si se leyera un solo
        lado se podría pasar en un sentido y no en el otro.
        """
        for row in range(self.board.ROWS):
            for column in range(self.board.COLUMNS):
                for next_row, next_column in [
                    (row - 1, column),
                    (row + 1, column),
                    (row, column - 1),
                    (row, column + 1),
                ]:
                    if not self.board.is_inside(next_row, next_column):
                        continue

                    ida = self.board.can_move_between(
                        row, column, next_row, next_column
                    )

                    vuelta = self.board.can_move_between(
                        next_row, next_column, row, column
                    )

                    self.assertEqual(
                        ida,
                        vuelta,
                        f"Asimetría entre ({row},{column}) "
                        f"y ({next_row},{next_column})"
                    )

    def test_puerta_cerrada_bloquea_y_abierta_deja_pasar(self):
        door = self.board.doors[0]

        self.assertFalse(
            self.board.can_move_between(
                door.row1, door.column1, door.row2, door.column2
            )
        )

        door.is_open = True

        self.assertTrue(
            self.board.can_move_between(
                door.row1, door.column1, door.row2, door.column2
            )
        )

    def test_costo_de_movimiento(self):
        fuego = self.board.fires[0]

        self.assertEqual(
            self.board.get_movement_cost(fuego.row, fuego.column),
            2
        )

        libre = None

        for row in range(self.board.ROWS):
            for column in range(self.board.COLUMNS):
                if self.board.is_clear(row, column):
                    libre = (row, column)
                    break
            if libre:
                break

        self.assertEqual(self.board.get_movement_cost(*libre), 1)


# =========================================================
# Daño estructural
# =========================================================

class TestDanio(unittest.TestCase):

    def setUp(self):
        self.board = new_board()

    def test_dos_marcadores_destruyen_la_pared(self):
        pared = None

        for row in range(self.board.ROWS):
            for column in range(self.board.COLUMNS - 1):
                if self.board.has_wall_between(row, column, row, column + 1):
                    pared = (row, column, row, column + 1)
                    break
            if pared:
                break

        self.assertIsNotNone(pared, "El tablero debe tener paredes")

        self.assertFalse(self.board.can_move_between(*pared))

        self.board.add_wall_damage(*pared)
        self.assertFalse(self.board.is_wall_destroyed(*pared))

        self.board.add_wall_damage(*pared)
        self.assertTrue(self.board.is_wall_destroyed(*pared))

        # Una pared destruida se puede atravesar.
        self.assertTrue(self.board.can_move_between(*pared))

        self.assertEqual(self.board.damage_markers, 2)

    def test_no_se_colocan_mas_de_24_marcadores(self):
        colocados = 0

        for row in range(self.board.ROWS):
            for column in range(self.board.COLUMNS):
                for _ in range(3):
                    if self.board.add_wall_damage(row, column, row - 1, column):
                        colocados += 1

        self.assertEqual(self.board.damage_markers, 24)
        self.assertEqual(colocados, 24)


# =========================================================
# Avance del fuego
# =========================================================

class TestAvanceDelFuego(unittest.TestCase):

    def setUp(self):
        self.model = new_model()
        self.board = self.model.board

    def _celda_despejada_sin_vecinos_en_fuego(self):
        for row in range(self.board.ROWS):
            for column in range(self.board.COLUMNS):
                if not self.board.is_clear(row, column):
                    continue

                vecinos_con_fuego = any(
                    self.board.has_fire(r, c)
                    for r, c in self.board.get_valid_neighbors(row, column)
                )

                if not vecinos_con_fuego:
                    return (row, column)

        return None

    def test_celda_despejada_recibe_humo(self):
        celda = self._celda_despejada_sin_vecinos_en_fuego()
        self.assertIsNotNone(celda)

        fire_phase.advance_fire(self.model, target=celda)

        self.assertTrue(self.board.has_smoke(*celda))

    def test_humo_se_convierte_en_fuego(self):
        celda = self._celda_despejada_sin_vecinos_en_fuego()
        self.board.set_state(celda[0], celda[1], SMOKE)

        fire_phase.advance_fire(self.model, target=celda)

        self.assertTrue(self.board.has_fire(*celda))

    def test_fuego_provoca_explosion(self):
        fuego = self.board.fires[0]
        antes = self.board.damage_markers

        resumen = fire_phase.advance_fire(
            self.model,
            target=(fuego.row, fuego.column)
        )

        self.assertTrue(resumen["explosion"])

        # Una explosión tiene que dejar huella: fuego nuevo,
        # paredes dañadas o puertas destruidas.
        huella = (
            resumen["new_fire"]
            + resumen["walls_damaged"]
            + resumen["doors_destroyed"]
        )

        self.assertGreater(huella, 0)
        self.assertGreaterEqual(self.board.damage_markers, antes)

    def test_flashover_enciende_humo_junto_al_fuego(self):
        fuego = self.board.fires[0]

        vecinos = self.board.get_valid_neighbors(fuego.row, fuego.column)

        vecino = None

        for row, column in vecinos:
            if self.board.is_clear(row, column):
                vecino = (row, column)
                break

        if vecino is None:
            self.skipTest("No hay vecino despejado junto al fuego")

        self.board.set_state(vecino[0], vecino[1], SMOKE)

        fire_phase._resolve_flashover(self.model)

        self.assertTrue(self.board.has_fire(*vecino))

    def test_humo_aislado_por_pared_no_se_enciende(self):
        """El fuego no atraviesa paredes para encender humo."""
        board = self.board

        for row in range(board.ROWS):
            for column in range(board.COLUMNS):
                if not board.has_fire(row, column):
                    continue

                for dr, dc in [(-1, 0), (1, 0), (0, -1), (0, 1)]:
                    nr, nc = row + dr, column + dc

                    if not board.is_inside(nr, nc):
                        continue

                    if board.can_move_between(row, column, nr, nc):
                        continue

                    if not board.is_clear(nr, nc):
                        continue

                    board.set_state(nr, nc, SMOKE)
                    fire_phase._resolve_flashover(self.model)

                    self.assertTrue(
                        board.has_smoke(nr, nc),
                        "El humo detrás de una pared no debe encenderse"
                    )
                    return

        self.skipTest("No se encontró humo aislado por pared")

    def test_bombero_sobre_fuego_es_derribado(self):
        bombero = self.model.firefighters[0]

        objetivo = self.board.fires[0]

        bombero.row = objetivo.row
        bombero.column = objetivo.column

        derribos_antes = self.model.knock_downs

        fire_phase._resolve_knock_downs(self.model, {"knocked_down": []})

        self.assertGreater(self.model.knock_downs, derribos_antes)
        self.assertTrue(
            self.board.is_exit(bombero.row, bombero.column)
        )

    def test_victima_cargada_se_pierde_al_ser_derribado(self):
        bombero = self.model.firefighters[0]
        objetivo = self.board.fires[0]

        bombero.row = objetivo.row
        bombero.column = objetivo.column
        bombero.carrying_victim = True

        perdidas_antes = self.model.victims_lost

        fire_phase._resolve_knock_downs(self.model, {"knocked_down": []})

        self.assertEqual(self.model.victims_lost, perdidas_antes + 1)
        self.assertFalse(bombero.carrying_victim)


# =========================================================
# Acciones del bombero
# =========================================================

class TestAccionesDelBombero(unittest.TestCase):

    def setUp(self):
        self.model = new_model()
        self.board = self.model.board
        self.bombero = self.model.firefighters[0]
        self.bombero.reset_action_points()

    def test_apagar_humo_cuesta_un_ap(self):
        self.board.set_state(self.bombero.row, self.bombero.column, SMOKE)

        antes = self.bombero.action_points

        self.assertTrue(
            self.bombero.extinguish(self.bombero.row, self.bombero.column)
        )

        self.assertEqual(self.bombero.action_points, antes - 1)
        self.assertTrue(
            self.board.is_clear(self.bombero.row, self.bombero.column)
        )

    def test_apagar_fuego_del_todo_cuesta_dos_ap(self):
        self.board.set_state(self.bombero.row, self.bombero.column, FIRE)

        antes = self.bombero.action_points

        self.assertTrue(
            self.bombero.extinguish(
                self.bombero.row,
                self.bombero.column,
                full=True
            )
        )

        self.assertEqual(self.bombero.action_points, antes - 2)
        self.assertTrue(
            self.board.is_clear(self.bombero.row, self.bombero.column)
        )

    def test_bajar_fuego_a_humo_cuesta_un_ap(self):
        self.board.set_state(self.bombero.row, self.bombero.column, FIRE)

        antes = self.bombero.action_points

        self.assertTrue(
            self.bombero.extinguish(
                self.bombero.row,
                self.bombero.column,
                full=False
            )
        )

        self.assertEqual(self.bombero.action_points, antes - 1)
        self.assertTrue(
            self.board.has_smoke(self.bombero.row, self.bombero.column)
        )

    def test_cortar_pared_cuesta_dos_ap_y_suma_danio(self):
        paredes = self.board.get_adjacent_walls(
            self.bombero.row,
            self.bombero.column
        )

        if not paredes:
            self.skipTest("El bombero no tiene paredes adyacentes")

        antes_ap = self.bombero.action_points
        antes_danio = self.board.damage_markers

        self.assertTrue(self.bombero.chop_wall(*paredes[0]))

        self.assertEqual(self.bombero.action_points, antes_ap - 2)
        self.assertEqual(self.board.damage_markers, antes_danio + 1)

    def test_no_se_puede_actuar_sin_ap(self):
        self.bombero.action_points = 0

        self.assertEqual(self.bombero.get_legal_actions(), [])

    def test_ap_guardados_tienen_tope(self):
        self.bombero.action_points = 10

        self.model._save_action_points(self.bombero)

        self.assertEqual(self.bombero.action_points, 4)
        self.assertEqual(self.bombero.action_points_wasted, 6)

    def test_turno_arranca_con_cuatro_ap(self):
        bombero = self.model.firefighters[1]

        self.assertEqual(bombero.action_points, 0)

        bombero.reset_action_points()

        self.assertEqual(bombero.action_points, 4)

    def test_no_se_puede_entrar_a_fuego_sin_ap_para_salir(self):
        """Entrar al fuego cuesta 2 AP y hay que poder salir."""
        board = self.board
        bombero = self.bombero

        destino = None

        for row, column in board.get_valid_neighbors(bombero.row, bombero.column):
            if board.has_fire(row, column):
                destino = (row, column)
                break

        if destino is None:
            # Se fuerza fuego en una celda vecina accesible.
            vecinos = board.get_valid_neighbors(bombero.row, bombero.column)

            if not vecinos:
                self.skipTest("El bombero no tiene vecinos accesibles")

            destino = vecinos[0]
            board.set_state(destino[0], destino[1], FIRE)

        bombero.action_points = 2

        acciones = bombero.get_legal_actions()

        movimientos = [
            a for a in acciones
            if a[0] == "move" and (a[1], a[2]) == destino
        ]

        self.assertEqual(
            movimientos,
            [],
            "Con 2 AP no debe poder entrar al fuego y quedarse"
        )

        bombero.action_points = 3

        acciones = bombero.get_legal_actions()

        movimientos = [
            a for a in acciones
            if a[0] == "move" and (a[1], a[2]) == destino
        ]

        self.assertEqual(len(movimientos), 1)


# =========================================================
# Puntos de interés
# =========================================================

class TestPuntosDeInteres(unittest.TestCase):

    def setUp(self):
        self.model = new_model()
        self.board = self.model.board
        self.bombero = self.model.firefighters[0]
        self.bombero.reset_action_points()

    def test_entrar_a_la_celda_revela_el_poi(self):
        poi = self.board.pois[0]

        self.bombero.row = poi.row
        self.bombero.column = poi.column

        self.bombero._on_enter_cell()

        self.assertTrue(poi.revealed)

    def test_falsa_alarma_se_retira_del_tablero(self):
        poi = next(p for p in self.board.pois if p.is_false_alarm)

        self.bombero.row = poi.row
        self.bombero.column = poi.column

        self.bombero._on_enter_cell()

        self.assertNotIn(poi, self.board.pois)
        self.assertEqual(self.model.false_alarms_revealed, 1)

    def test_cargar_y_rescatar_victima(self):
        poi = next(p for p in self.board.pois if p.is_victim)

        self.bombero.row = poi.row
        self.bombero.column = poi.column
        self.bombero._on_enter_cell()

        self.assertTrue(self.bombero.pick_up_victim())
        self.assertTrue(self.bombero.carrying_victim)
        self.assertNotIn(poi, self.board.pois)

        salida = self.board.exits[0]

        self.bombero.row = salida.row
        self.bombero.column = salida.column
        self.bombero._on_enter_cell()

        self.assertFalse(self.bombero.carrying_victim)
        self.assertEqual(self.model.victims_rescued, 1)

    def test_cargando_no_se_entra_al_fuego(self):
        self.bombero.carrying_victim = True

        fuego = self.board.fires[0]

        self.assertIsNone(
            self.bombero.get_move_cost(fuego.row, fuego.column)
        )

    def test_reposicion_mantiene_tres_pois(self):
        # Se vacía el tablero de POI y se repone.
        self.board.pois = []

        self.model._replenish_pois()

        self.assertEqual(self.board.count_hidden_pois(), 3)


# =========================================================
# Condiciones de fin
# =========================================================

class TestFinDeLaPartida(unittest.TestCase):

    def test_victoria_con_siete_rescatadas(self):
        model = new_model()
        model.victims_rescued = 7

        self.assertTrue(model._check_end_conditions())
        self.assertEqual(model.result, WIN)
        self.assertFalse(model.running)

    def test_derrota_con_cuatro_perdidas(self):
        model = new_model()
        model.victims_lost = 4

        self.assertTrue(model._check_end_conditions())
        self.assertEqual(model.result, LOSS_VICTIMS)

    def test_colapso_al_agotar_los_marcadores(self):
        model = new_model()
        model.board.damage_markers = model.MAX_DAMAGE

        self.assertTrue(model._check_end_conditions())
        self.assertEqual(model.result, LOSS_COLLAPSE)

    def test_la_partida_siempre_termina(self):
        for seed in range(5):
            model = new_model(seed=seed)
            resultado = model.run_game()

            self.assertIn(
                resultado,
                [WIN, LOSS_VICTIMS, LOSS_COLLAPSE],
                f"La partida con semilla {seed} no terminó bien"
            )
            self.assertFalse(model.running)

    def test_misma_semilla_da_misma_partida(self):
        """Reproducibilidad, indispensable para comparar estrategias."""
        a = new_model(seed=123)
        b = new_model(seed=123)

        a.run_game()
        b.run_game()

        self.assertEqual(a.result, b.result)
        self.assertEqual(a.turns, b.turns)
        self.assertEqual(a.victims_rescued, b.victims_rescued)
        self.assertEqual(a.board.damage_markers, b.board.damage_markers)


# =========================================================
# Estrategia aleatoria
# =========================================================

class TestEstrategiaAleatoria(unittest.TestCase):

    def test_solo_ejecuta_acciones_legales(self):
        """Ninguna acción elegida puede estar fuera del catálogo."""
        model = new_model(seed=7)

        for _ in range(40):
            if not model.running:
                break

            bombero = model.current_firefighter
            bombero.reset_action_points()

            legales = bombero.get_legal_actions()

            if legales:
                from model.flashpoint_model import random_strategy

                elegida = random_strategy(model, bombero)

                self.assertIn(elegida, legales)

            model.step()

    def test_recolecta_metricas(self):
        model = new_model(seed=3)
        model.run_game()

        datos = model.datacollector.get_model_vars_dataframe()

        self.assertGreater(len(datos), 0)

        for columna in [
            "resultado",
            "rescatadas",
            "perdidas",
            "danio",
            "fuegos",
            "turnos",
        ]:
            self.assertIn(columna, datos.columns)


if __name__ == "__main__":
    unittest.main(verbosity=2)
