using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class MinableComponent : MonoBehaviour
{
    [Header("Tiles")]
    public List<MiningObjectTileData> tiles = new List<MiningObjectTileData>();
    public int nrOfTiles;
    public Sprite backgroundSprite;
    public int scrollLimit;

    [Header("Resource")]
    public ItemDefinition resourceType;
    public int totalAmount = 0;      // remaining resource in the ground
    public int storedAmount = 0;     // produced and ready for pickup (via outposts)

    public event Action OnSurveyed;
    public event Action<int> OnOutpostPlaced; // slotIndex
    public event Action OnDepleted;
    public event Action OnProduced; // fired when outpost production added to storedAmount

    private void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnNewTurn += HandleNewTurn;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnNewTurn -= HandleNewTurn;
    }

    private void Start()
    {
        // Initialize tiles if not already set
        if (tiles.Count == 0)
        {
            for (int i = 0; i < nrOfTiles; i++)
            {
                var newTileData = new MiningObjectTileData {};
                newTileData.tileIndexRow = i / 10;
                newTileData.tileIndexCol = i % 10;
                    if (i == 2) { newTileData.isSurveyed = true; }

                tiles.Add(newTileData);
                //tiles.Add(new MiningObjectTileData { tileIndexRow = i / 10, tileIndexCol = i % 10 });
            }
        }
    }

    

    // Called by UI or game logic to survey this minable (reveal slots, etc.)
    /* public void StartSurvey()
     {
         if (isSurveyed) return;
         isSurveyed = true;
         OnSurveyed?.Invoke();
     }

     // Place an outpost on a given surface slot index. Returns true if placed.
     public bool PlaceOutpost(int slotIndex)
     {
         if (slotIndex < 0 || slotIndex >= surfaceSlots.Count) return false;
         var slot = surfaceSlots[slotIndex];
         if (!slot.isSurveyed || slot.hasMiningOutpost) return false;

         slot.hasMiningOutpost = true;
         OnOutpostPlaced?.Invoke(slotIndex);
         return true;
     }*/

    // Called per player turn (Option A). Produces resources from outposts into storedAmount.
    private void HandleNewTurn()
    {
        //ProduceFromOutposts();
    }
    /*
    // Count valid outposts and produce
    public void ProduceFromOutposts()
    {
        if (!isSurveyed) return;
        int outpostCount = 0;
        foreach (var s in surfaceSlots)
        {
            if (s.hasMiningOutpost && !s.hasAbandonMiningOutpost) outpostCount++;
        }

        if (outpostCount <= 0) return;

        int produce = outpostCount * productionPerOutpost;
        int actual = Mathf.Min(produce, totalAmount);
        if (actual <= 0) return;

        totalAmount -= actual;
        storedAmount += actual;

        OnProduced?.Invoke();

        if (totalAmount <= 0)
        {
            OnDepleted?.Invoke();
        }
    }

    // Unit-based extraction: compute extraction amount based on module mining speed and site multiplier.
    // This is the operation used by MineAction for manual mining by a unit.
    // Returns how many units were extracted from ground (not from storedAmount).
    public int ExtractUsingTool(int moduleMiningSpeed)
    {
        if (moduleMiningSpeed <= 0 || totalAmount <= 0) return 0;

        int extracted = Mathf.FloorToInt(moduleMiningSpeed * deliverMultiplier);
        extracted = Mathf.Max(0, extracted);
        extracted = Mathf.Min(extracted, totalAmount);
        totalAmount -= extracted;

        if (totalAmount <= 0) OnDepleted?.Invoke();

        return extracted;
    }

    // Units (or UI) can withdraw produced (storedAmount) that outposts produced.
    // Returns how many units actually taken.
    public int TryWithdrawStored(int amount)
    {
        if (amount <= 0 || storedAmount <= 0) return 0;
        int take = Mathf.Min(amount, storedAmount);
        storedAmount -= take;
        return take;
    }

    // Helper: get number of outposts currently active
    public int GetOutpostCount()
    {
        int c = 0;
        foreach (var s in surfaceSlots) if (s.hasMiningOutpost && !s.hasAbandonMiningOutpost) c++;
        return c;
    }*/
}