using System.Collections;
using System.Collections.Generic;
using TMPro;
#if UNITY_EDITOR
using UnityEditor.Rendering;
using UnityEditor.VersionControl;
#endif
using UnityEngine;

public class Unit : MonoBehaviour
{
    public string unitName = "Unit";
    public Sprite unitSprite;
    //public int movmentRange = 3;
    public int movedThisTurn = 0;
    public Transform col_DetectionRange;
    public float detectionRange = 5f;
    public List<GameObject> otherInDetectionRange = new List<GameObject>();
    public List<GameObject> detectedByOther = new List<GameObject>();
    //public int initativ = 5;
    public Transform grafics;
    public UnitType unitType;
    public bool isPlayerControlled = true;
    public bool haveAttackedThisTurn = false;
    public int gunnerHitChance = 50;
    public int gunnerCriticalHitChance = 10;
    public Inventory unitInventory; //Not set in the inspector.
    public List<GameObject> pool_hitInfo = new List<GameObject>();
    public bool isAmingForModule=false;
    public ItemInstance moduleToHit;
    public Unit target_Unit_Script;
    public Vector3 target_LastKnownPos;
    public bool followTarget;
    public bool hasMinedThisTurn;

    [Header("Reference")]
    public LaserBeam laserBeam_Script;
    public FactoryComponent factoryComponent;
    public MinableComponent minableComponent;
    public RouteComponent routeComponent;

    [Header("RunTime Referencec")]
    public ShipRuntimeData shipRuntimeData;     //All ship variabels

    [SerializeField]
    public List<ItemInstance> moduleRuntimeList = new List<ItemInstance>();   // List of modules TO TAKE DAMAGE.

    private void Awake()
    {
        unitInventory = GetComponent<Inventory>();
        factoryComponent = GetComponent<FactoryComponent>();
        minableComponent = GetComponent<MinableComponent>();
        routeComponent = GetComponent<RouteComponent>();
    }
    private void Start()
    {
        col_DetectionRange.localScale = new Vector3(detectionRange * 2, detectionRange * 2, 1);
        laserBeam_Script = GetComponent<LaserBeam>();
    }    
    public IEnumerator DoEnemyTurn(System.Action onFinished)
    {
        //Selected Unit enemy
        SelectionService.Instance.SetSelectedUnit(this);

        //Center on enemy
        CameraManager.Instance.CenterOnSelectedObject();

        //Hide Highlighter
        HexHighlighter.Instance.HideHighlight();

        //If enemy have targets
        if (otherInDetectionRange.Count > 0)
        {
            // Have a target in detection range. Move to it and attack.

            //Get first target in list
            target_Unit_Script = otherInDetectionRange[0].GetComponent<Unit>();

            //set path to player
            HexPathClickControllerPointTop_LineStrip.Instance.HandleTapToObject(otherInDetectionRange[0].transform.position, false);

            // movement — updated to new MovementManager API
            var path = HexPathClickControllerPointTop_LineStrip.Instance.LastPath;
            if (path != null && path.Count >= 2)
            {
                int remainingSteps = Mathf.Max(0, shipRuntimeData.currentMovmentRange - movedThisTurn);

                MovementResult moveRes = default;
                bool finished = false;

                // MoveAlongPath now takes a maxSteps and a callback
                yield return MovementManager.Instance.MoveAlongPath(this, path, remainingSteps, res =>
                {
                    moveRes = res;
                    finished = true;
                });

                // defensive wait (MoveAlongPath will usually invoke callback before returning)
                while (!finished) yield return null;

                // optional: react to partial/failed movement if needed
            }

            //Get ramdom module to hit
            this.moduleToHit = ModuleViewManager.Instance.GetRandomModule();

            // attack
            yield return AttackManager.Instance.TryToAttack(this, otherInDetectionRange[0]); //StartCoroutine(AttackTarget());

            //Save targets location for if detection are lost next trune.
            target_LastKnownPos = otherInDetectionRange[0].transform.position;
            followTarget = true;
        }
        else
        {
            if (followTarget)
            {
                //Target lost. Move to last known location.

                //Move if not at that position
                if (this.transform.position != target_LastKnownPos)
                {
                    //set path to last known location
                    HexPathClickControllerPointTop_LineStrip.Instance.HandleTapToObject(target_LastKnownPos, false);

                    // movement — updated to new MovementManager API
                    var path = HexPathClickControllerPointTop_LineStrip.Instance.LastPath;
                    if (path != null && path.Count >= 2)
                    {
                        int remainingSteps = Mathf.Max(0, shipRuntimeData.currentMovmentRange - movedThisTurn);

                        MovementResult moveRes = default;
                        bool finished = false;

                        // MoveAlongPath now takes a maxSteps and a callback
                        yield return MovementManager.Instance.MoveAlongPath(this, path, remainingSteps, res =>
                        {
                            moveRes = res;
                            finished = true;
                        });

                        // defensive wait (MoveAlongPath will usually invoke callback before returning)
                        while (!finished) yield return null;

                        // optional: react to partial/failed movement if needed
                    }
                }
            }
        }

        // we’re done!
        onFinished?.Invoke();
    }

    public bool DoUnitHaveOnlineModule(ModuleType typeToTest)
    {
        bool haveModule = false;
        foreach (var module in moduleRuntimeList)
        {
            if (module.ItemDefinition != null)
            {
                if (module.ItemDefinition.moduleType == typeToTest && module.isOnline)
                {
                    haveModule = true;
                }
            }
        }
        return haveModule; 
    }
    //When unit die or gets removed from game.
    public void RemoveUnitFromPlay()
    {
        //Remove from Gamanager list.
        if(isPlayerControlled)
        {
            //Remove from player unit list
            GameManager.Instance.playerUnits.Remove(this);
        }
        else
        {
            //Remove from enemy unit list
            GameManager.Instance.enemyUnits.Remove(this);
        }
        GameManager.Instance.unitsList.Remove(this);

        //Unsubscribe from fogOfWar
        FogOfWarManager.Instance.Unregister(this.GetComponent<FogVisionSource>());

        //Delite GO
        Destroy(this.gameObject);
    }

    public void DetectionTriggerColidedWithOther(GameObject other)
    {
        Debug.Log("DetectionTriggerColidedWithOther: this: " + this.transform.root.name + " other: " + other.transform.root.name);
        //Check if the other is friendly or eviel. Only add Evel to the list.
        if (other.transform.root.gameObject.tag == this.gameObject.tag)
        {
            //Debug.Log("Friendly detected. Ignore. This tag: " + this.gameObject.tag + " other tag: " + this.transform.root.gameObject.tag);
            return;
        }
        if (!otherInDetectionRange.Contains(other))
        {
            otherInDetectionRange.Add(other);

            Unit other_Unit_script = other.GetComponent<Unit>();

            // Player collide with Enemy
            if (other.transform.root.gameObject.tag == "Enemy")
            {
              //  GameManager.Instance.AddObjectToTurnOrder(other.transform.root.gameObject.GetComponent<Unit>());
                other_Unit_script.grafics.gameObject.SetActive(true);
            }


            if (!other_Unit_script.detectedByOther.Contains(this.gameObject))
            {
                other_Unit_script.detectedByOther.Add(this.gameObject);
            }
        }
    }
    public void DetectionTriggerExitWithOther(GameObject other)
    {
        if (otherInDetectionRange.Contains(other))
        {
            otherInDetectionRange.Remove(other);

            Unit other_Unit_script = other.transform.root.gameObject.GetComponent<Unit>();
            other_Unit_script.detectedByOther.Remove(this.gameObject);

            // No one detect this unit anymore
            if (other_Unit_script.detectedByOther.Count == 0)
            {
                // Player exit collide with Enemy
                if (other.transform.root.gameObject.tag == "Enemy")
                {
       //             GameManager.Instance.RemoveObjectToTurnOrder(other.transform.root.gameObject.GetComponent<Unit>());
                    other_Unit_script.grafics.gameObject.SetActive(false);
                }
            }
        }
    }

    public enum UnitType
    {
        Ship,
        Asteroid,
        AsteroidField,
        CargoHualer,
        EnemyShip,
        Probe,
        Satelite
    }
}
