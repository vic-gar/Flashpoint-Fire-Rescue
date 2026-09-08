"""Pruebas de la separación de generadores aleatorios.

La comparación entre estrategias con semillas pareadas solo es justa si
las decisiones al azar de una estrategia no cambian los dados del fuego
ni el mazo de POI. Estas pruebas usan el mecanismo real del modelo
(sus generadores, su fase del fuego y su estrategia aleatoria), no
variables preparadas a mano.

Ejecutar desde la carpeta Server:

    python -m unittest discover -s tests -v
"""

import os
import random
import sys
import unittest

sys.path.insert(
    0,
    os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
)

from model import fire_phase
from model.flashpoint_model import FlashPointModel
from strategies import STRATEGIES, get_strategy
from strategies.random_strategy import random_strategy


SERVER_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BOARD_FILE = os.path.join(SERVER_DIR, "data", "final.txt")


def new_model(seed, strategy="aleatoria"):
    return FlashPointModel(
        board_file=BOARD_FILE,
        seed=seed,
        strategy=get_strategy(strategy)
    )


def game_summary(model):
    """Lo que tiene que coincidir para decir que dos partidas son iguales."""
    return (
        model.result,
        model.turns,
        model.victims_rescued,
        model.victims_lost,
        model.board.damage_markers,
        model.knock_downs,
        model.replanifications,
        tuple(model.fire_targets),
        tuple((f.row, f.column) for f in model.firefighters),
    )


def consume_random_decisions(model, times=30):
    """Pide decisiones a la estrategia aleatoria sin ejecutarlas.

    Es exactamente la llamada que hace el modelo en cada acción, así
    que consume el RNG de estrategia por el camino real.
    """
    firefighter = model.current_firefighter
    firefighter.reset_action_points()

    for _ in range(times):
        random_strategy(model, firefighter)


# =========================================================
# Los generadores existen y son distintos
# =========================================================

class TestGeneradores(unittest.TestCase):

    def test_hay_tres_generadores_distintos(self):
        model = new_model(seed=1)

        generators = [
            model.fire_random,
            model.poi_random,
            model.strategy_random,
        ]

        for generator in generators:
            self.assertIsInstance(generator, random.Random)

        # Tres objetos distintos entre sí y distintos del de Mesa.
        self.assertEqual(len({id(g) for g in generators}), 3)

        for generator in generators:
            self.assertIsNot(generator, model.random)

    def test_los_generadores_no_producen_la_misma_secuencia(self):
        """Si los tres arrancaran iguales, las decisiones aleatorias
        estarían correlacionadas con los dados del fuego."""
        model = new_model(seed=1)

        fire = [model.fire_random.random() for _ in range(5)]
        poi = [model.poi_random.random() for _ in range(5)]
        strategy = [model.strategy_random.random() for _ in range(5)]

        self.assertNotEqual(fire, poi)
        self.assertNotEqual(fire, strategy)
        self.assertNotEqual(poi, strategy)


# =========================================================
# Reproducibilidad
# =========================================================

class TestReproducibilidad(unittest.TestCase):

    def test_misma_semilla_y_misma_estrategia_dan_la_misma_partida(self):
        for name in STRATEGIES:
            with self.subTest(estrategia=name):
                a = new_model(seed=123, strategy=name)
                b = new_model(seed=123, strategy=name)

                a.run_game()
                b.run_game()

                self.assertEqual(game_summary(a), game_summary(b))

    def test_semillas_distintas_dan_dados_distintos(self):
        """Control: si esto fallara, la prueba anterior no diría nada."""
        a = new_model(seed=1, strategy="mejorada")
        b = new_model(seed=2, strategy="mejorada")

        a.run_game()
        b.run_game()

        self.assertNotEqual(a.fire_targets, b.fire_targets)

    def test_las_fases_de_fuego_quedan_registradas(self):
        """Hay una fase de fuego por turno, más una si la partida
        terminó justo en la fase del fuego."""
        model = new_model(seed=4, strategy="mejorada")
        model.run_game()

        self.assertGreaterEqual(len(model.fire_targets), model.turns)
        self.assertLessEqual(len(model.fire_targets), model.turns + 1)


# =========================================================
# Misma semilla, distinta estrategia: mismo entorno
# =========================================================

class TestEntornoCompartido(unittest.TestCase):

    def test_los_dados_del_fuego_son_los_mismos_entre_estrategias(self):
        """Mientras dos partidas hayan jugado el mismo número de fases
        de fuego, la celda que salió en cada fase debe ser idéntica."""
        for seed in range(5):
            with self.subTest(semilla=seed):
                random_model = new_model(seed=seed, strategy="aleatoria")
                improved_model = new_model(seed=seed, strategy="mejorada")

                random_model.run_game()
                improved_model.run_game()

                shared = min(
                    len(random_model.fire_targets),
                    len(improved_model.fire_targets)
                )

                # Con menos de 10 fases la comparación diría poco.
                self.assertGreater(shared, 10)

                self.assertEqual(
                    random_model.fire_targets[:shared],
                    improved_model.fire_targets[:shared]
                )

    def test_el_mazo_de_poi_es_el_mismo_entre_estrategias(self):
        for seed in range(5):
            with self.subTest(semilla=seed):
                decks = [
                    new_model(seed=seed, strategy=name).poi_deck
                    for name in STRATEGIES
                ]

                for deck in decks[1:]:
                    self.assertEqual(decks[0], deck)

    def test_el_mazo_de_poi_si_se_baraja(self):
        """Control: el mazo no debe salir en el orden en que se arma."""
        unshuffled = None
        shuffled_seeds = 0

        for seed in range(5):
            model = new_model(seed=seed)

            if unshuffled is None:
                unshuffled = (
                    ["v"] * model.poi_deck.count("v")
                    + ["f"] * model.poi_deck.count("f")
                )

            if model.poi_deck != unshuffled:
                shuffled_seeds += 1

        self.assertGreater(shuffled_seeds, 0)


# =========================================================
# Las decisiones al azar no tocan el entorno
# =========================================================

class TestDecisionesNoMuevenElEntorno(unittest.TestCase):

    def test_consumir_decisiones_aleatorias_no_cambia_la_tirada_del_fuego(self):
        """La prueba central: dos modelos con la misma semilla; en uno
        se piden muchas decisiones a la estrategia aleatoria y en el
        otro ninguna. Las siguientes tiradas del fuego deben ser
        exactamente las mismas."""
        with_decisions = new_model(seed=42)
        without_decisions = new_model(seed=42)

        consume_random_decisions(with_decisions, times=30)

        for _ in range(10):
            self.assertEqual(
                fire_phase.roll_target(with_decisions),
                fire_phase.roll_target(without_decisions)
            )

    def test_consumir_decisiones_aleatorias_no_cambia_la_reposicion_de_poi(self):
        with_decisions = new_model(seed=42)
        without_decisions = new_model(seed=42)

        consume_random_decisions(with_decisions, times=30)

        for _ in range(10):
            self.assertEqual(
                fire_phase.roll_target(
                    with_decisions,
                    with_decisions.poi_random
                ),
                fire_phase.roll_target(
                    without_decisions,
                    without_decisions.poi_random
                )
            )

    def test_una_fase_de_fuego_completa_tras_decisiones_es_identica(self):
        """Lo mismo pero por el camino completo: advance_fire tira
        los dados por su cuenta y debe caer en la misma celda."""
        with_decisions = new_model(seed=42)
        without_decisions = new_model(seed=42)

        consume_random_decisions(with_decisions, times=30)

        fire_phase.advance_fire(with_decisions)
        fire_phase.advance_fire(without_decisions)

        self.assertEqual(
            with_decisions.fire_targets,
            without_decisions.fire_targets
        )

    def test_control_consumir_el_rng_del_fuego_si_cambia_la_tirada(self):
        """Control negativo: si alguien usara el generador del fuego
        para otra cosa, las tiradas sí se moverían. Demuestra que la
        prueba central puede fallar."""
        touched = new_model(seed=42)
        untouched = new_model(seed=42)

        touched.fire_random.random()

        rolls_touched = [fire_phase.roll_target(touched) for _ in range(10)]
        rolls_untouched = [fire_phase.roll_target(untouched) for _ in range(10)]

        self.assertNotEqual(rolls_touched, rolls_untouched)

    def test_la_estrategia_aleatoria_si_consume_su_generador(self):
        """Control: el RNG de estrategia es el que se mueve."""
        model = new_model(seed=42)
        before = model.strategy_random.getstate()

        consume_random_decisions(model, times=5)

        self.assertNotEqual(before, model.strategy_random.getstate())

    def test_la_mejorada_no_consume_el_generador_de_estrategia(self):
        """La mejorada es determinista: en una partida completa no
        toca el RNG de estrategia."""
        model = new_model(seed=5, strategy="mejorada")
        before = model.strategy_random.getstate()

        model.run_game()

        self.assertEqual(before, model.strategy_random.getstate())

    def test_el_generador_de_mesa_no_se_usa_en_toda_la_partida(self):
        """Ninguna fuente accidental: el random de Mesa queda igual
        antes y después de una partida completa, con cualquier
        estrategia."""
        for name in STRATEGIES:
            with self.subTest(estrategia=name):
                model = new_model(seed=7, strategy=name)

                before_random = model.random.getstate()
                before_rng = model.rng.bit_generator.state

                model.run_game()

                self.assertEqual(before_random, model.random.getstate())
                self.assertEqual(before_rng, model.rng.bit_generator.state)


if __name__ == "__main__":
    unittest.main()
