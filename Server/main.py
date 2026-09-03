from model.flashpoint_model import FlashPointModel


def main():
    model = FlashPointModel(rng=42)

    print("===================================")
    print("FLASH POINT - MODELO MESA")
    print("===================================")

    print("\nBomberos creados:")

    for firefighter in model.firefighters:
        print(firefighter)

    model.start_turn()

    firefighter = model.current_firefighter
    firefighter.random_turn()

    print(
        f"\nPosición final Bombero "
        f"{firefighter.firefighter_id}: "
        f"({firefighter.row + 1}, "
        f"{firefighter.column + 1})"
    )

    print(
        f"AP restantes: "
        f"{firefighter.action_points}"
    )


if __name__ == "__main__":
    main()