using UnityEngine;
#if UNITY_EDITOR
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;
#endif
public class EnergyManager : MonoBehaviour
{
    public static EnergyManager Instance { get; private set; }
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

    public void CalculateEnergyConsumption()
    {
        foreach (var playerUnit in GameManager.Instance.playerUnits)
        {
            playerUnit.shipRuntimeData.energyConsumptionThisTurn = 0;    // Reset the total energy consumtion before calculating it again.  

            foreach (var module in playerUnit.moduleRuntimeList)
            {
                if (module.isOnline)
                {
                    playerUnit.shipRuntimeData.energyConsumptionThisTurn = playerUnit.shipRuntimeData.energyConsumptionThisTurn + module.currentEnergyCostValue;
                }
            }
        }
    }

    public void ApplyEnergyCostAndTurnOffModules()
    {
        foreach (var playerUnit in GameManager.Instance.playerUnits)
        {
            //Apply energy cost
            playerUnit.shipRuntimeData.energyStorage = playerUnit.shipRuntimeData.energyStorage - playerUnit.shipRuntimeData.energyConsumptionThisTurn;
            //Turn off modules if unit dont have energy.
            if (playerUnit.shipRuntimeData.energyStorage < 0)
            {
                playerUnit.shipRuntimeData.energyStorage = 0;

                foreach (var module in playerUnit.moduleRuntimeList)
                {
                    module.isOnline = false;
                }

                //Sen player message
                string mainMessage =
                "OUT OF ENERGY:" + playerUnit.unitName;
                string subMessage = "";
                MessageSystemManager.Instance.CreateMessage(mainMessage, subMessage,
                playerUnit.transform.position, Color.white);
            }
        }
    }
}
