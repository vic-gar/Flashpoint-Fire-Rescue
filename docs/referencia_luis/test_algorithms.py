"""
╔════════════════════════════════════════════════════════════════╗
║                    UNIT TESTS FOR ALL ALGORITHMS               ║
║                                                                ║
║ Tests cada algoritmo de manera aislada para verificar:         ║
║ 1. Correctitud (produce resultado esperado)                   ║
║ 2. Edge cases (casos límite)                                  ║
║ 3. Performance (complejidad aceptable)                        ║
║                                                                ║
║ RUN: python -m pytest test_algorithms.py -v                   ║
║ o   python test_algorithms.py                                  ║
╚════════════════════════════════════════════════════════════════╝
"""

import unittest
import time
from typing import List

# Importar los algoritmos
import sys
sys.path.insert(0, '.')

from astar_pathfinding import (
    AStarPathfinder, GridHelper, PriorityQueue, CELL_WALKABLE, CELL_FIRE, CELL_WALL
)
from prioritization_heuristic import (
    ObjectivePrioritizer, Victim, Agent
)
from best_response_coordination import (
    BestResponseCoordinator, AssignmentConstraint
)
from replanification_detector import (
    ReplanificationDetector, GameState, ChangeType
)


# ============================================================================
# TEST SUITE 1: A* PATHFINDING
# ============================================================================

class TestAStarPathfinding(unittest.TestCase):
    """Tests del algoritmo A*."""
    
    def setUp(self):
        """Crea un grid de prueba."""
        self.pathfinder = AStarPathfinder()
        
        # Grid simple 6x8
        self.simple_grid = [
            [0, 0, 0, 0, 0, 0, 0, 0],
            [0, 0, 0, 0, 0, 0, 0, 0],
            [0, 0, 0, 0, 0, 0, 0, 0],
            [0, 0, 0, 0, 0, 0, 0, 0],
            [0, 0, 0, 0, 0, 0, 0, 0],
            [0, 0, 0, 0, 0, 0, 0, 0],
        ]
        
        # Grid con obstáculo
        self.grid_with_wall = [
            [0, 0, 0, 0, 0, 0, 0, 0],
            [0, 0, 0, 0, 2, 0, 0, 0],
            [0, 0, 0, 0, 2, 0, 0, 0],
            [0, 0, 0, 0, 2, 0, 0, 0],
            [0, 0, 0, 0, 2, 0, 0, 0],
            [0, 0, 0, 0, 0, 0, 0, 0],
        ]
    
    def test_straight_line_path(self):
        """Prueba: camino recto sin obstáculos."""
        start = (0, 0)
        goal = (0, 7)
        
        ruta, costo, explorados = self.pathfinder.find_path(
            self.simple_grid, start, goal
        )
        
        self.assertIsNotNone(ruta, "Should find a path")
        self.assertEqual(ruta[0], start, "Path should start at start")
        self.assertEqual(ruta[-1], goal, "Path should end at goal")
        self.assertEqual(len(ruta), 8, "Straight line should have 8 cells")
    
    def test_diagonal_path(self):
        """Prueba: camino diagonal (va diagonalmente por movimientos)."""
        start = (0, 0)
        goal = (5, 7)
        
        ruta, costo, explorados = self.pathfinder.find_path(
            self.simple_grid, start, goal
        )
        
        self.assertIsNotNone(ruta, "Should find a path")
        # Distancia Manhattan = 5 + 7 = 12
        # Pero ruta puede ser más larga
        self.assertGreaterEqual(len(ruta), 12, "Path length >= Manhattan distance")
    
    def test_path_around_obstacle(self):
        """Prueba: debe ir alrededor del muro."""
        start = (0, 4)
        goal = (5, 4)
        
        ruta, costo, explorados = self.pathfinder.find_path(
            self.grid_with_wall, start, goal
        )
        
        self.assertIsNotNone(ruta, "Should find a path around wall")
        
        # Verificar que NO passa por el muro
        for pos in ruta:
            row, col = pos
            self.assertNotEqual(
                self.grid_with_wall[row][col], 2,
                f"Path should not pass through wall at {pos}"
            )
    
    def test_invalid_start(self):
        """Prueba: posición inicial inválida."""
        start = (-1, -1)
        goal = (5, 7)
        
        ruta, costo, explorados = self.pathfinder.find_path(
            self.simple_grid, start, goal
        )
        
        self.assertIsNone(ruta, "Should return None for invalid start")
    
    def test_manhattan_heuristic(self):
        """Prueba: la heurística de Manhattan es admisible."""
        pos1 = (0, 0)
        pos2 = (5, 7)
        
        manhattan = GridHelper.manhattan_distance(pos1, pos2)
        
        # Manhattan nunca debe sobrestimar
        self.assertEqual(manhattan, 12, "Manhattan distance should be 5+7=12")
        self.assertLessEqual(manhattan, 12, "Heuristic is admissible")
    
    def test_performance_large_grid(self):
        """Prueba: rendimiento en grid grande sin obstáculos."""
        start = (0, 0)
        goal = (5, 7)
        
        start_time = time.time()
        ruta, costo, explorados = self.pathfinder.find_path(
            self.simple_grid, start, goal
        )
        elapsed = time.time() - start_time
        
        self.assertLess(elapsed, 0.1, "A* should complete in < 100ms")
        self.assertLess(explorados, 48, "Should not explore all 48 cells")


# ============================================================================
# TEST SUITE 2: PRIORITIZATION HEURISTIC
# ============================================================================

class TestPrioritizationHeuristic(unittest.TestCase):
    """Tests del priorizador de objetivos."""
    
    def setUp(self):
        """Configura priorizador."""
        self.prioritizer = ObjectivePrioritizer(
            distance_weight=1.0,
            danger_weight=3.0,
            urgency_weight=5.0
        )
        
        self.fire_map = [
            [False, False, False, False, False, False, False, False],
            [False, False, False, False, False, False, False, False],
            [False, False, False, False, False, False, True,  True],
            [False, False, False, False, False, False, True,  False],
            [False, False, False, False, True,  True,  False, False],
            [False, False, False, False, False, False, False, False],
        ]
    
    def test_prefer_closer_victim(self):
        """Prueba: prefiere víctima cercana."""
        victims = [
            Victim(position=(0, 0), is_identified=True),
            Victim(position=(5, 7), is_identified=True),
        ]
        
        agent_pos = (0, 0)
        
        best_victim, score = self.prioritizer.choose_best_victim(
            victims, agent_pos, self.fire_map
        )
        
        self.assertEqual(
            best_victim.position, (0, 0),
            "Should prefer closer victim"
        )
    
    def test_prefer_victim_in_danger(self):
        """Prueba: prefiere víctima en peligro de fuego."""
        victims = [
            Victim(position=(1, 1), is_identified=True),   # Segura, lejana
            Victim(position=(2, 7), is_identified=True),   # Peligro, fuego cerca
        ]
        
        agent_pos = (0, 0)
        
        best_victim, score = self.prioritizer.choose_best_victim(
            victims, agent_pos, self.fire_map
        )
        
        # Debería preferir la en peligro a pesar de estar más lejos
        self.assertNotEqual(
            best_victim.position, (1, 1),
            "Should prefer victim in danger over safe one"
        )
    
    def test_no_available_victims(self):
        """Prueba: no hay víctimas disponibles."""
        victims = [
            Victim(position=(0, 0), is_identified=True, is_rescued=True),
            Victim(position=(5, 7), is_identified=True, is_lost=True),
        ]
        
        agent_pos = (0, 0)
        
        best_victim, score = self.prioritizer.choose_best_victim(
            victims, agent_pos, self.fire_map
        )
        
        self.assertIsNone(best_victim, "Should return None if no available victims")
    
    def test_score_components(self):
        """Prueba: componentes de score calculan correctamente."""
        victim = Victim(position=(2, 2), is_identified=True)
        agent_pos = (0, 0)
        
        score = self.prioritizer.evaluate_victim(victim, agent_pos, self.fire_map)
        
        # Distancia = 4, debe penalizar
        self.assertLess(score.distance_component, 0, "Distance component should be negative")
        
        # Score total debe ser suma de componentes
        total = (score.distance_component + score.danger_component + 
                 score.urgency_component)
        self.assertAlmostEqual(score.score_total, total, places=5)


# ============================================================================
# TEST SUITE 3: BEST RESPONSE COORDINATION
# ============================================================================

class TestBestResponseCoordination(unittest.TestCase):
    """Tests de coordinación."""
    
    def setUp(self):
        """Configura coordinador."""
        self.coordinator = BestResponseCoordinator(num_agents=4)
    
    def test_no_duplicate_assignments(self):
        """Prueba: no asigna mismo objetivo a dos agentes."""
        # Asignar objetivo 1 a agente 0
        success1 = self.coordinator.assign_objective(agent_id=0, objective_id=1)
        self.assertTrue(success1, "First assignment should succeed")
        
        # Intentar asignar mismo objetivo a agente 1
        success2 = self.coordinator.assign_objective(agent_id=1, objective_id=1)
        self.assertFalse(success2, "Should not allow duplicate assignment")
    
    def test_release_objective(self):
        """Prueba: libera objetivo después de completarlo."""
        # Asignar
        self.coordinator.assign_objective(0, 1)
        
        # Liberar
        self.coordinator.release_objective(1)
        
        # Vuelve a asignar
        success = self.coordinator.assign_objective(1, 1)
        self.assertTrue(success, "Should allow reassignment after release")
    
    def test_best_response_calculation(self):
        """Prueba: calcula mejor respuesta correctamente."""
        available_objs = [1, 2, 3, 4]
        other_choices = {0: 1, 1: 1, 2: 2}  # Dos agentes en obj 1
        
        best_obj, utility = self.coordinator.calculate_best_response(
            agent_id=3, 
            available_objectives=available_objs,
            other_agents_choices=other_choices
        )
        
        # No debe elegir objetivo 1 (saturado)
        self.assertNotEqual(
            best_obj, 1,
            "Should not choose saturated objective"
        )


# ============================================================================
# TEST SUITE 4: REPLANIFICATION DETECTOR
# ============================================================================

class TestReplanificationDetector(unittest.TestCase):
    """Tests del detector de replanificación."""
    
    def setUp(self):
        """Configura detector."""
        self.detector = ReplanificationDetector()
    
    def test_detect_fire_appeared(self):
        """Prueba: detecta nuevo fuego."""
        prev_state = GameState(
            fire_map=[[False] * 8 for _ in range(6)],
            door_states={},
            rescued_victims=set(),
            lost_victims=set(),
            agent_positions={0: (0, 0)},
            turn_number=1
        )
        
        curr_state = prev_state.copy()
        curr_state.fire_map[2][2] = True  # Nuevo fuego
        
        changes = self.detector.detect_changes(prev_state, curr_state)
        
        fire_changes = [c for c in changes if c.change_type == ChangeType.FIRE_APPEARED]
        self.assertEqual(len(fire_changes), 1, "Should detect 1 new fire")
        self.assertEqual(fire_changes[0].position, (2, 2))
    
    def test_detect_victim_lost(self):
        """Prueba: detecta víctima perdida."""
        prev_state = GameState(
            fire_map=[[False] * 8 for _ in range(6)],
            door_states={},
            rescued_victims=set(),
            lost_victims=set(),
            agent_positions={0: (0, 0)},
            turn_number=1
        )
        
        curr_state = prev_state.copy()
        curr_state.lost_victims.add(5)  # Víctima 5 perdida
        
        changes = self.detector.detect_changes(prev_state, curr_state)
        
        loss_changes = [c for c in changes if c.change_type == ChangeType.VICTIM_LOST]
        self.assertEqual(len(loss_changes), 1, "Should detect 1 lost victim")
    
    def test_should_replan_with_nearby_fire(self):
        """Prueba: detecta si debe replanificar por fuego cercano."""
        fire_map = [
            [False, False, False, False, False, False, False, False],
            [False, False, True,  False, False, False, False, False],
            [False, False, False, False, False, False, False, False],
            [False, False, False, False, False, False, False, False],
            [False, False, False, False, False, False, False, False],
            [False, False, False, False, False, False, False, False],
        ]
        
        should_replan = self.detector.should_replan_path(
            agent_id=0,
            current_position=(0, 0),
            goal_position=(5, 7),
            fire_map=fire_map
        )
        
        # Fire está cerca (distancia 2)
        self.assertTrue(should_replan, "Should replan with nearby fire")


# ============================================================================
# RUNNER
# ============================================================================

if __name__ == "__main__":
    # Configurar el loader para importar de este directorio
    import sys
    import importlib.util
    
    # Cargar módulos manualmente
    for module_num in [1, 2, 3, 4]:
        module_name = f"algorithms_{module_num:02d}_*"
    
    # Correr tests
    print("=" * 70)
    print("RUNNING UNIT TESTS FOR FLASHPOINT ALGORITHMS")
    print("=" * 70)
    print()
    
    loader = unittest.TestLoader()
    suite = unittest.TestSuite()
    
    # Agregar todos los tests
    suite.addTests(loader.loadTestsFromTestCase(TestAStarPathfinding))
    suite.addTests(loader.loadTestsFromTestCase(TestPrioritizationHeuristic))
    suite.addTests(loader.loadTestsFromTestCase(TestBestResponseCoordination))
    suite.addTests(loader.loadTestsFromTestCase(TestReplanificationDetector))
    
    runner = unittest.TextTestRunner(verbosity=2)
    result = runner.run(suite)
    
    print()
    print("=" * 70)
    if result.wasSuccessful():
        print("✓ ALL TESTS PASSED!")
    else:
        print("✗ SOME TESTS FAILED")
    print("=" * 70)
