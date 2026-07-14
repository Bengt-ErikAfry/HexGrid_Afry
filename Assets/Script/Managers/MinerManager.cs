using UnityEngine;
#if UNITY_EDITOR
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;
#endif

public class MinerManager : MonoBehaviour
{
    public static MinerManager Instance { get; private set; }
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

    public void MineButton_Pressed()
    {
        Unit unit_script = SelectionService.Instance.SelectedUnit;

        //Check if Unit can mine
        if (!unit_script.DoUnitHaveOnlineModule(ModuleType.MiningLaser))
        { 
            string mainMessage =
            unit_script.unitName + "Do not have a Mining Laser";
            string subMessage = "";
            MessageSystemManager.Instance.CreateMessage(mainMessage, subMessage,
            unit_script.transform.position, Color.red);
            return;
        }

        //Set Stage to selectMiningTarget
        GameStateMachine.Instance.SetState(GameplayStateId.SelectingAsteroid);
    }

    /*
    public void StartMovingTowardsAsteroidField()
    {
        StartCoroutine(MovementManager.Instance.MoveUnitAlongPath(InputManager.Instance.selectedObject_Unit_Script, HexPathClickControllerPointTop_LineStrip.Instance.LastPath));
    }*/

    /*
    public bool CheckIfMineButtonShouldShow()
    {
        //Get shipHex coordinate
        Vector2Int shipHexCorrd = HexGridLinesBaker.Instance.GetGridPosFromWorldPos(InputManager.Instance.selectedObject.transform.position);

        if (GameManager.Instance.hexPointOfIntrest.ContainsKey(shipHexCorrd) && 
        {
            return true;
        }
        else
        {
            return false;
        }
    }*/
}
