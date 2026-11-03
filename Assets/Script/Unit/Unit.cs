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
    public string unitId; // Unique identifier for this unit, used for saving/loading and referencing.
    public string unitName = "Unit";
    public UnitType unitType;
    public ShipType shipType;   // Deside what icon in selection view to use. Also used for ship stats.
    public Vector2Int currentTileCoord;
    public int movedThisTurn = 0;
    private Transform col_DetectionRange;
    public float detectionRange = 5f;
    public List<GameObject> otherInDetectionRange = new List<GameObject>();
    public List<GameObject> detectedByOther = new List<GameObject>();
    //public int initativ = 5;
    public Transform grafics;

    public bool isPlayerControlled = true;
    public bool haveAttackedThisTurn = false;
    public int gunnerHitChance = 50;
    public int gunnerCriticalHitChance = 10;
    public Inventory unitInventory; //Not set in the inspector.
    public List<GameObject> pool_hitInfo = new List<GameObject>();
    public bool isAmingForModule = false;
    public ItemInstance moduleToHit;
    public Unit target_Unit_Script;
    public Vector3 target_LastKnownPos;
    public bool followTarget;
    public bool hasMinedThisTurn;
    public UnitLocationType unitLocationType = UnitLocationType.GameView;
    public List<Vector2Int> currentMovePath = new(); //used in movementManager to move unit.
    public bool isFreshlyCreatedUnit = false;


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

        grafics = this.transform.GetChild(0);
    }
    private void Start()
    {
        //Set detection range collider size
        col_DetectionRange = this.transform.GetChild(1);
        col_DetectionRange.localScale = new Vector3(detectionRange * 2, detectionRange * 2, 1);

        //Get Components.
        laserBeam_Script = GetComponent<LaserBeam>();

        //Add to GameManager list.
        if(GameManager.Instance.unitsList.Contains(this))
        {
            Debug.LogWarning("Unit already exists in GameManager unitsList: " + this.name);
        }
        else
        {
            GameManager.Instance.unitsList.Add(this);
            if (isPlayerControlled)
            {
                //Add to player unit list
                GameManager.Instance.playerUnits.Add(this);
            }
            else
            {
                //Add to enemy unit list
                GameManager.Instance.enemyUnits.Add(this);
            }
        }


        StartCoroutine(InitUnit(isFreshlyCreatedUnit)); //To register for FOG OF WAR to work.
    }

    //HexLayoutEditor.cs can obevisly not run a corutine!!!
    public void ApplyRuntimeVisuals()
    {
        if (grafics == null)
        {
            if (transform.childCount > 0)
                grafics = transform.GetChild(0);
        }

        if (grafics == null)
        {
            // Some spawned units do not have the standard child structure yet; continue without failing.
            return;
        }

        var spr = grafics.GetComponent<SpriteRenderer>();
        if (spr == null)
        {
            spr = GetComponentInChildren<SpriteRenderer>();
            if (spr != null)
                grafics = spr.transform;
        }

        if (spr != null)
        {
            GameObject managerObject = GameObject.Find("Managers");
            ShipClassesManager shipClassesManager = managerObject.GetComponent<ShipClassesManager>();
            if (shipClassesManager == null)
            {
                Debug.LogWarning($"ApplyRuntimeVisuals: ShipClassesManager.Instance is null for {name} (shipType={shipType}).");
                return;
            }

            Sprite sprite = shipClassesManager.GetShipIcon(shipType);
            if (sprite == null && shipType != ShipType.None)
                sprite = shipClassesManager.GetHullIcon(shipType);
            if (sprite == null && shipClassesManager.shipClasses != null && shipClassesManager.shipClasses.Count > 0)
                sprite = shipClassesManager.shipClasses[0].hullIcon;

            if (sprite != null)
                spr.sprite = sprite;
            else
                Debug.LogWarning($"ApplyRuntimeVisuals: no ship icon found for shipType={shipType} on {name}.");
        }

        if (string.IsNullOrEmpty(unitName) && string.IsNullOrEmpty(unitId))
            gameObject.name = "Unit";
        else if (string.IsNullOrEmpty(unitName))
            gameObject.name = "Unit (" + unitId + ")";
        else if (string.IsNullOrEmpty(unitId))
            gameObject.name = unitName;
        else
            gameObject.name = unitName + " (" + unitId + ")";
    }

    public void StartInitUnitCorutine()
    {
        Debug.Log("StartInitUnitCorutine called for unit: " + this.name);
        ApplyRuntimeVisuals();

        if (TileManager.Instance == null || !TileManager.Instance.HasBuiltLookup)
        {
            return;
        }

        StartCoroutine(InitUnit(false));
    }

    public IEnumerator InitUnit(bool isFreshlyCreatedUnit)
    {
        Debug.Log("InitUnit coroutine started for unit: " + this.name);
        ApplyRuntimeVisuals();

        // Wait until TileManager exists and has built its lookup
        yield return new WaitUntil(() => TileManager.Instance != null && TileManager.Instance.HasBuiltLookup);

        //Add to GameManager list.
        if (isPlayerControlled)
        {
            //Register to Tile Fog Of War
            TileManager.Instance.RegisterUnit(this, (int)detectionRange);
        }
        else
        {
            //Enemy units are not registered for fog of war, but you can add any other initialization logic here if needed.
        }

        //Set cordinates for this unit. Used for pathfinding and other tile-based logic.
        currentTileCoord = TileManager.Instance.WorldToAxial(this.transform.position);

        //Set ship icon in selection view
        if (grafics != null)
        {
            var sr = grafics.GetComponent<SpriteRenderer>();
            sr.sprite = shipRuntimeData.itemDefinition.icon;
        }

        //GameObject name for debuging
        this.gameObject.name = unitName + " (" + unitId + ")";

        if (isFreshlyCreatedUnit)
        {
            if (ModuleManager.Instance != null)
                ModuleManager.Instance.SetUpShipStats(this);
        }
    }

    public IEnumerator DoEnemyTurn(System.Action onFinished)
    {
        //Selected Unit enemy
        SelectionService.Instance.SetSelectedUnit(this);

        //Center on enemy
        CameraManager.Instance.CenterOnSelectedObject();

        //Hide Highlighter
        //HexHighlighter.Instance.HideHighlight();

        //If enemy have targets
        if (otherInDetectionRange.Count > 0)
        {
            // Have a target in detection range. Move to it and attack.

            //Get first target in list
            target_Unit_Script = otherInDetectionRange[0].GetComponent<Unit>();

            //set path to player
            //HexGridManager.Instance.CalculatePath(otherInDetectionRange[0].transform.position);

            // movement — updated to new MovementManager API
            var path = SelectionService.Instance.SelectedUnit.currentMovePath;
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
                    //HexGridManager.Instance.CalculatePath(target_LastKnownPos);

                    // movement — updated to new MovementManager API
                    var path = SelectionService.Instance.SelectedUnit.currentMovePath;
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
        if (isPlayerControlled)
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
        //FogOfWarManager.Instance.Unregister(this.GetComponent<FogVisionSource>());

        //Unsubscribe from Tile Fog Of War
        TileManager.Instance.UnregisterUnit(this);

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
}
public enum UnitType
{
    Ship,
    Asteroid,
    AsteroidField,
    CargoHualer,
    EnemyShip,
    Probe,
    Satelite,
    Building
}
public enum UnitLocationType
{
    GameView,
    MinableObject,
    Outpost
}

