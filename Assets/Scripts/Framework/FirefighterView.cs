using UnityEngine;

public class FirefighterView : MonoBehaviour
{
    private FirefighterData data;

    public int Id => data.id;
    public int Row => data.row;
    public int Column => data.column;
    public int ActionPoints => data.actionPoints;

    public void Initialize(FirefighterData firefighterData)
    {
        data = firefighterData;

        gameObject.name =
            $"Firefighter_{data.id}";
    }
}