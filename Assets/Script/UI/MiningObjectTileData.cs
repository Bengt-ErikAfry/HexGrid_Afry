using System;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class MiningObjectTileData : MonoBehaviour 
{
    [SerializeField]
    public TileData tileData;
    public GameObject fogGO;

    // USE STATE INSTED
    /*
    public void TileClicked()
    {                 
        // Handle tile click logic here
        Debug.Log($"Tile at Row: {tileData.tileIndexRow}, Col: {tileData.tileIndexCol} clicked.");
        
        MiningUIManager.Instance.HandleTileClick(tileData);
        /*
        if (tileData.isPlayerTurnOver)
        {
            Debug.Log("Player turn is over. Cannot click on the tile.");
            return;
        }
        if (!tileData.isSurveyed)
        {
            Debug.Log("Tile is not surveyed. Cannot click on the tile.");
            return;
        }
        if (tileData.hasMiningOutpost)
        {
            Debug.Log("Tile has a mining outpost. Cannot click on the tile.");
            return;
        }
        
    }*/

    public void SetRevealed(bool revealed)
    {
        if (tileData != null) tileData.isSurveyed = revealed;
        if (fogGO != null)
        {
            // if fogGO is an image you want to fade instead, implement coroutine/fade here
            fogGO.SetActive(!revealed);
        }
    }
}
