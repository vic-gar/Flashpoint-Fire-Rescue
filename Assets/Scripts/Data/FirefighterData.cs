[System.Serializable]
public class FirefighterData
{
    public int id;

    public int row;
    public int column;

    public int actionPoints;

    public bool carryingVictim;

    public FirefighterData(
        int id,
        int row,
        int column
    )
    {
        this.id = id;

        this.row = row;
        this.column = column;

        actionPoints = 4;
        carryingVictim = false;
    }
}