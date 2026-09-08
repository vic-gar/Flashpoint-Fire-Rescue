"""
╔════════════════════════════════════════════════════════════════╗
║                BEST RESPONSE COORDINATION ALGORITHM            ║
║                                                                ║
║ Basado en: Fictional Game notebook (clase)                    ║
║           Los robots observan al oponente y calculan           ║
║           la mejor respuesta a su estrategia observada.        ║
║                                                                ║
║ PROPÓSITO: Evitar que dos agentes rescaten la MISMA víctima    ║
║           mediante observación y coordinación.                 ║
║                                                                ║
║           Si todos observan a qué vans los demás,              ║
║           pueden elegir objetivos diferentes.                  ║
║                                                                ║
║ COMPLEJIDAD: O(n²) donde n = número de agentes                 ║
╚════════════════════════════════════════════════════════════════╝
"""

from typing import List, Dict, Tuple, Optional, Set
from dataclasses import dataclass, field
from collections import Counter


# ============================================================================
# DATACLASSES
# ============================================================================

@dataclass
class CoordinationHistory:
    """
    Historial de observaciones de QUÉ objetivo eligió cada agente.
    
    Usado para calcular frecuencias de decisiones pasadas.
    """
    objective_history: Dict[int, List[int]] = field(default_factory=dict)
    # objective_history[agent_id] = [obj_1, obj_2, obj_3, ...]
    
    def observe_choice(self, agent_id: int, objective_id: int) -> None:
        """Registra que agente_id eligió objetivo_id."""
        if agent_id not in self.objective_history:
            self.objective_history[agent_id] = []
        self.objective_history[agent_id].append(objective_id)
    
    def get_frequency(self, agent_id: int, objective_id: int) -> float:
        """
        Calcula: P(agente_id = objetivo_id) = veces_observado / total
        
        Usa smoothing (pseudocount = 1) para evitar dividir por 0.
        """
        if agent_id not in self.objective_history:
            return 0.5  # Default: sin información
        
        choices = self.objective_history[agent_id]
        if not choices:
            return 0.5
        
        times_chosen = choices.count(objective_id)
        # Smoothing: agregamos 1 a cada observación (Laplace smoothing)
        return (times_chosen + 1) / (len(choices) + 2)


@dataclass
class AssignmentConstraint:
    """
    Restricción de asignación: qué objetivo está asignado a quién.
    """
    objective_to_agent: Dict[int, int] = field(default_factory=dict)
    # objective_id -> agent_id (si -1 = sin asignar)
    
    def assign(self, objective_id: int, agent_id: int) -> None:
        """Asigna un objetivo a un agente."""
        self.objective_to_agent[objective_id] = agent_id
    
    def release(self, objective_id: int) -> None:
        """Desasigna un objetivo."""
        if objective_id in self.objective_to_agent:
            del self.objective_to_agent[objective_id]
    
    def is_available(self, objective_id: int) -> bool:
        """Retorna True si el objetivo no está asignado."""
        return objective_id not in self.objective_to_agent
    
    def get_assigned_agent(self, objective_id: int) -> Optional[int]:
        """Retorna el agent_id asignado, o None si sin asignar."""
        return self.objective_to_agent.get(objective_id, None)


# ============================================================================
# CLASE: Best Response Coordinator
# ============================================================================

class BestResponseCoordinator:
    """
    Coordina agentes usando estrategia de "mejor respuesta" (Best Response).
    
    Basado en: Fictional Game - cada agente:
    1. Observa qué objetivos eligieron otros agentes (frecuencias)
    2. Calcula la mejor respuesta a esa estrategia observada
    3. Elige diferente para no duplicar esfuerzo
    
    CÓMO FUNCIONA:
    
    Paso 1: OBSERVAR
       - Cada turno, cada agente ve qué objetivo eligió cada otro agente
       - Acumula frecuencias en un historial
    
    Paso 2: PREDECIR
       - Estima P(otro_agente = objetivo_X) basado en historial
    
    Paso 3: ELEGIR MEJOR RESPUESTA
       - Si todos van por objetivo_X, ese es saturado (baja utilidad)
       - Elige objetivo_Y sin saturar (mejor utilidad esperada)
    
    RESULTADO:
       - Convergencia a equilibrio: cada objetivo tiene UN agente
       - No hay desperdicio de recursos (no duplican esfuerzo)
    
    ANALOGY con Rational Learning (Fictional Game):
       - Rational Learning: 2 robots, 2 zonas, observan y coordinan
       - Best Response Coord: N agentes, K víctimas, observan y coordinan
    """
    
    def __init__(self, num_agents: int):
        """
        Inicializa coordinador.
        
        Args:
            num_agents: cantidad de agentes (bomberos)
        """
        self.num_agents = num_agents
        self.history = CoordinationHistory()
        self.constraints = AssignmentConstraint()
    
    def observe_agent_choice(self, agent_id: int, objective_id: int) -> None:
        """
        Registra que un agente eligió un objetivo.
        
        Llamado después de cada decisión de un agente.
        
        Args:
            agent_id: ID del agente (0 a num_agents-1)
            objective_id: ID del objetivo (víctima, POI, etc.)
        """
        self.history.observe_choice(agent_id, objective_id)
    
    def calculate_best_response(
        self,
        agent_id: int,
        available_objectives: List[int],
        other_agents_choices: Dict[int, int]
    ) -> Tuple[int, float]:
        """
        Calcula la MEJOR RESPUESTA para un agente dado lo que otros eligieron.
        
        ALGORITMO (Best Response):
        
        1. Para cada objetivo disponible:
           a. Estimar P(otros_elijen_esto) = suma de probabilidades
           b. Calcular utilidad esperada:
              utility = (1.0 - P(saturado)) * max_utility
           c. Track el objetivo con máxima utilidad
        
        2. Si hay datos históricos:
           - Usa frecuencias del historial
           - Ej: si agente_0 fue a objetivo_1 el 80% de las veces,
             entonces evitar objetivo_1 cuando agente_0 libre
        
        3. Si no hay datos:
           - Asumir probabilidades iguales
           - Elegir aleatoriamente (en real, usar A* para preferencia)
        
        Args:
            agent_id: agente que decide
            available_objectives: lista de objetivos sin asignar
            other_agents_choices: {agent_id -> objective_id} de otros agentes
            
        Returns:
            tupla (best_objective_id, expected_utility)
        """
        
        if not available_objectives:
            return -1, 0.0
        
        # Contar cuántos agentes van por cada objetivo
        objective_saturation = Counter(other_agents_choices.values())
        
        best_objective = available_objectives[0]
        best_utility = -1.0
        
        for obj_id in available_objectives:
            
            # Calcular cuántos otros van por este objetivo
            other_agents_here = objective_saturation.get(obj_id, 0)
            
            # Estimar saturación: si N agentes en 1 objetivo = baja utilidad
            # Fórmula: utility ∝ 1 / (1 + saturation)
            saturation = 1.0 + other_agents_here
            utility = 1.0 / saturation
            
            # Si hay historial, ajustar por frecuencia histórica
            # (agentes tienden a repetir lo que eligieron antes)
            max_agent_id = max(other_agents_choices.keys()) if other_agents_choices else 0
            for other_id in range(max_agent_id + 1):
                if other_id == agent_id:
                    continue
                freq = self.history.get_frequency(other_id, obj_id)
                utility *= (1.0 - freq * 0.5)  # Penaliza si otro suele ir aquí
            
            if utility > best_utility:
                best_utility = utility
                best_objective = obj_id
        
        return best_objective, best_utility
    
    def coordinate_team(
        self,
        available_objectives: List[int],
        current_choices: Dict[int, int]
    ) -> Dict[int, int]:
        """
        Coordina TODO el equipo: cada agente calcula su mejor respuesta.
        
        ALGORITMO (Iterado):
        
        1. Para cada agente (en orden):
           a. Calcular mejor respuesta a lo que otros eligieron
           b. Asignar ese objetivo
           c. Actualizar observaciones
        
        2. Resultado: coordinación emergente
           - Sin comunicación explícita
           - Solo observando decisiones pasadas
        
        Nota: Simplified version - en real sería iterado hasta convergencia.
        
        Args:
            available_objectives: lista de víctimas/POIs disponibles
            current_choices: {agent_id -> objective_id} decisiones actuales
            
        Returns:
            dict con asignaciones coordinadas {agent_id -> best_objective_id}
        """
        
        coordinated = {}
        used_objectives: Set[int] = set()
        
        for agent_id in range(self.num_agents):
            
            # Objetivos sin usar todavía
            remaining = [o for o in available_objectives if o not in used_objectives]
            
            if not remaining:
                continue
            
            # Otros agentes (sin incluir este)
            other_choices = {
                aid: obj for aid, obj in current_choices.items()
                if aid != agent_id
            }
            
            # Calcular mejor respuesta
            best_obj, utility = self.calculate_best_response(
                agent_id, remaining, other_choices
            )
            
            if best_obj != -1:
                coordinated[agent_id] = best_obj
                used_objectives.add(best_obj)
        
        return coordinated
    
    def assign_objective(
        self,
        agent_id: int,
        objective_id: int
    ) -> bool:
        """
        Asigna un objetivo a un agente (si está disponible).
        
        Args:
            agent_id: agente
            objective_id: objetivo
            
        Returns:
            True si asignación exitosa, False si objetivo ya asignado
        """
        if not self.constraints.is_available(objective_id):
            return False
        
        self.constraints.assign(objective_id, agent_id)
        return True
    
    def release_objective(self, objective_id: int) -> None:
        """Desasigna un objetivo (ej: cuando se completa)."""
        self.constraints.release(objective_id)
    
    def get_assignments(self) -> Dict[int, int]:
        """Retorna asignaciones actuales."""
        return self.constraints.objective_to_agent.copy()


# ============================================================================
# FUNCIONES DE DEMOSTRACIÓN
# ============================================================================

def demo_coordination():
    """Demo de coordinación con 4 agentes y 4 víctimas."""
    
    num_agents = 4
    num_objectives = 4
    
    coordinator = BestResponseCoordinator(num_agents)
    
    # Simular 5 turnos de observación y coordinación
    print("=" * 70)
    print("BEST RESPONSE COORDINATION DEMO")
    print("=" * 70)
    print(f"Agents: {num_agents}, Objectives: {num_objectives}\n")
    
    available_objectives = list(range(num_objectives))
    
    for turn in range(1, 6):
        print(f"--- TURN {turn} ---")
        
        # Simular que cada agente hace una elección (aquí random para demo)
        import random
        current_choices = {
            agent_id: random.choice(available_objectives)
            for agent_id in range(num_agents)
        }
        
        print(f"Current choices: {current_choices}")
        
        # Calcular mejor respuesta para cada agente
        print("Best responses:")
        for agent_id in range(num_agents):
            other_choices = {
                aid: obj for aid, obj in current_choices.items()
                if aid != agent_id
            }
            
            best_obj, utility = coordinator.calculate_best_response(
                agent_id, available_objectives, other_choices
            )
            
            print(f"  Agent {agent_id}: best={best_obj}, utility={utility:.3f}")
            
            # Registrar observación
            coordinator.observe_agent_choice(agent_id, current_choices[agent_id])
        
        print()
    
    print("-" * 70)
    print(f"Final assignments: {coordinator.get_assignments()}")
    print("=" * 70)


if __name__ == "__main__":
    demo_coordination()
