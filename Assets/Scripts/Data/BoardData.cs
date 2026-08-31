using System.Collections.Generic;

[System.Serializable]
public class BoardData
{
    public int rows;
    public int columns;

    public List<List<CellData>> cells;
    public List<DoorData> doors;
    public List<FireData> fires;
    public List<POIData> pois;
    public List<ExitData> exits;

    public BoardData(int rows, int columns)
    {
        this.rows = rows;
        this.columns = columns;

        cells = new List<List<CellData>>();
        doors = new List<DoorData>();
        fires = new List<FireData>();
        pois = new List<POIData>();
        exits = new List<ExitData>();
    }
}