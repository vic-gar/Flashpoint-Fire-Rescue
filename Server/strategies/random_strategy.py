"""Estrategia aleatoria: la línea base del proyecto.

NO BORRAR: es la "solución aleatoria" del criterio 1 de la rúbrica y el
control contra el que se compara la estrategia mejorada (criterio 2).

Elige al azar entre las acciones legales del bombero, así que nunca
hace una jugada inválida, solo una poco inteligente.

Origen: la idea viene de execute_random_action del agente original de
Víctor (elegir con random.choice entre las acciones válidas). Se movió a
una función con la firma decide(model, firefighter) para poder
intercambiarla por la mejorada sin tocar el modelo.

RNG: usa model.strategy_random, no el generador del fuego. Así las
decisiones al azar no corren la secuencia de dados del entorno y la
comparación con semillas pareadas es justa (ver
FlashPointModel._create_random_generators).
"""


def random_strategy(model, firefighter):
    actions = firefighter.get_legal_actions()

    if not actions:
        return None

    # RNG de estrategia: solo decisiones, nunca eventos del juego.
    return model.strategy_random.choice(actions)


random_strategy.strategy_name = "aleatoria"
