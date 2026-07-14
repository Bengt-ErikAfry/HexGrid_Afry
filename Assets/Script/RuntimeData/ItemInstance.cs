using System.Diagnostics;
using UnityEngine;

[System.Serializable]
public class ItemInstance 
{
    //Defenition (the module data Do not Change this)
    public ItemDefinition ItemDefinition;

    public ShipType shipType;

    public int currentHealth;
    public int nextTurnHealth;
    public int currentMaxHealth;
    public int nextTurnMaxHealth;

    public int currentPowerUpBoost = 1;
    public int nextTurnPowerUpboost = 1;

    public int currentDamage;
    public int nextTurnDamage;
    public int currentRange;
    public int nextTurnRange;
    public int currentEnergyCostValue;
    public int nextTurnEnergyCostValue;
    public int currentEnergyStartPowerBoost;
    public int nextTurnEnergyStartPowerBoost;
    public int currentEnergyProduction;
    public int nextTurnEnergyProduction;
    public int currentEnergyMaxStorage;
    public int nextTurnEnergyMaxStorage;
    public int currentWear;
    public int nextTurnWear;
    public int currentStorageSpace;
    public int nextTurnStorageSpace;
    public int currentMiningSpeed;
    public int nextTurnMiningSpeed;

    public bool isOnline = false;
    public bool isBroken = false;
    public bool isDestroyd = false;

    public int currentSolidersGarding = 0;
    public int nextTurnSolidersGarding = 0;
    public ItemInstance Clone()
    {
        return (ItemInstance)this.MemberwiseClone();
    }
public void TakeDamage(int amount)
    {
        //Cant take more damage then destroyd
        if (isDestroyd) return;

        //Calculate DAMAGE
        currentHealth = currentHealth - amount;
        if (currentHealth < 0) { currentHealth = 0; }
        nextTurnHealth = currentHealth;
        UnityEngine.Debug.Log($"{ItemDefinition.itemName} taken: {amount} Current Health: {currentHealth}"); 

        //Get status.
        GetComponentStatus();
    }
    public void Repair(int amount)
    {
        currentHealth = Mathf.Min(ItemDefinition.maxHealth, currentHealth + Mathf.Abs(amount));

        //Check if broken
        if (currentHealth >= -ItemDefinition.maxHealth / 2) { isBroken = false; }
    }
    public string GetComponentStatus()
    {
        //Check if destroyd
        if (currentHealth <= 0)
        {
            isDestroyd = true;
            return "Destroyd";
        }
        else
        {
            //Check if broken
            if (currentHealth <= currentMaxHealth / 2)
            {
                isBroken = true;
                isOnline = false;
                return "Broken";
            }
            else
            {
                return "OnLine";
            }
        }
    }
}
