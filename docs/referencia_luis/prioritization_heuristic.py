"""
╔════════════════════════════════════════════════════════════════╗
║           OBJECTIVE PRIORITIZATION GREEDY HEURISTIC            ║
║                                                                ║
║ Basado en: Rational Learning notebook (clase)                 ║
║           Tomando la estrategia "siempre elige mejor"          ║
║                                                                ║
║ PROPÓSITO: Decidir CUÁL víctima rescatar primero basándose     ║
║           en una evaluación rápida de:                         ║
║           - Distancia                                          ║
║           - Peligro inmediato (fuego cercano)                  ║
║           - Urgencia (si será perdida pronto)                  ║
║                                                                ║
║ COMPLEJIDAD: O(k) donde k = número de víctimas vivas           ║
╚════════════════════════════════════════════════════════════════╝
"""

from typing import List, Tuple, Optional, Dict
from dataclasses import dataclass


# ============================================================================
# DATACLASSES: Estructuras de datos
# ============================================================================

@dataclass
class Victim:
    """Representa una víctima en el tablero."""
    position: Tuple[int, int]      # (row, col)
    is_identified: bool = False    # ¿Se conoce su ubicación?
    is_rescued: bool = False       # ¿Ya fue rescatada?
    is_lost: bool = False          # ¿Se perdió en fuego?
    
    def is_available(self) -> bool:
        """Retorna True si la víctima puede ser rescatada."""
        return not self.is_rescued and not self.is_lost


@dataclass
class Agent:
    """Representa un agente bombero."""
    agent_id: int
    position: Tuple[int, int]
    carrying_victim: bool = False
    action_points: int = 4  # AP al inicio de turno


@dataclass
class PrioritizationScore:
    """
    Resultado de la evaluación de un objetivo.
    
    score_total: número final (mayor = más urgente)
    components: breakdown de cada componente (para debugging)
    """
    score_total: float
    distance_component: float      # Distancia (negativo = penaliza)
    danger_component: float        # Peligro de fuego (penaliza)
    urgency_component: float       # Urgencia (premia)


# ============================================================================
# CLASE: Objective Prioritizer
# ============================================================================

class ObjectivePrioritizer:
    """
    Evalúa víctimas u objetivos y elige el mejor usando heurística greedy.
    
    Basado en: Rational Learning - siempre elige la acción que maximiza
    utilidad esperada en el momento actual.
    
    En este caso: maximiza "urgencia de rescate".
    
    COMPONENTES DEL SCORE:
    1. DISTANCE: cercano = mejor (penaliza distancia)
    2. DANGER: en peligro = más urgente (premia si fuego cerca)
    3. URGENCY: a punto de perderse = crítico (premia por proximidad a fuego)
    
    FÓRMULA:
        score = -distance_peso × distancia 
              + danger_peso × peligro
              + urgency_peso × urgencia
    
    PESOS calibrados para que:
    - Una víctima cercana sea preferible
    - Una víctima en peligro sea MUCHO más preferible
    - Una víctima a punto de perderse sea CRÍTICA
    """
    
    def __init__(
        self,
        distance_weight: float = 1.0,
        danger_weight: float = 3.0,
        urgency_weight: float = 5.0
    ):
        """
        Inicializa el priorizador con pesos para cada componente.
        
        Args:
            distance_weight: penalidad por distancia (defecto: 1.0)
            danger_weight: premio por estar en zona peligrosa (defecto: 3.0)
            urgency_weight: premio por estar a punto de perderse (defecto: 5.0)
        
        Nota: Estos pesos pueden ajustarse para cambiar comportamiento.
              Aumentar urgency_weight = rescata víctimas en peligro primero.
              Aumentar distance_weight = prefiere víctimas cercanas.
        """
        self.distance_weight = distance_weight
        self.danger_weight = danger_weight
        self.urgency_weight = urgency_weight
    
    def evaluate_victim(
        self,
        victim: Victim,
        agent_position: Tuple[int, int],
        fire_map: List[List[int]],
        fire_proximity_threshold: int = 2
    ) -> PrioritizationScore:
        """
        Evalúa UNA víctima y retorna un score.
        
        COMPONENTES:
        
        1. DISTANCE COMPONENT:
           - Distancia Manhattan desde agente a víctima
           - Penaliza distancia (más lejos = peor)
           - Fórmula: -distance_weight × distancia_manhattan
        
        2. DANGER COMPONENT:
           - ¿Hay fuego cerca de la víctima?
           - Si hay fuego dentro de threshold = muy urgente
           - Fórmula: danger_weight si fuego_cercano else 0
        
        3. URGENCY COMPONENT:
           - ¿Está la víctima casi rodeada de fuego?
           - Cuenta cuántas celdas vecinas tiene fuego
           - Si 3+ vecinos con fuego = crítica (urgency máximo)
           - Fórmula: urgency_weight × (vecinos_fuego / 4)
        
        Args:
            victim: Victim a evaluar
            agent_position: (row, col) del agente
            fire_map: matriz booleana donde True = hay fuego
            fire_proximity_threshold: distancia máxima para "fuego cercano"
            
        Returns:
            PrioritizationScore con breakdown completo
        """
        
        # COMPONENTE 1: DISTANCIA
        distance = self._manhattan_distance(agent_position, victim.position)
        distance_component = -self.distance_weight * distance
        
        # COMPONENTE 2: PELIGRO (¿hay fuego cerca?)
        danger_component = 0.0
        if self._is_fire_nearby(victim.position, fire_map, fire_proximity_threshold):
            danger_component = self.danger_weight
        
        # COMPONENTE 3: URGENCIA (¿cuánto fuego tiene alrededor?)
        fire_neighbors = self._count_fire_neighbors(victim.position, fire_map)
        urgency_component = self.urgency_weight * (fire_neighbors / 4.0)
        
        # SCORE TOTAL
        total_score = distance_component + danger_component + urgency_component
        
        return PrioritizationScore(
            score_total=total_score,
            distance_component=distance_component,
            danger_component=danger_component,
            urgency_component=urgency_component
        )
    
    def choose_best_victim(
        self,
        victims: List[Victim],
        agent_position: Tuple[int, int],
        fire_map: List[List[int]]
    ) -> Tuple[Optional[Victim], PrioritizationScore]:
        """
        Elige la MEJOR víctima de la lista usando greedy heuristic.
        
        ALGORITMO (Greedy):
        1. Evalúa CADA víctima disponible
        2. Elige la con MAYOR score (más urgente)
        3. Retorna esa víctima + su score
        
        Nota: DETERMINISTA - siempre elige el mejor.
              (vs epsilon-greedy de Rational Learning que explora)
        
        Args:
            victims: lista de Victim
            agent_position: (row, col) del agente
            fire_map: matriz de fuego
            
        Returns:
            tupla (víctima_elegida, score)
            - víctima_elegida: Victim con mayor score
            - score: PrioritizationScore de esa víctima
            - Retorna (None, dummy_score) si no hay víctimas disponibles
        """
        
        available_victims = [v for v in victims if v.is_available()]
        
        if not available_victims:
            # Dummy score si no hay opciones
            dummy = PrioritizationScore(0, 0, 0, 0)
            return None, dummy
        
        # Evalúa cada víctima
        scores: Dict[Victim, PrioritizationScore] = {}
        for victim in available_victims:
            scores[victim] = self.evaluate_victim(
                victim, agent_position, fire_map
            )
        
        # Elige la con máximo score
        best_victim = max(available_victims, key=lambda v: scores[v].score_total)
        best_score = scores[best_victim]
        
        return best_victim, best_score
    
    # ========== HELPERS PRIVADOS ==========
    
    @staticmethod
    def _manhattan_distance(pos1: Tuple[int, int], pos2: Tuple[int, int]) -> int:
        """Distancia Manhattan entre dos posiciones."""
        row1, col1 = pos1
        row2, col2 = pos2
        return abs(row1 - row2) + abs(col1 - col2)
    
    @staticmethod
    def _is_fire_nearby(
        position: Tuple[int, int],
        fire_map: List[List[int]],
        threshold: int = 2
    ) -> bool:
        """
        Verifica si hay fuego dentro de 'threshold' celdas.
        
        Args:
            position: (row, col)
            fire_map: True/1 = fuego, False/0 = sin fuego
            threshold: distancia máxima
            
        Returns:
            True si hay fuego dentro de threshold
        """
        row, col = position
        
        for r in range(len(fire_map)):
            for c in range(len(fire_map[0]) if len(fire_map) > 0 else 0):
                if fire_map[r][c]:  # Si hay fuego
                    dist = abs(r - row) + abs(c - col)
                    if dist <= threshold:
                        return True
        
        return False
    
    @staticmethod
    def _count_fire_neighbors(
        position: Tuple[int, int],
        fire_map: List[List[int]]
    ) -> int:
        """
        Cuenta cuántos de los 4 vecinos (arriba, abajo, izq, der) tienen fuego.
        
        Args:
            position: (row, col)
            fire_map: matriz de fuego
            
        Returns:
            número de vecinos con fuego (0-4)
        """
        row, col = position
        count = 0
        
        # Arriba
        if row - 1 >= 0 and fire_map[row - 1][col]:
            count += 1
        
        # Abajo
        if row + 1 < len(fire_map) and fire_map[row + 1][col]:
            count += 1
        
        # Izquierda
        if col - 1 >= 0 and fire_map[row][col - 1]:
            count += 1
        
        # Derecha
        if col + 1 < len(fire_map[0]) if len(fire_map) > 0 else False:
            if fire_map[row][col + 1]:
                count += 1
        
        return count


# ============================================================================
# FUNCIONES DE DEMOSTRACIÓN
# ============================================================================

def demo_prioritization():
    """Demo del priorizador con un escenario simple."""
    
    # 3 víctimas
    victims = [
        Victim(position=(1, 1), is_identified=True),  # Cercana, sin peligro
        Victim(position=(4, 5), is_identified=True),  # Media distancia, fuego cerca
        Victim(position=(2, 7), is_identified=True),  # Lejana, rodeada fuego
    ]
    
    # Mapa de fuego (True = hay fuego)
    fire_map = [
        [False, False, False, False, False, False, False, False],
        [False, False, False, False, False, False, False, False],
        [False, False, False, False, False, False, True,  True],
        [False, False, False, False, False, False, True,  False],
        [False, False, False, False, True,  True,  False, False],
        [False, False, False, False, False, False, False, False],
    ]
    
    agent_pos = (0, 0)
    
    prioritizer = ObjectivePrioritizer()
    
    print("=" * 70)
    print("OBJECTIVE PRIORITIZATION DEMO")
    print("=" * 70)
    print(f"Agent position: {agent_pos}\n")
    
    # Evalúa cada víctima
    for i, victim in enumerate(victims):
        score = prioritizer.evaluate_victim(victim, agent_pos, fire_map)
        print(f"Víctima {i+1} at {victim.position}:")
        print(f"  Distance component:  {score.distance_component:7.2f}")
        print(f"  Danger component:    {score.danger_component:7.2f}")
        print(f"  Urgency component:   {score.urgency_component:7.2f}")
        print(f"  ────────────────────────────")
        print(f"  TOTAL SCORE:         {score.score_total:7.2f}")
        print()
    
    # Elige la mejor
    best_victim, best_score = prioritizer.choose_best_victim(
        victims, agent_pos, fire_map
    )
    
    print("-" * 70)
    print(f"✓ BEST CHOICE: Victim at {best_victim.position}")
    print(f"  Score: {best_score.score_total:.2f}")
    print("=" * 70)


if __name__ == "__main__":
    demo_prioritization()
