using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public GameObject cellPrefab;
    public GameObject wallPrefab;
    public GameObject doorPrefab;
    public GameObject firePrefab;
    public GameObject poiPrefab;
    public GameObject exitPrefab;
    public GameObject firefighterPrefab;

    public Transform firefightersParent;

    public int rows = 6;
    public int columns = 8;

    public float cellSize = 1.0f;

    private BoardData boardData;

    void Start()
    {
        boardData = BoardFileReader.LoadBoard("final");

        if (boardData == null)
        {
            return;
        }

        GenerateBoard();
        GenerateDoors();
        GenerateFires();
        GeneratePOIs();
        GenerateExits();
        GenerateFirefighters();
    }

    void GenerateBoard()
    {
        for (int row = 0; row < boardData.rows; row++)
        {
            for (int column = 0; column < boardData.columns; column++)
            {
                Vector3 position = new Vector3(
                    column * cellSize,
                    0,
                    -row * cellSize
                );

                GameObject cellObject = Instantiate(
                    cellPrefab,
                    position,
                    Quaternion.identity,
                    transform
                );

                CellView cellView = cellObject.GetComponent<CellView>();

                if (cellView != null)
                {
                    cellView.Initialize(row, column);
                }

                CellData data = boardData.cells[row][column];

                CreateWalls(data, position);

                Debug.Log(
                    $"Cell {row + 1},{column + 1} | " +
                    $"U:{data.wallUp} " +
                    $"L:{data.wallLeft} " +
                    $"D:{data.wallDown} " +
                    $"R:{data.wallRight}"
                );
            }
        }
    }

    void GenerateFires()
    {
        foreach (FireData fire in boardData.fires)
        {
            Vector3 position = new Vector3(
                fire.column * cellSize,
                0.25f,
                -fire.row * cellSize
            );

            GameObject fireObject = Instantiate(
                firePrefab,
                position,
                Quaternion.identity,
                transform
            );

            fireObject.name =
                $"Fire_{fire.row + 1}_{fire.column + 1}";
        }
    }

    void GenerateDoors()
    {
        foreach (DoorData door in boardData.doors)
        {
            Vector3 cell1Position = new Vector3(
                door.column1 * cellSize,
                0,
                -door.row1 * cellSize
            );

            Vector3 cell2Position = new Vector3(
                door.column2 * cellSize,
                0,
                -door.row2 * cellSize
            );

            Vector3 doorPosition =
                (cell1Position + cell2Position) / 2f;

            Quaternion rotation = Quaternion.identity;

            // Si cambia la columna, la puerta divide izquierda/derecha
            if (door.column1 != door.column2)
            {
                rotation = Quaternion.Euler(0, 90, 0);
            }

            GameObject doorObject = Instantiate(
                doorPrefab,
                doorPosition + new Vector3(0, 0.5f, 0),
                rotation,
                transform
            );

            doorObject.name =
                $"Door_{door.row1 + 1}_{door.column1 + 1}_" +
                $"{door.row2 + 1}_{door.column2 + 1}";
        }
    }

    void GenerateExits()
    {
        foreach (ExitData exit in boardData.exits)
        {
            Vector3 position = new Vector3(
                exit.column * cellSize,
                0.08f,
                -exit.row * cellSize
            );

            GameObject exitObject = Instantiate(
                exitPrefab,
                position,
                Quaternion.identity,
                transform
            );

            exitObject.name =
                $"Exit_{exit.row + 1}_{exit.column + 1}";
        }
    }

    void GeneratePOIs()
    {
        foreach (POIData poi in boardData.pois)
        {
            Vector3 position = new Vector3(
                poi.column * cellSize,
                0.15f,
                -poi.row * cellSize
            );

            GameObject poiObject = Instantiate(
                poiPrefab,
                position,
                Quaternion.identity,
                transform
            );

            poiObject.name =
                $"POI_{poi.row + 1}_{poi.column + 1}";

            POIView poiView =
                poiObject.GetComponent<POIView>();

            if (poiView != null)
            {
                poiView.Initialize(poi);
            }
        }
    }

    void CreateWalls(CellData data, Vector3 cellPosition)
    {
        float half = cellSize / 2f;

        // =========================
        // Pared superior
        // =========================

        bool doorUp = false;

        if (data.row > 0)
        {
            doorUp = HasDoorBetween(
                data.row,
                data.column,
                data.row - 1,
                data.column
            );
        }

        if (data.wallUp && !doorUp)
        {
            Vector3 position =
                cellPosition +
                new Vector3(0, 0.5f, half);

            GameObject wall = Instantiate(
                wallPrefab,
                position,
                Quaternion.identity,
                transform
            );

            wall.name =
                $"Wall_Up_{data.row + 1}_{data.column + 1}";
        }

        // =========================
        // Pared izquierda
        // =========================

        bool doorLeft = false;

        if (data.column > 0)
        {
            doorLeft = HasDoorBetween(
                data.row,
                data.column,
                data.row,
                data.column - 1
            );
        }

        if (data.wallLeft && !doorLeft)
        {
            Vector3 position =
                cellPosition +
                new Vector3(-half, 0.5f, 0);

            GameObject wall = Instantiate(
                wallPrefab,
                position,
                Quaternion.Euler(0, 90, 0),
                transform
            );

            wall.name =
                $"Wall_Left_{data.row + 1}_{data.column + 1}";
        }

        // =========================
        // Borde inferior
        // =========================

        if (data.row == rows - 1 && data.wallDown)
        {
            Vector3 position =
                cellPosition +
                new Vector3(0, 0.5f, -half);

            GameObject wall = Instantiate(
                wallPrefab,
                position,
                Quaternion.identity,
                transform
            );

            wall.name =
                $"Wall_Down_{data.row + 1}_{data.column + 1}";
        }

        // =========================
        // Borde derecho
        // =========================

        if (data.column == columns - 1 && data.wallRight)
        {
            Vector3 position =
                cellPosition +
                new Vector3(half, 0.5f, 0);

            GameObject wall = Instantiate(
                wallPrefab,
                position,
                Quaternion.Euler(0, 90, 0),
                transform
            );

            wall.name =
                $"Wall_Right_{data.row + 1}_{data.column + 1}";
        }
    }

    bool HasDoorBetween(
        int row1,
        int column1,
        int row2,
        int column2
    )
    {
        foreach (DoorData door in boardData.doors)
        {
            bool sameDirection =
                door.row1 == row1 &&
                door.column1 == column1 &&
                door.row2 == row2 &&
                door.column2 == column2;

            bool oppositeDirection =
                door.row1 == row2 &&
                door.column1 == column2 &&
                door.row2 == row1 &&
                door.column2 == column1;

            if (sameDirection || oppositeDirection)
            {
                return true;
            }
        }

        return false;
    }

    void GenerateFirefighters()
    {
        foreach (FirefighterData firefighter in boardData.firefighters)
        {
            Vector3 position = new Vector3(
                firefighter.column * cellSize,
                0.4f,
                -firefighter.row * cellSize
            );

            GameObject firefighterObject = Instantiate(
                firefighterPrefab,
                position,
                Quaternion.identity,
                firefightersParent
            );

            FirefighterView view =
                firefighterObject.GetComponent<FirefighterView>();

            if (view != null)
            {
                view.Initialize(firefighter);
            }
        }
    }
}