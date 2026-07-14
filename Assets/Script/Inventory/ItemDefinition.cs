using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using static UnityEditor.Progress;
#endif

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Items/Item")]
public class ItemDefinition : ScriptableObject
{
    //All data are specified after a DEFAULT FIGHTER Ship type. Then "Shiptype" handels the diference between shiptypes.

    [Header("Item stats(Static)")]
    public string itemName;
    public Sprite icon;
    public ItemType itemType;
    //public ShipType shipType;
    public ModuleType moduleType;
    public bool itemCanStack = false;
    public int maxHealth = 100;
    public string description;
    public float size = 1;

    //Comon Module stats that all modules have.
    public int energyCost = 5;
    public float energyConsumtion_Boost = 1.5f;
    public int wear = 2;  //Higher in worse
    public float wearModifier_Boost = 2f;

    [Header("Craftable")]
    public int craftingTime; // in seconds
    [System.Serializable]
    public struct MaterialRequirement
    {
        public ItemDefinition item;
        public int amount;
    }
    public List<MaterialRequirement> requiredItems = new List<MaterialRequirement>();

    [Header("Weapon")]
    public int damage = 10;
    public int range = 3;
    public AmmoType ammoType = AmmoType.Energy;
    public float rangeAccuracyModifier = 0.5f;  //Higher in worse
    public float rangeDamageModifier = 0.5f;  //Higher in worse

    [Header("Weapon Boosters")]
    public float damage_Boost = 1.5f;
    public float range_Boost = 1.5f;
    public float rangeAccuracyModifier_Boost = 0.5f;  //Higher in better
    public float rangeDamageModifier_Boost = 0.5f;  //Higher in better

    [Header("Mining Laser")]
    public int miningSpeed = 0; //Rocks/turn

    [Header("Mining Laser Boost")]
    public float miningSpeed_Boost = 0;

    [Header("Engine")]
    public int movmentRange = 2;

    [Header("Engine Boosters")]
    public float movmentRange_Boost = 1.5f;

    [Header("Hull Boosters")]
    public float health_Boost = 1.5f;

    [Header("Reactor")]
    public int energyStartPowerBoost = 5;
    public int energyMaxStorage = 100;
    public int energyProduction = 5;

    [Header("Reactor Boosters")]
    public float extraStartPowerBoost_Boost = 2f;
    public float energyProduction_Boost = 1.5f;
    public float energyStorage_Boost = 1.5f;

    [Header("Storage")]
    public int storageSpace = 0;
}
public enum ItemType
{
    Rocks,
    Ore,
    Craftable,
    Tool,
    Ship,
    Units,
    Weapon,
    Module
}
public enum AmmoType
{
    Energy,
    Projectile,
    Explosive
}

//Used for determine what module to show in the moduleAming view.
public enum ModuleType
{
    None,
    Engine,
    Hull,
    Reactor,
    Weapon,
    HeavyWeapon,
    MiningLaser,
    Storage,
    ComunicationArray,
    SurvayArray,
    BoardingDock 
}