using System.Collections.Generic;
using UnityEngine;

public class ShipClassesManager : MonoBehaviour
{
    public static ShipClassesManager Instance { get; private set; }

    public List<ShipClass> shipClasses;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public Sprite GetShipLayoutBackground(ShipType shipType)
    {
        var shipClass = shipClasses.Find(s => s.shipType == shipType);
        return shipClass != null ? shipClass.ShipLayoutBackground : null;
    }
    public TextAsset GetShipLayoutJson(ShipType shipType)
    {
        var shipClass = shipClasses.Find(s => s.shipType == shipType);
        return shipClass != null ? shipClass.shipLayoutJson : null;
    }
    public ItemDefinition GetAssemblyResultItem(ShipType shipType)
    {
        var shipClass = shipClasses.Find(s => s.shipType == shipType);
        return shipClass != null ? shipClass.assemblyResultItem : null;
    }
    public GameObject GetAssemblyResultPrefab(ShipType shipType)
    {
        var shipClass = shipClasses.Find(s => s.shipType == shipType);
        return shipClass != null ? shipClass.assemblyResultPrefab : null;
    }
    public Sprite GetHullIcon(ShipType shipType)
    {
        var shipClass = shipClasses.Find(s => s.shipType == shipType);
        return shipClass != null ? shipClass.hullIcon : null;
    }
}

[System.Serializable]
public class ShipClass
{
    public ShipType shipType;
    
    public float maxHealthMod = 1;
    public float sizeMod = 1;
    public float energyCostMod = 1;
    public float craftingTimeMod = 1;
    public float craftingMaterialAmountMod = 1;

    [Header("Ship")]
    //public List<ItemDefinition> startModules = new List<ItemDefinition>();
    public Sprite ShipLayoutBackground;
    public TextAsset shipLayoutJson;
    public ItemDefinition assemblyResultItem;
    public GameObject assemblyResultPrefab;

    [Header("Hull")]
    public Sprite hullIcon;

    [Header("Weapon")]
    public int damageMod = 1;
    public int rangeMod = 1;

    [Header("Reactor")]
    public int energyMaxStorageMod = 1;
    public int energyProductionMod = 1;

    [Header("Storage")]
    public int storageSpaceMod = 1;
}
public enum ShipType
{
    None,
    Fighter,
    Corvette,
    CapitalShip,
    Stelite,
    DeployableMingingOutpostKit
}
