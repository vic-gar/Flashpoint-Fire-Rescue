/// <summary>
/// Una celda que empieza la partida con fuego.
/// </summary>
[System.Serializable]
public class FireData
{
    public int row;
    public int column;

    public FireData(int row, int column)
    {
        this.row = row;
        this.column = column;
    }
}