"""
╔════════════════════════════════════════════════════════════════╗
║              REPLANIFICATION DETECTOR LOGIC                    ║
║                                                                ║
║ Basado en: Estado de la simulación (clase)                    ║
║           Monitoreo de cambios que invalidan planes.           ║
║                                                                ║
║ PROPÓSITO: Detectar RÁPIDAMENTE cuándo un agente debe          ║
║           recalcular su ruta/objetivo porque algo cambió:      ║
║           - Nuevo fuego apareció                               ║
║           - Puerta se cerró (bloqueó camino)                   ║
║           - Objetivo completado                                ║
║           - Víctima perdida en fuego                           ║
║                                                                ║
║ COMPLEJIDAD: O(n) comparación estado anterior vs actual        ║
╚════════════════════════════════════════════════════════════════╝
"""

from typing import List, Tuple, Set, Dict, Optional
from dataclasses import dataclass, field
from enum import Enum


# ============================================================================
# ENUMS & DATACLASSES
# ============================================================================

class ChangeType(Enum):
    """Tipos de cambios que requieren replanificación."""
    FIRE_APPEARED = "fire_appeared"          # Nuevo fuego en celda
    FIRE_EXTINGUISHED = "fire_extinguished"  # Fuego fue extinguido
    DOOR_CLOSED = "door_closed"              # Puerta se cerró
    DOOR_OPENED = "door_opened"              # Puerta se abrió
    VICTIM_RESCUED = "victim_rescued"        # Víctima fue rescatada
    VICTIM_LOST = "victim_lost"              # Víctima se perdió en fuego
    OBJECTIVE_REACHED = "objective_reached"  # Agente llegó a objetivo
    INVALID_PATH = "invalid_path"            # Camino ya no válido


@dataclass
class BoardChange:
    """Representa un cambio en el tablero."""
    change_type: ChangeType
    position: Optional[Tuple[int, int]] = None  # Dónde ocurrió
    agent_id: Optional[int] = None              # Qué agente afectado
    affected_paths: Set[int] = field(default_factory=set)  # Qué agentes replanificar


@dataclass
class GameState:
    """
    Snapshot del estado del juego.
    
    Usado para detectar cambios comparando estado anterior vs actual.
    """
    fire_map: List[List[bool]]                  # True = hay fuego
    door_states: Dict[Tuple[int, int], bool]   # True = cerrada
    rescued_victims: Set[int]                   # IDs de víctimas rescatadas
    lost_victims: Set[int]                      # IDs de víctimas perdidas
    agent_positions: Dict[int, Tuple[int, int]]  # Posición de cada agente
    turn_number: int = 0
    
    def copy(self) -> 'GameState':
        """Crea una copia profunda del estado."""
        return GameState(
            fire_map=[row[:] for row in self.fire_map],
            door_states=self.door_states.copy(),
            rescued_victims=self.rescued_victims.copy(),
            lost_victims=self.lost_victims.copy(),
            agent_positions=self.agent_positions.copy(),
            turn_number=self.turn_number
        )


# ============================================================================
# CLASE: Replanification Detector
# ============================================================================

class ReplanificationDetector:
    """
    Detecta cambios en el tablero que invalidan planes actuales.
    
    FILOSOFÍA:
    - Los agentes hacen planes (rutas A*, objetivos prioritarios)
    - El juego es dinámico (fuego propaga, puertas cierran)
    - Si algo IMPORTANTE cambia, el plan ya no es válido
    - MUST replanificar rápidamente
    
    CAMBIOS QUE REQUIEREN REPLANIFICACIÓN:
    
    1. FIRE (crítico):
       - Nuevo fuego bloquea ruta actual?
       - → Si, REPLANIFICAR A*
    
    2. DOORS (crítico):
       - Puerta se cerró en mi camino?
       - → Si, REPLANIFICAR A*
    
    3. VICTIMS (importante):
       - Víctima que iba a rescatar se perdió?
       - → Si, CAMBIAR OBJETIVO (Prioritizer)
    
    4. POSITION (contextual):
       - Agente llegó a su objetivo?
       - → Si, ELEGIR NUEVO OBJETIVO
    
    COMPLEJIDAD:
    - Detección: O(n) comparación de mapas
    - Bastante rápido: < 10ms para grid 6x8
    """
    
    def __init__(self):
        """Inicializa detector."""
        self.previous_state: Optional[GameState] = None
        self.changes_this_turn: List[BoardChange] = []
    
    def detect_changes(
        self,
        previous_state: GameState,
        current_state: GameState
    ) -> List[BoardChange]:
        """
        Compara estado anterior vs actual y detecta cambios.
        
        ALGORITMO:
        1. Comparar mapa de fuego
        2. Comparar estado de puertas
        3. Comparar víctimas rescatadas/perdidas
        4. Comparar posiciones de agentes
        5. Agrupar cambios por agente afectado
        
        Args:
            previous_state: estado del turno anterior
            current_state: estado actual
            
        Returns:
            lista de BoardChange que ocurrieron
        """
        
        self.changes_this_turn = []
        
        # 1. DETECTAR CAMBIOS DE FUEGO
        self._detect_fire_changes(previous_state, current_state)
        
        # 2. DETECTAR CAMBIOS DE PUERTAS
        self._detect_door_changes(previous_state, current_state)
        
        # 3. DETECTAR CAMBIOS DE VÍCTIMAS
        self._detect_victim_changes(previous_state, current_state)
        
        # 4. DETECTAR CAMBIOS DE POSICIÓN
        self._detect_position_changes(previous_state, current_state)
        
        # 5. MARCAR AGENTES AFECTADOS
        self._mark_affected_agents(current_state)
        
        return self.changes_this_turn
    
    def should_replan_path(
        self,
        agent_id: int,
        current_position: Tuple[int, int],
        goal_position: Tuple[int, int],
        fire_map: List[List[bool]]
    ) -> bool:
        """
        Verifica si debe replanificar A* para un agente.
        
        RAZONES PARA REPLANIFICAR:
        1. Hay nuevo fuego EN su ruta actual
        2. Su camino fue bloqueado (puerta cerró)
        3. Llegó al destino
        
        Args:
            agent_id: agente a revisar
            current_position: donde está ahora
            goal_position: a dónde va
            fire_map: mapa actual de fuego
            
        Returns:
            True si debe replanificar
        """
        
        # Simple heuristic: si hay fuego cerca, replanificar
        fire_threshold = 2  # Distancia máxima para considerar "cerca"
        
        distance_to_fire = self._distance_to_nearest_fire(
            current_position, fire_map, fire_threshold
        )
        
        # Si fuego está en radio, replanificar
        if distance_to_fire <= fire_threshold:
            return True
        
        # Si llegó al destino, también replanificar (para nuevo objetivo)
        if current_position == goal_position:
            return True
        
        return False
    
    def should_change_objective(
        self,
        target_victim_id: int,
        lost_victims: Set[int]
    ) -> bool:
        """
        Verifica si debe cambiar de objetivo (víctima).
        
        Args:
            target_victim_id: víctima que estaba rescatando
            lost_victims: IDs de víctimas perdidas
            
        Returns:
            True si víctima se perdió
        """
        return target_victim_id in lost_victims
    
    def get_changes_for_agent(self, agent_id: int) -> List[BoardChange]:
        """Retorna cambios que afectan a un agente específico."""
        return [c for c in self.changes_this_turn if agent_id in c.affected_paths]
    
    # ========== MÉTODOS PRIVADOS DE DETECCIÓN ==========
    
    def _detect_fire_changes(
        self,
        prev: GameState,
        curr: GameState
    ) -> None:
        """Detecta fuego nuevo o extinguido."""
        
        for row in range(len(curr.fire_map)):
            for col in range(len(curr.fire_map[0])):
                
                was_fire = prev.fire_map[row][col]
                is_fire = curr.fire_map[row][col]
                
                if not was_fire and is_fire:
                    # Fuego NUEVO
                    change = BoardChange(
                        change_type=ChangeType.FIRE_APPEARED,
                        position=(row, col)
                    )
                    self.changes_this_turn.append(change)
                
                elif was_fire and not is_fire:
                    # Fuego extinguido
                    change = BoardChange(
                        change_type=ChangeType.FIRE_EXTINGUISHED,
                        position=(row, col)
                    )
                    self.changes_this_turn.append(change)
    
    def _detect_door_changes(
        self,
        prev: GameState,
        curr: GameState
    ) -> None:
        """Detecta puertas que se abrieron/cerraron."""
        
        all_doors = set(prev.door_states.keys()) | set(curr.door_states.keys())
        
        for door_pos in all_doors:
            was_closed = prev.door_states.get(door_pos, False)
            is_closed = curr.door_states.get(door_pos, False)
            
            if not was_closed and is_closed:
                # Puerta CERRADA
                change = BoardChange(
                    change_type=ChangeType.DOOR_CLOSED,
                    position=door_pos
                )
                self.changes_this_turn.append(change)
            
            elif was_closed and not is_closed:
                # Puerta abierta
                change = BoardChange(
                    change_type=ChangeType.DOOR_OPENED,
                    position=door_pos
                )
                self.changes_this_turn.append(change)
    
    def _detect_victim_changes(
        self,
        prev: GameState,
        curr: GameState
    ) -> None:
        """Detecta víctimas rescatadas o perdidas."""
        
        # Víctimas rescatadas NUEVAS
        newly_rescued = curr.rescued_victims - prev.rescued_victims
        for victim_id in newly_rescued:
            change = BoardChange(
                change_type=ChangeType.VICTIM_RESCUED,
                agent_id=None  # Unknown which agent
            )
            self.changes_this_turn.append(change)
        
        # Víctimas PERDIDAS nuevas
        newly_lost = curr.lost_victims - prev.lost_victims
        for victim_id in newly_lost:
            change = BoardChange(
                change_type=ChangeType.VICTIM_LOST,
                agent_id=None
            )
            self.changes_this_turn.append(change)
    
    def _detect_position_changes(
        self,
        prev: GameState,
        curr: GameState
    ) -> None:
        """Detecta agentes que alcanzaron objetivos."""
        
        for agent_id, curr_pos in curr.agent_positions.items():
            prev_pos = prev.agent_positions.get(agent_id, None)
            
            if prev_pos and curr_pos != prev_pos:
                # Agente se movió (potentially reached objective)
                change = BoardChange(
                    change_type=ChangeType.OBJECTIVE_REACHED,
                    position=curr_pos,
                    agent_id=agent_id
                )
                self.changes_this_turn.append(change)
    
    def _mark_affected_agents(self, state: GameState) -> None:
        """Marca qué agentes se ven afectados por cada cambio."""
        
        for change in self.changes_this_turn:
            # Si el cambio afecta a un agente específico
            if change.agent_id is not None:
                change.affected_paths.add(change.agent_id)
            
            # Si es fuego/puerta, afecta a agentes cercanos
            if change.position and change.change_type in [
                ChangeType.FIRE_APPEARED,
                ChangeType.DOOR_CLOSED
            ]:
                for agent_id, agent_pos in state.agent_positions.items():
                    dist = abs(agent_pos[0] - change.position[0]) + \
                           abs(agent_pos[1] - change.position[1])
                    
                    # Si está dentro de 3 celdas, afecta
                    if dist <= 3:
                        change.affected_paths.add(agent_id)
    
    @staticmethod
    def _distance_to_nearest_fire(
        position: Tuple[int, int],
        fire_map: List[List[bool]],
        max_distance: int
    ) -> int:
        """Encuentra la distancia al fuego más cercano."""
        
        min_dist = float('inf')
        
        for row in range(len(fire_map)):
            for col in range(len(fire_map[0])):
                if fire_map[row][col]:
                    dist = abs(position[0] - row) + abs(position[1] - col)
                    min_dist = min(min_dist, dist)
        
        return min_dist if min_dist != float('inf') else max_distance + 1


# ============================================================================
# FUNCIONES DE DEMOSTRACIÓN
# ============================================================================

def demo_replanification():
    """Demo de detección de cambios."""
    
    print("=" * 70)
    print("REPLANIFICATION DETECTOR DEMO")
    print("=" * 70)
    
    # Estado anterior (turno 5)
    prev_state = GameState(
        fire_map=[
            [False, False, False, False, False, False, False, False],
            [False, False, True,  False, False, False, False, False],
            [False, False, False, False, False, False, False, False],
            [False, False, False, False, False, False, False, False],
            [False, False, False, False, False, False, False, False],
            [False, False, False, False, False, False, False, False],
        ],
        door_states={(2, 4): False, (3, 2): False},
        rescued_victims={0},
        lost_victims=set(),
        agent_positions={0: (0, 0), 1: (1, 5), 2: (2, 2), 3: (5, 7)},
        turn_number=5
    )
    
    # Estado actual (turno 6) - CAMBIOS OCURRIERON
    curr_state = GameState(
        fire_map=[
            [False, False, False, False, False, False, False, False],
            [False, False, True,  False, True,  False, False, False],  # ← Nuevo fuego
            [False, False, False, False, False, False, False, False],
            [False, False, False, False, False, False, False, False],
            [False, False, False, False, False, False, False, False],
            [False, False, False, False, False, False, False, False],
        ],
        door_states={(2, 4): True, (3, 2): False},  # ← Puerta cerrada
        rescued_victims={0, 1},  # ← Nueva víctima rescatada
        lost_victims=set(),
        agent_positions={0: (0, 1), 1: (1, 5), 2: (2, 3), 3: (5, 7)},  # ← Se movieron
        turn_number=6
    )
    
    detector = ReplanificationDetector()
    changes = detector.detect_changes(prev_state, curr_state)
    
    print(f"\nChanges detected: {len(changes)}\n")
    
    for i, change in enumerate(changes, 1):
        print(f"{i}. {change.change_type.value}")
        if change.position:
            print(f"   Position: {change.position}")
        if change.affected_paths:
            print(f"   Affected agents: {change.affected_paths}")
    
    print("\n" + "-" * 70)
    print("Agent replan decisions:")
    print("-" * 70)
    
    for agent_id in range(4):
        changes_for_agent = detector.get_changes_for_agent(agent_id)
        should_replan = detector.should_replan_path(
            agent_id,
            curr_state.agent_positions[agent_id],
            (5, 7),  # ejemplo goal
            curr_state.fire_map
        )
        print(f"Agent {agent_id}: {len(changes_for_agent)} changes → Replan: {should_replan}")
    
    print("=" * 70)


if __name__ == "__main__":
    demo_replanification()
