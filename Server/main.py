"""Punto de entrada para ver una partida completa en consola.

Corre una simulación de Flash Point con la estrategia indicada y
muestra turno por turno lo que hace cada bombero, cómo avanza el
fuego y cómo termina la partida. Sirve para depurar sin Unity; el
servidor (server.py) y los experimentos (run_batch.py) usan el mismo
modelo. Incluye la selección de estrategia y un resumen al final.

Uso (los argumentos van en cualquier orden):

    python main.py                    semilla 42, estrategia mejorada
    python main.py 7                  semilla 7
    python main.py 7 aleatoria        semilla 7 con la estrategia aleatoria
    python main.py 7 mejorada mudo    sin el detalle turno por turno
"""

import sys

from model.flashpoint_model import FlashPointModel
from strategies import STRATEGIES, get_strategy


def main():
    seed = 42
    verbose = True
    strategy_name = "mejorada"

    # Los argumentos se reconocen por su forma, no por su posición:
    # un número es la semilla, "mudo" apaga el detalle y cualquier otra
    # palabra tiene que ser el nombre de una estrategia.
    for arg in sys.argv[1:]:
        if arg.isdigit():
            seed = int(arg)
        elif arg == "mudo":
            verbose = False
        elif arg in STRATEGIES:
            strategy_name = arg
        else:
            print(f"Argumento no reconocido: {arg}")
            print(f"Estrategias: {', '.join(STRATEGIES)}")
            sys.exit(1)

    model = FlashPointModel(
        seed=seed,
        verbose=verbose,
        strategy=get_strategy(strategy_name)
    )

    print("=" * 55)
    print("FLASH POINT: FIRE RESCUE - SIMULACIÓN MULTIAGENTE")
    print("=" * 55)
    print(f"Estrategia: {model.strategy_name}")
    print(f"Semilla:    {seed}")
    print(f"Bomberos:   {len(model.firefighters)}")
    print(f"Tablero:    {model.board.ROWS}x{model.board.COLUMNS}")
    print(f"Fuego inicial: {model.board.count_fires()} celdas")
    print(f"POI en tablero: {len(model.board.pois)}")
    print(f"POI por salir:  {len(model.poi_deck)}")

    model.run_game()

    print()
    print("=" * 55)
    print("FIN DE LA PARTIDA")
    print("=" * 55)
    print(f"Resultado:            {model.result}")
    print(f"Turnos jugados:       {model.turns}")
    print(f"Víctimas rescatadas:  {model.victims_rescued}")
    print(f"Víctimas perdidas:    {model.victims_lost}")
    print(f"Víctimas reveladas:   {model.victims_revealed}")
    print(f"Falsas alarmas:       {model.false_alarms_revealed}")
    print(f"Daño estructural:     {model.board.damage_markers} de {model.MAX_DAMAGE}")
    print(f"Bomberos derribados:  {model.knock_downs}")
    print(f"Replaneaciones:       {model.replanifications}")
    print(f"Celdas con fuego:     {model.board.count_fires()}")
    print(f"Celdas con humo:      {len(model.board.smokes)}")

    print()
    print("Por bombero:")

    for firefighter in model.firefighters:
        print(
            f"  Bombero {firefighter.firefighter_id}: "
            f"{firefighter.cells_moved} celdas recorridas, "
            f"{firefighter.action_points_spent} AP gastados, "
            f"{firefighter.rescues} rescates, "
            f"{firefighter.fires_extinguished} fuegos apagados"
        )


if __name__ == "__main__":
    main()
