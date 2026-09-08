"""Estrategias de decisión de los bomberos.

Toda estrategia es una función decide(model, firefighter) que devuelve
UNA acción legal del catálogo de FirefighterAgent.get_legal_actions()
o None para terminar el turno guardando los AP que queden.

    aleatoria                  línea base del criterio 1 de la rúbrica
    mejorada                   A* + priorización + coordinación (criterio 2)
    mejorada_sin_coordinacion  igual pero sin repartir objetivos, para
                               medir por separado cuánto aporta coordinar

Este registro es el único lugar donde se conocen los nombres: main.py,
server.py, run_batch.py y el inspector de Unity (campo Strategy) los
usan tal cual. Para agregar una estrategia basta con escribir la
función y darla de alta en STRATEGIES.

Se importan los módulos, no las funciones, para que
strategies.random_strategy siga siendo el módulo y no se confunda con
la función del mismo nombre que vive dentro.
"""

from strategies import random_strategy as _random
from strategies import improved_strategy as _improved


STRATEGIES = {
    "aleatoria": _random.random_strategy,
    "mejorada": _improved.improved_strategy,
    "mejorada_sin_coordinacion": _improved.improved_strategy_no_coordination,
}


def get_strategy(name):
    """Busca una estrategia por nombre. Lanza ValueError si no existe."""
    if name not in STRATEGIES:
        raise ValueError(
            f"Estrategia desconocida: {name}. "
            f"Opciones: {', '.join(STRATEGIES)}"
        )

    return STRATEGIES[name]
