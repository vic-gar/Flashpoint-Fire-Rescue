[System.Serializable]
public class CellData
{
    public int row;
    public int column;

    public bool wallUp;
    public bool wallLeft;
    public bool wallDown;
    public bool wallRight;

    public CellData(int row, int column)
    {
        this.row = row;
        this.column = column;
    }

    public void SetWalls(string wallCode)
    {
        wallUp = wallCode[0] == '1';
        wallLeft = wallCode[1] == '1';
        wallDown = wallCode[2] == '1';
        wallRight = wallCode[3] == '1';
    }
}