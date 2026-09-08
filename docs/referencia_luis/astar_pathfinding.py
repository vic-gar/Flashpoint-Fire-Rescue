"""
╔════════════════════════════════════════════════════════════════╗
║                    A* PATHFINDING ALGORITHM                    ║
║                                                                ║
║ Basado en: Path Planning notebook (clase de sistemas multiagente)║
║                                                                ║
║ PROPÓSITO: Encontrar la ruta ÓPTIMA (menor costo) entre dos   ║
║           puntos en el grid 6x8 de Flashpoint, evitando       ║
║           obstáculos (paredes, fuego).                         ║
║                                                                ║
║ COMPLEJIDAD: O(n log n) donde n = número de celdas exploradas║
╚════════════════════════════════════════════════════════════════╝
"""

import heapq
from typing import List, Tuple, Optional, Set
from dataclasses import dataclass

# ============================================================================
# CONSTANTES
# ============================================================================

INFINITE_COST = 1_000_000  # Costo infinito para nodos no alcanzables
GRID_WIDTH = 8
GRID_HEIGHT = 6

# Tipos de celdas en el grid
CELL_WALKABLE = 0      # Celda normal, costo 1 AP
CELL_FIRE = 1          # Celda con fuego, no puede atravesar
CELL_WALL = 2          # Pared, no puede atravesar
CELL_SMOKE = 3         # Humo, cuesta más pero puede pasar


# ============================================================================
# CLASE: Priority Queue para A*
# ============================================================================

class PriorityQueue:
    """
    Cola de prioridades usando heapq (implementación de heap binaria).
    
    Nota: Basada en el código del notebook de Path Planning.
    Los elementos se ordenan por prioridad (menor número = mayor prioridad).
    """
    
    def __init__(self):
        self._data = []
    
    def empty(self) -> bool:
        """Retorna True si la cola está vacía."""
        return len(self._data) == 0
    
    def push(self, priority: float, value: Tuple[int, int]) -> None:
        """
        Inserta un elemento en la cola con una prioridad.
        
        Args:
            priority: número (menor = más prioritario)
            value: tupla (row, col) representando posición en grid
        """
        heapq.heappush(self._data, (priority, value))
    
    def pop(self) -> Tuple[float, Tuple[int, int]]:
        """
        Extrae y retorna el elemento con mayor prioridad (menor número).
        
        Returns:
            tupla (priority, value)
            
        Raises:
            Exception: si la cola está vacía
        """
        if self.empty():
            raise Exception("Priority queue is empty")
        return heapq.heappop(self._data)
    
    def clear(self) -> None:
        """Vacía la cola de prioridades."""
        self._data.clear()


# ============================================================================
# CLASE: Grid Helper
# ============================================================================

class GridHelper:
    """
    Funciones utilitarias para trabajar con el grid.
    
    Convierte entre:
    - Coordenadas (row, col)
    - Índices lineales
    - Validación de límites
    - Obtención de vecinos
    """
    
    @staticmethod
    def is_valid(grid: List[List[int]], position: Tuple[int, int]) -> bool:
        """
        Verifica si una posición está dentro de los límites del grid.
        
        Args:
            grid: matriz del juego
            position: (row, col)
            
        Returns:
            True si posición es válida
        """
        row, col = position
        height = len(grid)
        width = len(grid[0]) if height > 0 else 0
        
        return 0 <= row < height and 0 <= col < width
    
    @staticmethod
    def get_neighbors(grid: List[List[int]], position: Tuple[int, int]) -> List[Tuple[int, int]]:
        """
        Retorna las 4 celdas adyacentes (arriba, abajo, izq, der).
        
        Nota: NO incluye diagonales (como en Flashpoint).
        
        Args:
            grid: matriz del juego
            position: (row, col)
            
        Returns:
            lista de posiciones vecinas válidas
        """
        row, col = position
        neighbors = []
        
        # Arriba
        if GridHelper.is_valid(grid, (row - 1, col)):
            neighbors.append((row - 1, col))
        
        # Abajo
        if GridHelper.is_valid(grid, (row + 1, col)):
            neighbors.append((row + 1, col))
        
        # Izquierda
        if GridHelper.is_valid(grid, (row, col - 1)):
            neighbors.append((row, col - 1))
        
        # Derecha
        if GridHelper.is_valid(grid, (row, col + 1)):
            neighbors.append((row, col + 1))
        
        return neighbors
    
    @staticmethod
    def get_cost(grid: List[List[int]], position: Tuple[int, int]) -> int:
        """
        Retorna el costo de atravesar una celda.
        
        Args:
            grid: matriz del juego
            position: (row, col)
            
        Returns:
            - 1 si es celda normal
            - 2 si es humo (más caro)
            - INFINITE_COST si es obstáculo (no puede pasar)
        """
        row, col = position
        
        if not GridHelper.is_valid(grid, (row, col)):
            return INFINITE_COST
        
        cell_type = grid[row][col]
        
        if cell_type == CELL_WALKABLE:
            return 1
        elif cell_type == CELL_SMOKE:
            return 2
        else:  # FIRE, WALL
            return INFINITE_COST
    
    @staticmethod
    def manhattan_distance(pos1: Tuple[int, int], pos2: Tuple[int, int]) -> int:
        """
        Heurística: distancia Manhattan entre dos posiciones.
        
        Usada en A* para estimar distancia al objetivo.
        Admisible porque NUNCA sobreestima la distancia real.
        
        Args:
            pos1: (row1, col1)
            pos2: (row2, col2)
            
        Returns:
            |row1 - row2| + |col1 - col2|
        """
        row1, col1 = pos1
        row2, col2 = pos2
        return abs(row1 - row2) + abs(col1 - col2)


# ============================================================================
# CLASE: A* Algorithm
# ============================================================================

class AStarPathfinder:
    """
    Algoritmo A* para encontrar el camino más corto en un grid.
    
    Basado en: Path Planning notebook (clase)
    
    CÓMO FUNCIONA:
    1. Mantiene distancia_real_mínima a cada nodo (desde start)
    2. Usa Priority Queue ordenada por: (distancia_real + heurística_Manhattan)
    3. Explora nodos en orden de menor "costo_estimado_total"
    4. Cuando llega al destino, reconstruye el camino
    
    VENTAJAS vs Dijkstra:
    - Explora MENOS nodos (heurística guía búsqueda)
    - Sigue siendo ÓPTIMO (si heurística es admisible)
    - Manhattan es admisible porque nunca sobreestima
    
    COMPLEJIDAD:
    - Tiempo: O(n log n) donde n = nodos explorados
    - Espacio: O(n) para arrays y cola
    """
    
    def __init__(self):
        self.nodes_explored = 0  # Para métricas
    
    def find_path(
        self,
        grid: List[List[int]],
        start: Tuple[int, int],
        goal: Tuple[int, int]
    ) -> Tuple[Optional[List[Tuple[int, int]]], int, int]:
        """
        Encuentra la ruta óptima usando A*.
        
        Args:
            grid: matriz donde grid[row][col] = tipo de celda
            start: posición inicial (row, col)
            goal: posición destino (row, col)
            
        Returns:
            tupla (ruta, costo_total, nodos_explorados)
            - ruta: lista de posiciones o None si no hay camino
            - costo_total: suma de costos de cada paso
            - nodos_explorados: cuántos nodos visitó (para métricas)
        """
        
        # Validaciones iniciales
        if not GridHelper.is_valid(grid, start):
            return None, INFINITE_COST, 0
        if not GridHelper.is_valid(grid, goal):
            return None, INFINITE_COST, 0
        
        self.nodes_explored = 0
        height = len(grid)
        width = len(grid[0]) if height > 0 else 0
        total_cells = height * width
        
        # Arrays principales
        distance = [INFINITE_COST] * total_cells  # Distancia real mínima
        previous = [None] * total_cells            # Para reconstruir ruta
        visited: Set[Tuple[int, int]] = set()     # Nodos ya explorados
        
        # Priority Queue
        pq = PriorityQueue()
        
        # Inicialización
        start_idx = self._to_index(grid, start)
        goal_idx = self._to_index(grid, goal)
        
        distance[start_idx] = 0
        h_start = GridHelper.manhattan_distance(start, goal)
        pq.push(h_start, start)
        
        # Búsqueda A*
        while not pq.empty():
            current_f, current_pos = pq.pop()  # f = g + h
            
            # Si ya fue visitado, saltar (lazy deletion)
            if current_pos in visited:
                continue
            
            visited.add(current_pos)
            self.nodes_explored += 1
            
            # ¡Llegamos al destino!
            if current_pos == goal:
                ruta = self._reconstruct_path(grid, previous, start, goal)
                current_idx = self._to_index(grid, current_pos)
                return ruta, distance[current_idx], self.nodes_explored
            
            # Explorar vecinos
            for neighbor_pos in GridHelper.get_neighbors(grid, current_pos):
                
                # Si ya fue visitado, saltar
                if neighbor_pos in visited:
                    continue
                
                # Calcular nuevo costo
                step_cost = GridHelper.get_cost(grid, neighbor_pos)
                
                if step_cost == INFINITE_COST:
                    continue  # No puede pasar por aquí
                
                neighbor_idx = self._to_index(grid, neighbor_pos)
                current_idx = self._to_index(grid, current_pos)
                new_distance = distance[current_idx] + step_cost
                
                # ¿Es mejor camino?
                if new_distance < distance[neighbor_idx]:
                    distance[neighbor_idx] = new_distance
                    previous[neighbor_idx] = current_pos
                    
                    # Calcular f = g + h
                    g = new_distance
                    h = GridHelper.manhattan_distance(neighbor_pos, goal)
                    f = g + h
                    
                    pq.push(f, neighbor_pos)
        
        # No hay camino
        return None, INFINITE_COST, self.nodes_explored
    
    @staticmethod
    def _to_index(grid: List[List[int]], position: Tuple[int, int]) -> int:
        """Convierte (row, col) a índice lineal."""
        row, col = position
        width = len(grid[0]) if len(grid) > 0 else 0
        return row * width + col
    
    @staticmethod
    def _to_position(grid: List[List[int]], index: int) -> Tuple[int, int]:
        """Convierte índice lineal a (row, col)."""
        width = len(grid[0]) if len(grid) > 0 else 0
        return (index // width, index % width)
    
    @staticmethod
    def _reconstruct_path(
        grid: List[List[int]],
        previous: List[Optional[Tuple[int, int]]],
        start: Tuple[int, int],
        goal: Tuple[int, int]
    ) -> List[Tuple[int, int]]:
        """Reconstruye el camino desde start a goal usando los punteros."""
        ruta = []
        current = goal
        
        while current is not None:
            ruta.insert(0, current)
            goal_idx = AStarPathfinder._to_index(grid, current)
            current = previous[goal_idx]
        
        return ruta if ruta and ruta[0] == start else [start, goal]


# ============================================================================
# FUNCIONES DE DEMOSTRACIÓN
# ============================================================================

def demo_astar():
    """Demo del A* con un grid simple."""
    
    # Grid simple: 6x8
    # 0 = celda normal
    # 1 = fuego (no puede pasar)
    # 2 = pared (no puede pasar)
    grid = [
        [0, 0, 0, 0, 2, 0, 0, 0],
        [0, 1, 1, 0, 2, 0, 0, 0],
        [0, 0, 0, 0, 0, 0, 1, 0],
        [2, 0, 1, 1, 0, 0, 1, 0],
        [0, 0, 0, 0, 0, 0, 0, 0],
        [0, 1, 0, 0, 0, 0, 0, 0],
    ]
    
    start = (0, 0)
    goal = (5, 7)
    
    pathfinder = AStarPathfinder()
    ruta, costo, explorados = pathfinder.find_path(grid, start, goal)
    
    print("=" * 60)
    print("A* PATHFINDING DEMO")
    print("=" * 60)
    print(f"Start: {start}")
    print(f"Goal:  {goal}")
    print(f"\nRuta encontrada:")
    if ruta:
        print(f"  {' → '.join(str(p) for p in ruta)}")
        print(f"\nCosto total: {costo} AP")
        print(f"Nodos explorados: {explorados}")
        print(f"Largo de ruta: {len(ruta)} pasos")
    else:
        print("  NO HAY CAMINO")
    print("=" * 60)


if __name__ == "__main__":
    demo_astar()
