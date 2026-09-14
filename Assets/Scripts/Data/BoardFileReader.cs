using System.Collections.Generic;
using UnityEngine;

public static class BoardFileReader
{
    public static BoardData LoadBoard(string fileName)
    {
        TextAsset file = Resources.Load<TextAsset>(fileName);

        if (file == null)
        {
            Debug.LogError("No se pudo encontrar el archivo: " + fileName);
            return null;
        }

        string[] lines = file.text.Split(
            new[] { '\r', '\n' },
            System.StringSplitOptions.RemoveEmptyEntries
        );

        int rows = 6;
        int columns = 8;

        BoardData boardData = new BoardData(rows, columns);

        // =========================
        // Leer paredes
        // =========================

        for (int row = 0; row < rows; row++)
        {
            string[] values = lines[row].Split(' ');

            List<CellData> currentRow = new List<CellData>();

            for (int column = 0; column < columns; column++)
            {
                CellData cell = new CellData(row, column);

                cell.SetWalls(values[column]);

                currentRow.Add(cell);
            }

            boardData.cells.Add(currentRow);
        }

        // =========================
        // Leer POI iniciales
        // =========================

        int poiStartLine = 6;
        int poiCount = 3;

        for (int i = 0; i < poiCount; i++)
        {
            string[] values =
                lines[poiStartLine + i].Split(' ');

            int row = int.Parse(values[0]) - 1;
            int column = int.Parse(values[1]) - 1;

            char type = values[2][0];

            boardData.pois.Add(
                new POIData(row, column, type)
            );
        }

        // =========================
        // Leer fuego inicial
        // =========================

        int fireStartLine = 9;
        int fireCount = 10;

        for (int i = 0; i < fireCount; i++)
        {
            string[] values =
                lines[fireStartLine + i].Split(' ');

            int row = int.Parse(values[0]) - 1;
            int column = int.Parse(values[1]) - 1;

            boardData.fires.Add(
                new FireData(row, column)
            );
        }

        // =========================
        // Leer puertas
        // =========================

        int doorStartLine = 19;
        int doorCount = 8;

        for (int i = 0; i < doorCount; i++)
        {
            string[] values = lines[doorStartLine + i].Split(' ');

            int row1 = int.Parse(values[0]) - 1;
            int column1 = int.Parse(values[1]) - 1;

            int row2 = int.Parse(values[2]) - 1;
            int column2 = int.Parse(values[3]) - 1;

            DoorData door = new DoorData(
                row1,
                column1,
                row2,
                column2
            );

            boardData.doors.Add(door);
        }

        // =========================
        // Leer entradas/salidas
        // =========================

        int exitStartLine = 27;
        int exitCount = 4;

        for (int i = 0; i < exitCount; i++)
        {
            string[] values =
                lines[exitStartLine + i].Split(' ');

            int row = int.Parse(values[0]) - 1;
            int column = int.Parse(values[1]) - 1;

            boardData.exits.Add(
                new ExitData(row, column)
            );
        }

        // =========================
        // Crear bomberos iniciales
        // =========================

        if (boardData.exits.Count >= 4)
        {
            ExitData exit1 = boardData.exits[0];
            ExitData exit2 = boardData.exits[1];
            ExitData exit3 = boardData.exits[2];
            ExitData exit4 = boardData.exits[3];

            boardData.firefighters.Add(
                new FirefighterData(
                    1,
                    exit1.row,
                    exit1.column
                )
            );

            boardData.firefighters.Add(
                new FirefighterData(
                    2,
                    exit1.row,
                    exit1.column
                )
            );

            boardData.firefighters.Add(
                new FirefighterData(
                    3,
                    exit2.row,
                    exit2.column
                )
            );

            boardData.firefighters.Add(
                new FirefighterData(
                    4,
                    exit2.row,
                    exit2.column
                )
            );

            boardData.firefighters.Add(
                new FirefighterData(
                    5,
                    exit3.row,
                    exit3.column
                )
            );

            boardData.firefighters.Add(
                new FirefighterData(
                    6,
                    exit4.row,
                    exit4.column
                )
            );
        }

        return boardData;
    }
}