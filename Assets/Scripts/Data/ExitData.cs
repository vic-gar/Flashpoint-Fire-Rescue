/// <summary>
/// Una salida del edificio. Es a donde los bomberos llevan a los civiles.
/// </summary>
[System.Serializable]
public class ExitData
{
    public int row;
    public int column;

    public ExitData(int row, int column)
    {
        this.row = row;
        this.column = column;
    }
}