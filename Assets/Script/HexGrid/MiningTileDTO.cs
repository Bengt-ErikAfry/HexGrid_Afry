using System;
using System.Collections.Generic;

// DTOs used for JSON import/export -- plain data only (no GameObject refs).
[Serializable]
public class MiningTileDTO
{
    public int tileIndexRow;
    public int tileIndexCol;
    public bool isSurveyed;
    public bool hasMiningOutpost;
    public bool hasAbandonMiningOutpost;
    public float oreAmount;
    // Add other primitive fields you want to persist.
}

[Serializable]
public class MiningTileCollectionDTO
{
    public List<MiningTileDTO> tiles = new List<MiningTileDTO>();
    public int nrOfTiles;
}