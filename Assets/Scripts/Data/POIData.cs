[System.Serializable]
public class POIData
{
    public int row;
    public int column;
    public char type;

    public POIData(int row, int column, char type)
    {
        this.row = row;
        this.column = column;
        this.type = type;
    }

    public bool IsVictim()
    {
        return type == 'v';
    }

    public bool IsFalseAlarm()
    {
        return type == 'f';
    }
}