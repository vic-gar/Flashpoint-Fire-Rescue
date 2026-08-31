using UnityEngine;

public class CellView : MonoBehaviour
{
    public int row;
    public int column;

    public void Initialize(int newRow, int newColumn)
    {
        row = newRow;
        column = newColumn;

        gameObject.name = $"Cell_{row + 1}_{column + 1}";
    }
}