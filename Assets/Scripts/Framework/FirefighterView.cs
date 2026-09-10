using UnityEngine;

/// <summary>
/// Componente del objeto de un bombero en la escena. Guarda sus datos y
/// los deja consultar desde fuera.
/// </summary>
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