[System.Serializable]
public class ShipRuntimeData
{
    public ItemDefinition itemDefinition;

    public ShipType shipType;

    public int currentAvalibulePowerUpBoost;
    public int nextTurnAvalibulePowerUpBoost;
    public int currentMovmentRange;
    public int nextTurnMovmentRange;
    public int currentEnergyProduction;
    public int nextTurnEnergyProduction;
    public int currentEnergyMaxStorage;
    public int nextTurnEnergyMaxStorage;

    public int energyConsumptionThisTurn;
    public int energyStorage;
}
