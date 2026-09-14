using UnityEngine;

/// <summary>
/// Componente del marcador de POI en la escena. Al revelarse avisa por
/// consola si era un civil o una falsa alarma.
/// </summary>
public class POIView : MonoBehaviour
{
    private POIData data;
    private bool revealed = false;

    public void Initialize(POIData poiData)
    {
        data = poiData;
        revealed = false;
    }

    /// <summary>
    /// Se llama la primera vez que un bombero comprueba el marcador.
    /// Las siguientes veces no hace nada.
    /// </summary>
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