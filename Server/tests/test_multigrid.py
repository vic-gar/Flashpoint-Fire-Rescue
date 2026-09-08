"""Pruebas de la integración con MultiGrid de Mesa.

Qué se está comprobando y por qué importa:

MultiGrid es el espacio oficial de Mesa. El proyecto lo usa para la
posición de los bomberos, mientras Board sigue siendo el dueño de las
paredes, las puertas y el estado del tablero. Estas pruebas verifican
las dos mitades y, sobre todo, que la integración NO cambió las reglas
ni los resultados.

La prueba más importante del archivo es
test_el_orden_de_los_vecinos_es_el_mismo_que_el_de_board: Mesa devuelve
la vecindad ordenada por (x, y), que en nuestras coordenadas es
izquierda, arriba, abajo, derecha. Board la devuelve como arriba,
abajo, izquierda, derecha. Ese orden entra al catálogo de acciones
legales y de ahí a lo que elige la estrategia aleatoria con una semilla
dada. En la primera versión de la integración cambiaron 1164 métricas
de los experimentos solo por eso, sin que ninguna regla cambiara.

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

from mesa.space import MultiGrid

from model.board import CLEAR, FIRE, POIData
from model.flashpoint_model import FlashPointModel
from model import fire_phase
from strategies import STRATEGIES, get_strategy, astar


SERVER_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BOARD_FILE = os.path.join(SERVER_DIR, "data", "final.txt")


def new_model(seed=1, strategy="mejorada"):
    return FlashPointModel(
        board_file=BOARD_FILE,
        seed=seed,
        strategy=get_strategy(strategy)
    )


def cell(firefighter):
    """Celda del bombero en coordenadas del proyecto (fila, columna)."""
    return (firefighter.row, firefighter.column)


def grid_cell(firefighter):
    """La misma celda, leída del MultiGrid en su orden (x, y)."""
    x, y = firefighter.pos
    return (y, x)


# =========================================================
# El espacio existe y está configurado como se pidió
# =========================================================

class TestEspacioMultiGrid(unittest.TestCase):

    def setUp(self):
        self.model = new_model()

    def test_el_modelo_tiene_una_instancia_de_multigrid(self):
        self.assertIsInstance(self.model.grid, MultiGrid)

    def test_ancho_ocho_y_alto_seis(self):
        """Mesa ordena el grid como (ancho, alto). El tablero del reto
        es de 6 filas por 8 columnas, así que ancho 8 y alto 6."""
        self.assertEqual(self.model.grid.width, 8)
        self.assertEqual(self.model.grid.height, 6)

        self.assertEqual(self.model.grid.width, self.model.board.COLUMNS)
        self.assertEqual(self.model.grid.height, self.model.board.ROWS)

    def test_torus_desactivado(self):
        self.assertFalse(self.model.grid.torus)

    def test_las_esquinas_del_grid_existen_y_una_mas_no(self):
        self.assertFalse(self.model.grid.out_of_bounds((0, 0)))
        self.assertFalse(self.model.grid.out_of_bounds((7, 5)))
        self.assertTrue(self.model.grid.out_of_bounds((8, 0)))
        self.assertTrue(self.model.grid.out_of_bounds((0, 6)))


# =========================================================
# Los bomberos viven en el grid
# =========================================================

class TestBomberosEnElGrid(unittest.TestCase):

    def setUp(self):
        self.model = new_model()

    def test_los_seis_bomberos_estan_colocados(self):
        self.assertEqual(len(self.model.firefighters), 6)

        for firefighter in self.model.firefighters:
            self.assertIsNotNone(
                firefighter.pos,
                f"El bombero {firefighter.firefighter_id} no está en el grid"
            )

    def test_cada_bombero_esta_en_la_celda_que_dice_estar(self):
        for firefighter in self.model.firefighters:
            self.assertEqual(cell(firefighter), grid_cell(firefighter))

            contenido = self.model.grid.get_cell_list_contents(
                [firefighter.pos]
            )

            self.assertIn(firefighter, contenido)

    def test_arrancan_en_las_entradas_del_tablero(self):
        salidas = {(e.row, e.column) for e in self.model.board.exits}

        for firefighter in self.model.firefighters:
            self.assertIn(cell(firefighter), salidas)

    def test_el_grid_contiene_exactamente_seis_agentes(self):
        total = sum(
            len(self.model.grid.get_cell_list_contents([(x, y)]))
            for x in range(self.model.grid.width)
            for y in range(self.model.grid.height)
        )

        self.assertEqual(total, 6)

    def test_dos_bomberos_pueden_compartir_celda(self):
        """Es la razón por la que se usa MultiGrid y no SingleGrid: el
        reto dice que puede haber más de un agente por celda."""
        primero, segundo = self.model.firefighters[0], self.model.firefighters[1]

        primero.set_cell(3, 3)
        segundo.set_cell(3, 3)

        contenido = self.model.grid.get_cell_list_contents([(3, 3)])

        self.assertIn(primero, contenido)
        self.assertIn(segundo, contenido)
        self.assertGreaterEqual(len(contenido), 2)

    def test_la_configuracion_inicial_ya_comparte_celdas(self):
        """El reparto del reto pone dos bomberos en cada una de las dos
        primeras entradas, así que la partida arranca con celdas
        compartidas."""
        ocupadas = {}

        for firefighter in self.model.firefighters:
            ocupadas.setdefault(cell(firefighter), []).append(firefighter)

        compartidas = [c for c, fs in ocupadas.items() if len(fs) > 1]

        self.assertTrue(
            compartidas,
            "Se esperaba al menos una celda con más de un bombero"
        )


# =========================================================
# Mover al bombero mueve al agente en el grid
# =========================================================

class TestSincronizacionDePosicion(unittest.TestCase):

    def setUp(self):
        self.model = new_model()
        self.firefighter = self.model.firefighters[0]

    def test_set_cell_actualiza_el_grid(self):
        origen = self.firefighter.pos

        self.firefighter.set_cell(2, 5)

        self.assertEqual(self.firefighter.pos, (5, 2))
        self.assertEqual(cell(self.firefighter), (2, 5))

        self.assertIn(
            self.firefighter,
            self.model.grid.get_cell_list_contents([(5, 2)])
        )
        self.assertNotIn(
            self.firefighter,
            self.model.grid.get_cell_list_contents([origen])
        )

    def test_asignar_row_o_column_tambien_mueve_al_agente(self):
        """row y column son propiedades derivadas de self.pos, así que
        el código antiguo que asigna una u otra sigue funcionando y no
        puede desincronizarse."""
        self.firefighter.set_cell(1, 1)

        self.firefighter.row = 4
        self.assertEqual(self.firefighter.pos, (1, 4))

        self.firefighter.column = 6
        self.assertEqual(self.firefighter.pos, (6, 4))

        self.assertEqual(cell(self.firefighter), grid_cell(self.firefighter))

    def test_moverse_por_una_accion_legal_actualiza_el_grid(self):
        self.firefighter.reset_action_points()

        destino = self.model.movement_neighbors(
            self.firefighter.row,
            self.firefighter.column
        )[0]

        self.assertTrue(self.firefighter.move_to(*destino))

        self.assertEqual(cell(self.firefighter), destino)
        self.assertEqual(grid_cell(self.firefighter), destino)

    def test_el_derribo_tambien_actualiza_el_grid(self):
        """Cuando el fuego alcanza a un bombero, vuelve a una entrada.
        Ese cambio de posición debe verse igual en el grid."""
        board = self.model.board

        self.firefighter.set_cell(2, 2)
        board.set_state(2, 2, FIRE)

        fire_phase._resolve_knock_downs(
            self.model,
            {"knocked_down": []}
        )

        salidas = {(e.row, e.column) for e in board.exits}

        self.assertIn(cell(self.firefighter), salidas)
        self.assertEqual(cell(self.firefighter), grid_cell(self.firefighter))
        self.assertIn(
            self.firefighter,
            self.model.grid.get_cell_list_contents([self.firefighter.pos])
        )

    def test_toda_una_partida_mantiene_la_posicion_sincronizada(self):
        model = new_model(seed=9)

        for _ in range(40):
            if not model.running:
                break

            model.step()

            for firefighter in model.firefighters:
                self.assertEqual(
                    cell(firefighter),
                    grid_cell(firefighter),
                    f"Bombero {firefighter.firefighter_id} desincronizado"
                )


# =========================================================
# Vecindad ortogonal: moore=False y torus=False
# =========================================================

class TestVecindadOrtogonal(unittest.TestCase):

    def setUp(self):
        self.model = new_model()
        self.grid = self.model.grid

    def test_moore_false_devuelve_vecindad_de_von_neumann(self):
        """Una celda interior tiene como máximo cuatro vecinos
        ortogonales, y ninguno es diagonal."""
        vecinos = self.grid.get_neighborhood(
            (4, 2),
            moore=False,
            include_center=False
        )

        self.assertEqual(len(vecinos), 4)
        self.assertEqual(
            set(vecinos),
            {(3, 2), (5, 2), (4, 1), (4, 3)}
        )

    def test_moore_true_si_incluiria_diagonales(self):
        """Control: si esta prueba fallara, la anterior no demostraría
        nada, porque no habría diferencia entre moore True y False."""
        vecinos = self.grid.get_neighborhood(
            (4, 2),
            moore=True,
            include_center=False
        )

        self.assertEqual(len(vecinos), 8)
        self.assertIn((3, 1), vecinos)

    def test_ninguna_celda_interior_pasa_de_cuatro_direcciones(self):
        for row in range(self.model.board.ROWS):
            for column in range(self.model.board.COLUMNS):
                vecinos = self.grid.get_neighborhood(
                    (column, row),
                    moore=False,
                    include_center=False
                )

                self.assertLessEqual(len(vecinos), 4)

                for x, y in vecinos:
                    distancia = abs(x - column) + abs(y - row)
                    self.assertEqual(
                        distancia,
                        1,
                        f"Vecino no ortogonal de (fila {row}, col {column})"
                    )

    def test_una_esquina_no_conecta_con_el_extremo_opuesto(self):
        """torus=False: el edificio no da la vuelta."""
        vecinos = set(
            self.grid.get_neighborhood((0, 0), moore=False, include_center=False)
        )

        self.assertEqual(vecinos, {(1, 0), (0, 1)})

        self.assertNotIn((7, 0), vecinos)
        self.assertNotIn((0, 5), vecinos)

    def test_con_torus_true_si_habria_vuelta(self):
        """Control del caso anterior: con torus=True la esquina sí
        conectaría con el extremo opuesto, así que la prueba de arriba
        puede fallar y por eso vale."""
        con_vuelta = MultiGrid(width=8, height=6, torus=True)

        vecinos = set(
            con_vuelta.get_neighborhood((0, 0), moore=False, include_center=False)
        )

        self.assertIn((7, 0), vecinos)
        self.assertIn((0, 5), vecinos)

    def test_las_cuatro_esquinas_tienen_dos_vecinos(self):
        for esquina in [(0, 0), (7, 0), (0, 5), (7, 5)]:
            vecinos = self.grid.get_neighborhood(
                esquina,
                moore=False,
                include_center=False
            )

            self.assertEqual(len(vecinos), 2, f"esquina {esquina}")


# =========================================================
# El grid da la geometría, Board da las reglas
# =========================================================

class TestGridYBoardJuntos(unittest.TestCase):

    def setUp(self):
        self.model = new_model()
        self.board = self.model.board

    def test_el_orden_de_los_vecinos_es_el_mismo_que_el_de_board(self):
        """Mesa devuelve la vecindad ordenada por (x, y), que sería
        izquierda, arriba, abajo, derecha. El proyecto la recorre como
        arriba, abajo, izquierda, derecha, y ese orden llega hasta el
        catálogo de acciones legales.

        Si esta prueba falla, la estrategia aleatoria elige distinto con
        la misma semilla y los experimentos cambian sin que ninguna
        regla haya cambiado.
        """
        for row in range(self.board.ROWS):
            for column in range(self.board.COLUMNS):
                self.assertEqual(
                    self.model.movement_neighbors(row, column),
                    self.board.get_valid_neighbors(row, column),
                    f"Orden distinto en (fila {row}, col {column})"
                )

    def test_el_orden_sigue_coincidiendo_con_puertas_abiertas(self):
        """El caso anterior con el tablero inicial. Aquí se abren todas
        las puertas para mover la topología y volver a comparar."""
        for door in self.board.doors:
            door.is_open = True

        for row in range(self.board.ROWS):
            for column in range(self.board.COLUMNS):
                self.assertEqual(
                    self.model.movement_neighbors(row, column),
                    self.board.get_valid_neighbors(row, column)
                )

    def test_una_pared_sigue_bloqueando_aunque_el_grid_liste_la_celda(self):
        """El grid solo sabe de geometría. La pared la pone Board."""
        board = self.board

        bloqueadas = [
            (r, c, nr, nc)
            for r in range(board.ROWS)
            for c in range(board.COLUMNS)
            for nr, nc in [(r - 1, c), (r + 1, c), (r, c - 1), (r, c + 1)]
            if board.is_inside(nr, nc)
            and board.has_wall_between(r, c, nr, nc)
            and not board.is_wall_destroyed(r, c, nr, nc)
            and board.get_door_between(r, c, nr, nc) is None
        ]

        self.assertTrue(bloqueadas, "El tablero no tiene paredes interiores")

        row, column, next_row, next_column = bloqueadas[0]

        vecinos_grid = self.model.grid.get_neighborhood(
            (column, row),
            moore=False,
            include_center=False
        )

        self.assertIn((next_column, next_row), vecinos_grid)

        self.assertNotIn(
            (next_row, next_column),
            self.model.movement_neighbors(row, column)
        )

    def test_una_puerta_cerrada_bloquea_y_al_abrirla_deja_pasar(self):
        door = self.board.doors[0]
        door.is_open = False

        origen = (door.row1, door.column1)
        destino = (door.row2, door.column2)

        self.assertNotIn(destino, self.model.movement_neighbors(*origen))

        door.is_open = True

        self.assertIn(destino, self.model.movement_neighbors(*origen))

    def test_abrir_la_puerta_habilita_la_accion_de_moverse(self):
        model = new_model()
        door = model.board.doors[0]
        door.is_open = False

        firefighter = model.firefighters[0]
        firefighter.set_cell(door.row1, door.column1)
        firefighter.reset_action_points()

        destino = ("move", door.row2, door.column2)

        self.assertNotIn(destino, firefighter.get_legal_actions())

        self.assertTrue(firefighter.open_door(door))

        self.assertIn(destino, firefighter.get_legal_actions())


# =========================================================
# Las acciones legales siguen sin diagonales
# =========================================================

class TestSinDiagonales(unittest.TestCase):

    def test_ninguna_accion_de_movimiento_es_diagonal(self):
        for seed in range(3):
            model = new_model(seed=seed)

            for _ in range(30):
                if not model.running:
                    break

                firefighter = model.current_firefighter
                firefighter.reset_action_points()

                for action in firefighter.get_legal_actions():
                    if action[0] != "move":
                        continue

                    distancia = (
                        abs(action[1] - firefighter.row)
                        + abs(action[2] - firefighter.column)
                    )

                    self.assertEqual(
                        distancia,
                        1,
                        f"Movimiento no ortogonal: {action}"
                    )

                model.step()

    def test_a_estrella_solo_usa_las_cuatro_direcciones(self):
        self.assertEqual(
            set(astar.DIRECTIONS),
            {(-1, 0), (1, 0), (0, -1), (0, 1)}
        )

    def test_las_rutas_de_a_estrella_no_dan_pasos_diagonales(self):
        model = new_model()
        board = model.board

        origen = (model.firefighters[0].row, model.firefighters[0].column)
        metas = [(e.row, e.column) for e in board.exits]

        for meta in metas:
            path, _ = astar.find_path(board, origen, meta)

            if path is None:
                continue

            for actual, siguiente in zip(path, path[1:]):
                distancia = (
                    abs(actual[0] - siguiente[0])
                    + abs(actual[1] - siguiente[1])
                )

                self.assertEqual(distancia, 1)

    def test_las_rutas_de_a_estrella_respetan_paredes_y_puertas(self):
        model = new_model()
        board = model.board

        for firefighter in model.firefighters:
            origen = (firefighter.row, firefighter.column)

            for poi in board.pois:
                path, _ = astar.find_path(board, origen, (poi.row, poi.column))

                if path is None:
                    continue

                for actual, siguiente in zip(path, path[1:]):
                    puerta = board.get_door_between(*actual, *siguiente)

                    puerta_cerrada = (
                        puerta is not None
                        and not puerta.is_open
                        and not puerta.is_destroyed
                    )

                    # O se puede pasar, o hay una puerta que el plan
                    # incluye abrir. Nunca una pared.
                    self.assertTrue(
                        board.can_move_between(*actual, *siguiente)
                        or puerta_cerrada,
                        f"Paso inválido {actual} -> {siguiente}"
                    )


# =========================================================
# Las reglas del juego no cambiaron
# =========================================================

class TestReglasIntactas(unittest.TestCase):

    def test_cargar_una_victima_sigue_funcionando(self):
        model = new_model()
        board = model.board
        firefighter = model.firefighters[0]

        poi = POIData(row=3, column=3, poi_type="v")
        poi.revealed = True
        board.pois.append(poi)

        firefighter.set_cell(3, 3)
        firefighter.reset_action_points()

        self.assertIn(("pick_up",), firefighter.get_legal_actions())
        self.assertTrue(firefighter.pick_up_victim())
        self.assertTrue(firefighter.carrying_victim)

    def test_rescatar_en_una_salida_sigue_contando(self):
        model = new_model()
        board = model.board
        firefighter = model.firefighters[0]

        firefighter.carrying_victim = True

        salida = board.exits[0]
        firefighter.set_cell(salida.row, salida.column)
        firefighter._on_enter_cell()

        self.assertFalse(firefighter.carrying_victim)
        self.assertEqual(model.victims_rescued, 1)

    def test_las_tres_estrategias_terminan_su_partida(self):
        for nombre in STRATEGIES:
            with self.subTest(estrategia=nombre):
                model = new_model(seed=4, strategy=nombre)
                resultado = model.run_game()

                self.assertIn(
                    resultado,
                    ["victoria", "derrota_victimas", "derrota_colapso"]
                )
                self.assertFalse(model.running)

    def test_misma_semilla_sigue_dando_la_misma_partida(self):
        for nombre in STRATEGIES:
            with self.subTest(estrategia=nombre):
                a = new_model(seed=31, strategy=nombre)
                b = new_model(seed=31, strategy=nombre)

                a.run_game()
                b.run_game()

                self.assertEqual(a.result, b.result)
                self.assertEqual(a.turns, b.turns)
                self.assertEqual(a.victims_rescued, b.victims_rescued)
                self.assertEqual(a.board.damage_markers, b.board.damage_markers)
                self.assertEqual(a.fire_targets, b.fire_targets)

    def test_el_estado_para_unity_sale_del_grid(self):
        """COMPATIBILIDAD CON UNITY: el JSON no cambió de forma. Las
        posiciones que se envían son las del MultiGrid."""
        model = new_model()
        estado = model.get_state()

        esperados = {
            "resultado", "estrategia", "turno", "bombero_actual",
            "rescatadas", "perdidas", "danio",
            "bomberos", "fuegos", "humos", "pois", "puertas",
        }

        self.assertEqual(set(estado.keys()), esperados)
        self.assertEqual(len(estado["bomberos"]), 6)

        for enviado, firefighter in zip(estado["bomberos"], model.firefighters):
            self.assertEqual(
                (enviado["fila"], enviado["columna"]),
                grid_cell(firefighter)
            )


# =========================================================
# MultiGrid y los generadores aleatorios
# =========================================================

class TestMultiGridYRng(unittest.TestCase):

    def test_crear_y_usar_el_grid_no_consume_rng(self):
        """Si MultiGrid gastara números del generador del fuego, las
        comparaciones con semillas pareadas dejarían de ser justas."""
        model = new_model()

        fuego = model.fire_random.getstate()
        poi = model.poi_random.getstate()
        estrategia = model.strategy_random.getstate()

        firefighter = model.firefighters[0]
        firefighter.set_cell(2, 2)
        firefighter.set_cell(3, 2)
        model.grid.get_neighborhood((2, 3), moore=False)
        model.movement_neighbors(3, 2)

        self.assertEqual(fuego, model.fire_random.getstate())
        self.assertEqual(poi, model.poi_random.getstate())
        self.assertEqual(estrategia, model.strategy_random.getstate())

    def test_los_tres_generadores_siguen_siendo_independientes(self):
        model = new_model()

        generadores = [
            model.fire_random,
            model.poi_random,
            model.strategy_random,
        ]

        self.assertEqual(len({id(g) for g in generadores}), 3)

        secuencias = [[g.random() for _ in range(5)] for g in generadores]

        self.assertNotEqual(secuencias[0], secuencias[1])
        self.assertNotEqual(secuencias[0], secuencias[2])
        self.assertNotEqual(secuencias[1], secuencias[2])

    def test_colocar_bomberos_no_usa_el_random_de_mesa(self):
        model = new_model()

        antes = model.random.getstate()
        model.run_game()

        self.assertEqual(antes, model.random.getstate())


# =========================================================
# Reinicio de partida
# =========================================================

class TestReinicio(unittest.TestCase):

    def test_una_partida_nueva_reconstruye_el_grid(self):
        """El servidor reinicia creando un modelo nuevo, así que el
        grid nuevo tiene que venir limpio y completo."""
        primera = new_model(seed=5)
        primera.run_game()

        segunda = new_model(seed=5)

        self.assertIsNot(segunda.grid, primera.grid)
        self.assertIsInstance(segunda.grid, MultiGrid)
        self.assertEqual(segunda.grid.width, 8)
        self.assertEqual(segunda.grid.height, 6)
        self.assertFalse(segunda.grid.torus)

        for firefighter in segunda.firefighters:
            self.assertIsNotNone(firefighter.pos)

    def test_no_quedan_agentes_de_mas_tras_reiniciar(self):
        segunda = new_model(seed=5)

        en_el_grid = [
            agent
            for x in range(segunda.grid.width)
            for y in range(segunda.grid.height)
            for agent in segunda.grid.get_cell_list_contents([(x, y)])
        ]

        self.assertEqual(len(en_el_grid), 6)
        self.assertEqual(len(set(id(a) for a in en_el_grid)), 6)
        self.assertEqual(len(segunda.agents), 6)

        ids = sorted(f.firefighter_id for f in segunda.firefighters)
        self.assertEqual(ids, [1, 2, 3, 4, 5, 6])

    def test_tras_una_partida_completa_siguen_siendo_seis_en_el_grid(self):
        model = new_model(seed=6)
        model.run_game()

        en_el_grid = [
            agent
            for x in range(model.grid.width)
            for y in range(model.grid.height)
            for agent in model.grid.get_cell_list_contents([(x, y)])
        ]

        self.assertEqual(len(en_el_grid), 6)


if __name__ == "__main__":
    unittest.main()
