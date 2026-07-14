using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;
#endif
using static UnityEngine.GraphicsBuffer;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public Unit unit_script;

    public GameObject moveButton;
    public GameObject executeMoveButton;

    public GameObject endTurnButton;
    public GameObject nextUnitButton;
    public GameObject attackButton;
    public GameObject mineButton;

    private int currentTurnIndex = -1;                   //Player unit to move/attack. Incress if player click next unit button.

    public RectTransform stackViewRectTransform;
    public Transform stackViewContent;
    public GameObject stackViewPrefab;

    public GameObject BlockPlayerInputPanel;

    public GameObject selectedUnitView;
    public Image selectedUnitImage;
    public TMP_Text selectedUnitNameText;
    public TMP_Text selectedUnitQuickStatsText;

    public TMP_Text activeState;

    public RectTransform asteroidView;

    public TMP_Text routeButtonText;
    public Button routeButton;
    public GameObject routeButtonGO;

    private Coroutine _waitForSelectionServiceCoroutine;

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

    private void OnEnable()
    {
        // If SelectionService already exists subscribe immediately and initialize UI.
        if (SelectionService.Instance != null)
        {
            SelectionService.Instance.OnSelectionChanged += OnSelectionChanged;
            unit_script = SelectionService.Instance.SelectedUnit;
            //UpdateRouteButton(unit_script);
            ShowSelectedUnitView(unit_script);
        }
        else
        {
            // Wait for SelectionService to be created (safe startup ordering)
            _waitForSelectionServiceCoroutine = StartCoroutine(WaitForSelectionService());
        }
    }

    private IEnumerator WaitForSelectionService()
    {
        while (SelectionService.Instance == null)
            yield return null;

        SelectionService.Instance.OnSelectionChanged += OnSelectionChanged;
        unit_script = SelectionService.Instance.SelectedUnit;
        //UpdateRouteButton(unit_script);
        ShowSelectedUnitView(unit_script);
        _waitForSelectionServiceCoroutine = null;
    }

    private void OnDisable()
    {
        if (SelectionService.Instance != null)
            SelectionService.Instance.OnSelectionChanged -= OnSelectionChanged;

        if (_waitForSelectionServiceCoroutine != null)
        {
            StopCoroutine(_waitForSelectionServiceCoroutine);
            _waitForSelectionServiceCoroutine = null;
        }
    }

    private void OnSelectionChanged(Unit unit)
    {
        unit_script = unit;
        UpdateRouteButton(unit);
        ShowSelectedUnitView(unit);

        // Hide old path when nothing is selected, or when the selected unit has no route.
        if (HexPathClickControllerPointTop_LineStrip.Instance == null) return;

        if (unit == null)
        {
            HexPathClickControllerPointTop_LineStrip.Instance.ClearPath();
            return;
        }

        // If the selected unit has route actions, visualize the full route; otherwise clear any visible path.
        var rc = unit.GetComponent<RouteComponent>();
        if (rc != null && rc.routeActions != null && rc.routeActions.Count > 0)
        {
            HexPathClickControllerPointTop_LineStrip.Instance.CalculateRoutePath(unit);
        }
        else
        {
            HexPathClickControllerPointTop_LineStrip.Instance.ClearPath();
        }
    }

    public void ResetUI()
    {
        moveButton.SetActive(true);
        executeMoveButton.SetActive(false);

        endTurnButton.SetActive(true);
        nextUnitButton.SetActive(true);
        attackButton.SetActive(true);
        mineButton.SetActive(true);
        routeButtonGO.SetActive(true);
    }

    public void HideAllUI()
    {
        moveButton.SetActive(false);
        executeMoveButton.SetActive(false);

        endTurnButton.SetActive(false);
        nextUnitButton.SetActive(false);
        attackButton.SetActive(false);
        mineButton.SetActive(false);
        routeButtonGO.SetActive(false);
    }

    //Called from selectionState when player select a unit. This is to prevent player from moving enemy units.
    public void HideUnitMovmentControlls()
    {
        moveButton.SetActive(false);
        executeMoveButton.SetActive(false);
    }

    public void HideUnitAttackControlls()
    {
        attackButton.SetActive(false);
    }

    public void EnterPathPlaningMode()
    {
        moveButton.SetActive(false);
        executeMoveButton.SetActive(true);
    }

    //Called from UI when player click "Move Unit" button. Set state to planning path and let player click on tile to move to.
    public void MoveUnitButtonPressed()
    {
        GameStateMachine.Instance.SetState(GameplayStateId.PlanningPath);
    }

    //Called from UI when player click "Execute Move" button. Start move unit along path.
    public void ExecuteMoveButtonPressed()
    {
        if (SelectionService.Instance.SelectedUnit == null)
        {
            Debug.LogWarning("ExecuteMoveButtonPressed: unit_script is null.");
            return;
        }
        
        // STOP any running route for this unit (do not remove the route data by default)
        RouteExecutor.Instance?.StopRouteFor(SelectionService.Instance.SelectedUnit);

        var path = HexPathClickControllerPointTop_LineStrip.Instance.LastPath;
        if (path == null || path.Count < 2)
        {
            Debug.LogWarning("No path to move along.");
            return;
        }

        // Compute step budget = movement range left this turn
        int remainingSteps = Mathf.Max(0, unit_script.shipRuntimeData.currentMovmentRange - unit_script.movedThisTurn);
        if (remainingSteps <= 0)
        {
            Debug.Log("Unit has no movement points left.");
            return;
        }

        //Lock player input when unit moves
        GameStateMachine.Instance.SetState(GameplayStateId.BlockPlayerInput);

        Debug.Log("ExecuteMoveButtonPressed: " + unit_script.gameObject.name);
        // Start the movement coroutine. Provide a callback to react when movement attempt finishes.
        StartCoroutine(MovementManager.Instance.MoveAlongPath(unit_script, path, remainingSteps, result =>
        {
            // Runs on completion (or partial / failure)
            if (result.Failed)
            {
                Debug.Log("Movement failed.");
            }
            else if (result.Completed)
            {
                Debug.Log("Movement completed to destination.");
            }
            else
            {
                Debug.Log($"Movement used {result.StepsUsed} steps and stopped (will retry next turn if route).");
            }

            // Update UI / camera / state as needed
            UIManager.Instance.ShowSelectedUnitView(unit_script);
            CameraManager.Instance.CenterOnSelectedObject();
            GameStateMachine.Instance.SetState(GameplayStateId.Selecting);
        }));
    }

    //Called from UI
    public void AttackButtonPressed()
    {
        GameStateMachine.Instance.SetState(GameplayStateId.Attacking);
    }

    // Updated to use RouteExecutor (start/stop) instead of toggling a flag on RouteComponent
    public void RouteButton_Pressed()
    {
        if (unit_script == null)
        {
            Debug.LogWarning("unit_script is null in UIManager.");
            return;
        }

        var rc = unit_script.GetComponent<RouteComponent>();
        if (rc == null)
        {
            Debug.LogWarning("unit missing route component.");
            return;
        }

        // If route is actively running -> stop it and hide visuals (do NOT remove actions)
        //if (RouteExecutor.Instance != null && RouteExecutor.Instance.IsRouteRunning(unit_script))
        if(rc.IsRouteStarted)
        {
            RouteExecutor.Instance.StopRouteFor(unit_script);
            HexPathClickControllerPointTop_LineStrip.Instance?.ClearPath();
            rc.IsRouteStarted = false;
            routeButtonText.text = "Run Route";
            return;
        }
        else
        // If there is a route -> start it from the beginning
        //if (rc.HasActiveRoute())
        {
            rc.actionIndex = 0; // start from beginning
            RouteExecutor.Instance?.StartRouteFor(unit_script);
            rc.IsRouteStarted = true;
            routeButtonText.text = "Stop Route";
            return;
        }

        /*
        // Fallback: nothing to run
        Debug.LogWarning("RouteButton pressed but no route available for this unit.");
        routeButtonText.text = "Run Route";
        */
    }

    // NEW: explicit-update API — callers must pass the Unit to inspect (no InputManager fallback)
    public void UpdateRouteButton(Unit u)
    {
        if (u == null)
        {
            routeButton.interactable = false;
            routeButtonText.text = "Run Route";
            return;
        }

        var rc = u.GetComponent<RouteComponent>();
        if (rc == null)
        {
            routeButton.interactable = false;
            routeButtonText.text = "Run Route";
            return;
        }

        // Enable button when there are actions to run
        routeButton.interactable = true;

        // Show Stop only while route coroutine is running
        if (rc.IsRouteStarted)
        {
            routeButtonText.text = "Stop Route";
            return;
        }
        else
        {
            routeButtonText.text = "Run Route";
        }
    }

    //Called from UI when player click "Next Unit" button. Select next unit in player unit list and start turn for that unit. If no more unit have moves left start from the top of the list.
    public void SelectNextPLAYERUnitInUnitList()
    {
        //Increment list
        currentTurnIndex++;
        if (currentTurnIndex > GameManager.Instance.playerUnits.Count - 1) { currentTurnIndex = 0; }

        //Select unit (still setting InputManager for other legacy code)
        SelectionService.Instance.SetSelectedUnit(GameManager.Instance.playerUnits[currentTurnIndex]);

        // Update UI-managed selection
        unit_script = GameManager.Instance.playerUnits[currentTurnIndex];

        //Send message to player
        MessageSystemManager.Instance.CreateMessage("Unit " + unit_script.gameObject.name + " turne started with "
            + (unit_script.shipRuntimeData.currentMovmentRange - unit_script.movedThisTurn) + " moves left!"
            , null, HexGridLinesBaker.Instance.GetGridPosFromWorldPos(unit_script.transform.position), Color.green);

        //Reset UI
        ResetUI();

        //Set selecting state
        GameStateMachine.Instance.SetState(GameplayStateId.Selecting);

        //Move Camera
        CameraManager.Instance.CenterOnSelectedObject();

        //Show selected unit
        ShowSelectedUnitView(unit_script);

        /*   ---- Remove with new mining system
        //AutoRun miner if on mission
        if (unit_script.miningComponent != null && unit_script.miningComponent.HasMission)
        {
            unit_script.miningComponent.OnTurn();
        }
        */
    }

    public void ActivateSelectedUnit()
    {
        //Reset UI
        ResetUI();

        //Set selecting state
        GameStateMachine.Instance.SetState(GameplayStateId.Selecting);

        //Move Camera
        CameraManager.Instance.CenterOnSelectedObject();

        //Show selected unit
        ShowSelectedUnitView(unit_script);
    }

    //Called from attack and selection state when player click on hex with more than one unit. Show stack view with all units in hex and let player select unit to attack or select.
    public void ShowStackView(List<Unit> unitsInClickedHex, Vector2 screenPos)
    {
        //Show stack view

        stackViewRectTransform.anchoredPosition = new Vector2((-Screen.width / 2) + screenPos.x, (-Screen.height / 2) + screenPos.y);
        stackViewRectTransform.gameObject.SetActive(true);

        //Clear old units entries
        foreach (Transform oldUnits in stackViewContent)
        {
            Destroy(oldUnits.gameObject);
        }

        Debug.Log("ShowStackView: " + unitsInClickedHex.Count + " units in clicked hex.");

        // Populate new stackView
        for (int i = 0; i < unitsInClickedHex.Count; i++)
        {
            //Instatiate
            GameObject newStackViewPrefab = Instantiate(stackViewPrefab, stackViewContent);

            //Get entery script
            StackViewUnit_Entery stackViewUnit_Entery_Script = newStackViewPrefab.GetComponent<StackViewUnit_Entery>();

            //Set Image
            stackViewUnit_Entery_Script.unitSprite.sprite = unitsInClickedHex[i].unitSprite;

            //Set name
            stackViewUnit_Entery_Script.unitName.text = unitsInClickedHex[i].unitName;

            //Set unit script
            stackViewUnit_Entery_Script.unit_script = unitsInClickedHex[i];
        }
    }

    public void HideStackView()
    {
        stackViewRectTransform.gameObject.SetActive(false);
    }

    // UPDATED: explicit parameter — show view for provided unit (no InputManager dependency)
    public void ShowSelectedUnitView(Unit unit)
    {
        if (unit != null)
        {
            unit_script = unit;

            ResetUI();

            selectedUnitView.SetActive(true);
            selectedUnitImage.sprite = unit_script.unitSprite;
            selectedUnitNameText.text = unit_script.unitName;
            selectedUnitQuickStatsText.text = $"Movment{unit_script.movedThisTurn} / {unit_script.shipRuntimeData.currentMovmentRange}\n Have Attacked: {unit_script.haveAttackedThisTurn}";
            switch (unit_script.unitType)
            {
                case Unit.UnitType.Ship:
                    break;
                    /* ----- Remove with new minging system
                case Unit.UnitType.Asteroid:
                    break;
                case Unit.UnitType.AsteroidField:
                    ShowAsteroidField_SelectedUnitView();
                    break;*/
                case Unit.UnitType.CargoHualer:
                    break;
                case Unit.UnitType.EnemyShip:
                    break;
                case Unit.UnitType.Probe:
                    break;
                default:
                    break;
            }
        }
        else
        {
            selectedUnitView.SetActive(false);
        }
    }

    /* ----- Remove with new minging system
    public void ShowAsteroidField_SelectedUnitView()
    {
        if (unit_script != null && unit_script.asteroidFieldComponent.isSurveyed)
        {
            selectedUnitNameText.text = $"{unit_script.unitName} Rocks left: {unit_script.asteroidFieldComponent.totalAmount}";
        }
        else if (unit_script != null)
        {
            selectedUnitNameText.text = $"{unit_script.unitName} Unsurveyed.";
        }
    }*/

    public void HideSelectedUnitView()
    {
        selectedUnitView.SetActive(false);
        HideAllUI();
    }

    /* ----- Remove with new minging system
    public void ShowAsteroidView()
    {
        asteroidView.offsetMax = new Vector2(0, 0);
        asteroidView.offsetMin = new Vector2(0, 0);
        asteroidView.gameObject.SetActive(true);
    }
    //Called from close button
    public void HideAsteroidView()
    {
        asteroidView.gameObject.SetActive(false);
    }
    */
}
