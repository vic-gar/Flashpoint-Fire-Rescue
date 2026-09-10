"""Estrategia aleatoria: la línea base del proyecto.

Es la línea base del proyecto: el control contra el que se compara la
estrategia mejorada.

Elige al azar entre las acciones legales del bombero, así que nunca
hace una jugada inválida, solo una poco inteligente.

Tiene la firma decide(model, firefighter), la misma que la estrategia
mejorada, para poder intercambiarlas sin tocar el modelo.

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
