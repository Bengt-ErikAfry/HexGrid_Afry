using UnityEngine;
using UnityEngine.UI;

public class HexGrid_TilePrefab : MonoBehaviour
{
    public int tileIndexRow;
    public int tileIndexCol;
    public Image tileHighlightImage;

    public void SetHighlight(bool highlight)
    {
        if (tileHighlightImage != null)
        {
            tileHighlightImage.enabled = highlight;
        }
    }
}
