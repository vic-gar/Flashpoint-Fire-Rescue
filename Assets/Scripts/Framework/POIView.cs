using UnityEngine;

public class POIView : MonoBehaviour
{
    private POIData data;
    private bool revealed = false;

    public void Initialize(POIData poiData)
    {
        data = poiData;
        revealed = false;
    }

    public void Reveal()
    {
        if (revealed)
        {
            return;
        }

        revealed = true;

        if (data.IsVictim())
        {
            Debug.Log(
                $"POI revelado en " +
                $"({data.row + 1},{data.column + 1}): VICTIMA"
            );
        }
        else
        {
            Debug.Log(
                $"POI revelado en " +
                $"({data.row + 1},{data.column + 1}): FALSA ALARMA"
            );
        }
    }
}