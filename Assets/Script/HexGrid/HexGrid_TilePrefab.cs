using UnityEngine;
using UnityEngine.UI;

public class HexGrid_TilePrefab : MonoBehaviour
{
    public int tileIndexRow;
    public int tileIndexCol;
    public GameObject tileHighlightGameObject;

    public void SetHighlight(bool highlight)
    {
        if (tileHighlightGameObject != null)
        {
            tileHighlightGameObject.SetActive(highlight);
        }
        else
        {
            Debug.LogWarning($"Tile at row {tileIndexRow}, col {tileIndexCol} does not have a tileHighlightGameObject assigned.");
        }
    }
}
