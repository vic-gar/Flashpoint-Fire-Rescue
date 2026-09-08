"""Corre partidas en lote y guarda las métricas reales en CSV.

Es la base de la evidencia de los criterios 2 (estrategia mejorada) y
3 (rendimiento) de la rúbrica. Todo lo que imprime sale de partidas
completas del modelo Mesa; no hay ningún número estimado.
Desarrollado con apoyo de Claude; los números son del modelo.

Comparación con semillas pareadas: la partida i de cada estrategia
arranca con la misma semilla, así que las tres parten del mismo tablero,
del mismo mazo de POI y de la misma secuencia de dados del fuego.
Cualquier diferencia se debe a la estrategia y no a la suerte del
sorteo. Esto es cierto porque el modelo usa generadores separados para
el fuego, los POI y las decisiones al azar (ver
FlashPointModel._create_random_generators y tests/test_rng.py).

Métricas por partida (una fila por semilla en resultados_<estrategia>.csv):
    victoria, víctimas rescatadas/perdidas/reveladas, falsas alarmas,
    daño estructural, turnos, derribos, replanificaciones, celdas
    recorridas, AP gastados y desperdiciados (arriba del tope de 4
    guardados), fuegos y humos apagados, paredes cortadas, fuegos al
    final y tiempo en milisegundos.

Uso (desde la carpeta Server):

    python run_batch.py                  30 partidas aleatorias
    python run_batch.py 50 mejorada      50 partidas con la mejorada
    python run_batch.py 100 comparar     las 3 estrategias, semillas 0-99

Con "comparar" se generan:
    resultados_aleatoria.csv
    resultados_mejorada.csv
    resultados_mejorada_sin_coordinacion.csv
    resultados_comparacion.csv     (una fila por estrategia, promedios)
"""

import csv
import sys
import time

from model.flashpoint_model import FlashPointModel
from strategies import STRATEGIES, get_strategy


COMPARISON_ORDER = [
    "aleatoria",
    "mejorada_sin_coordinacion",
    "mejorada",
]


def run_batch(runs=30, strategy=None, seed_start=0, board_file="data/final.txt"):
    """Corre 'runs' partidas y devuelve una fila de métricas por partida.

    strategy puede ser None (aleatoria), un nombre del registro o la
    función misma.
    """
    if isinstance(strategy, str):
        strategy = get_strategy(strategy)

    results = []

    for i in range(runs):
        seed = seed_start + i

        start = time.perf_counter()

        model = FlashPointModel(
            board_file=board_file,
            strategy=strategy,
            seed=seed
        )

        model.run_game()

        elapsed_ms = (time.perf_counter() - start) * 1000

        firefighters = model.firefighters

        results.append({
            "estrategia": model.strategy_name,
            "semilla": seed,
            "resultado": model.result,
            "victoria": int(model.result == "victoria"),
            "victimas_rescatadas": model.victims_rescued,
            "victimas_perdidas": model.victims_lost,
            "victimas_reveladas": model.victims_revealed,
            "falsas_alarmas": model.false_alarms_revealed,
            "danio_estructural": model.board.damage_markers,
            "turnos": model.turns,
            "derribos": model.knock_downs,
            "replanificaciones": model.replanifications,
            "celdas_recorridas": sum(f.cells_moved for f in firefighters),
            "ap_gastados": sum(f.action_points_spent for f in firefighters),
            "ap_desperdiciados": sum(f.action_points_wasted for f in firefighters),
            "fuegos_apagados": sum(f.fires_extinguished for f in firefighters),
            "humos_apagados": sum(f.smokes_extinguished for f in firefighters),
            "paredes_cortadas": sum(f.walls_chopped for f in firefighters),
            "fuegos_al_final": model.board.count_fires(),
            "tiempo_ms": round(elapsed_ms, 2),
        })

    return results


def save_csv(results, path):
    if not results:
        return

    with open(path, "w", newline="", encoding="utf-8") as file:
        writer = csv.DictWriter(file, fieldnames=list(results[0].keys()))
        writer.writeheader()
        writer.writerows(results)


# Métricas que se promedian en el resumen, en el orden de la tabla.
SUMMARY_FIELDS = [
    "victoria",
    "victimas_rescatadas",
    "victimas_perdidas",
    "danio_estructural",
    "turnos",
    "derribos",
    "replanificaciones",
    "ap_gastados",
    "ap_desperdiciados",
    "celdas_recorridas",
    "fuegos_apagados",
    "humos_apagados",
    "paredes_cortadas",
    "tiempo_ms",
]


def summarize(results):
    """Promedios simples de la corrida, sin interpretar nada."""
    total = len(results)

    if total == 0:
        return {}

    summary = {
        "estrategia": results[0]["estrategia"],
        "partidas": total,
    }

    for field in SUMMARY_FIELDS:
        summary[field] = sum(row[field] for row in results) / total

    # Desglose de cómo terminaron las partidas.
    for outcome in ["victoria", "derrota_victimas", "derrota_colapso"]:
        summary["n_" + outcome] = sum(
            1 for row in results if row["resultado"] == outcome
        )

    return summary


def print_summary(summary):
    print(f"\n{summary['estrategia']} ({summary['partidas']} partidas)")

    for field in SUMMARY_FIELDS:
        value = summary[field]

        if field == "victoria":
            print(f"  tasa de victoria         {100 * value:6.1f} %")
        else:
            print(f"  {field:24s} {value:8.2f}")

    print(
        f"  desglose: {summary['n_victoria']} victorias, "
        f"{summary['n_derrota_victimas']} derrotas por víctimas, "
        f"{summary['n_derrota_colapso']} colapsos"
    )


def compare(runs=100, seed_start=0):
    """Corre las tres estrategias con las mismas semillas y las compara."""
    summaries = []

    for name in COMPARISON_ORDER:
        print(f"Corriendo {runs} partidas con '{name}'...")

        results = run_batch(runs=runs, strategy=name, seed_start=seed_start)

        save_csv(results, f"resultados_{name}.csv")

        summaries.append(summarize(results))

    with open("resultados_comparacion.csv", "w", newline="", encoding="utf-8") as file:
        writer = csv.DictWriter(file, fieldnames=list(summaries[0].keys()))
        writer.writeheader()
        writer.writerows(summaries)

    print("\n" + "=" * 60)
    print(f"COMPARACIÓN CON SEMILLAS PAREADAS ({seed_start} a {seed_start + runs - 1})")
    print("=" * 60)

    for summary in summaries:
        print_summary(summary)

    print("\nArchivos: resultados_<estrategia>.csv y resultados_comparacion.csv")

    return summaries


def main():
    runs = 30
    mode = "aleatoria"

    for arg in sys.argv[1:]:
        if arg.isdigit():
            runs = int(arg)
        else:
            mode = arg

    if mode == "comparar":
        compare(runs=runs)
        return

    if mode not in STRATEGIES:
        print(f"Estrategia desconocida: {mode}")
        print(f"Opciones: {', '.join(STRATEGIES)}, comparar")
        sys.exit(1)

    print(f"Corriendo {runs} partidas con la estrategia '{mode}'...")

    results = run_batch(runs=runs, strategy=mode)

    output = f"resultados_{mode}.csv"
    save_csv(results, output)

    print_summary(summarize(results))
    print(f"\nCSV guardado en: {output}")


if __name__ == "__main__":
    main()
