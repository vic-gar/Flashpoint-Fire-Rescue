[System.Serializable]
public class DoorData
{
    public int row1;
    public int column1;
    public int row2;
    public int column2;

    public DoorData(int row1, int column1, int row2, int column2)
    {
        this.row1 = row1;
        this.column1 = column1;
        this.row2 = row2;
        this.column2 = column2;
    }
}