using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RouteManager : MonoBehaviour
{
    public static RouteManager Instance;

    public GameObject routeScrollViewContentGO;
    public GameObject routeSlotPrefab;
    public TMP_Dropdown itemDropDown;
    public TMP_InputField itemQuantityInputField;
    public Toggle allAvalibuleItemsToggle;
    public Toggle noWaitingForItems;
    public Toggle waitingForItems;
    public Toggle loopRouteToggle;
    public GameObject pickupDropPanel;
    public TMP_Text ObjectText;
    public AddRouteType addRouteType;
    public List<RouteSlotUI> routeSlotUI_List = new List<RouteSlotUI>();

    public Inventory selectedTargetInventory;

    public int routeEntitySelectedIndex = 0;

    public List<ItemDefinition> allItemsList = new List<ItemDefinition>();

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // UI: show route for explicit unit (no InputManager usage)
    public void UpdateRouteViewFor(Unit unit)
    {
        if (unit == null)
        {
            Debug.LogError("No unit provided to show route for!");
            return;
        }

        //Clear old UI
        foreach (Transform child in routeScrollViewContentGO.transform) Destroy(child.gameObject);

        var routeComponent = unit.GetComponent<RouteComponent>();
        if (routeComponent == null)
        {
            Debug.LogError("Selected ship does not have a RouteComponent!");
            return;
        }

        //Create entity
        routeSlotUI_List.Clear();
        for (int i = 0; i < routeComponent.routeActions.Count; i++)
        {
            GameObject routeSlot = Instantiate(routeSlotPrefab, routeScrollViewContentGO.transform);
            var slotUI = routeSlot.GetComponent<RouteSlotUI>();
            slotUI.Setup(routeComponent.routeActions[i], i);
            routeSlotUI_List.Add(slotUI);
        }

        routeComponent.loopRoute = loopRouteToggle.isOn;
        UpdateRouteSlotColor(routeEntitySelectedIndex);
        LoadAllItemsList();
        LoadAllItemsToDroppdown();
    }

    public void MoveSlotUp(int slotToMove)
    {
        //Change the order in the slotlist
        if (slotToMove <= 0 || slotToMove >= routeSlotUI_List.Count)
            return;

        (routeSlotUI_List[slotToMove - 1], routeSlotUI_List[slotToMove]) =
            (routeSlotUI_List[slotToMove], routeSlotUI_List[slotToMove - 1]);

        //Change the order in action list.
        var unit = SelectionService.Instance.SelectedUnit;
        if (unit == null)
        {
            Debug.LogWarning("MoveSlotUp: no unit selected.");
            return;
        }

        var rc = unit.GetComponent<RouteComponent>();
        if (rc == null)
        {
            Debug.LogWarning("MoveSlotUp: selected unit has no RouteComponent.");
            return;
        }

        // swap actions in the route data
        var tmp = rc.routeActions[slotToMove - 1];
        rc.routeActions[slotToMove - 1] = rc.routeActions[slotToMove];
        rc.routeActions[slotToMove] = tmp;

        routeEntitySelectedIndex = slotToMove - 1;
        UpdateRouteViewFor(SelectionService.Instance.SelectedUnit);
    }

    public void MoveSlotDown(int slotToMove)
    {
        if (slotToMove < 0 || slotToMove >= routeSlotUI_List.Count -1)
            return;

        (routeSlotUI_List[slotToMove + 1], routeSlotUI_List[slotToMove]) =
            (routeSlotUI_List[slotToMove], routeSlotUI_List[slotToMove + 1]);

        var unit = SelectionService.Instance.SelectedUnit;
        if (unit == null)
        {
            Debug.LogWarning("MoveSlotDown: no unit selected.");
            return;
        }

        var rc = unit.GetComponent<RouteComponent>();
        if (rc == null)
        {
            Debug.LogWarning("MoveSlotDown: selected unit has no RouteComponent.");
            return;
        }

        // swap actions in the route data
        var tmp = rc.routeActions[slotToMove + 1];
        rc.routeActions[slotToMove + 1] = rc.routeActions[slotToMove];
        rc.routeActions[slotToMove] = tmp;

        routeEntitySelectedIndex = slotToMove + 1;
        UpdateRouteViewFor(SelectionService.Instance.SelectedUnit);
    }

    public void DeleteSlotRoute(int slotToDelete)
    {

    }

    public void SelectRouteSlot(int index)
    {
        routeEntitySelectedIndex = index;
        UpdateRouteSlotColor(index);
    }

    public void UpdateRouteSlotColor(int index)
    {
        //Update UI
        for (int i = 0; i < routeSlotUI_List.Count; i++)
        {
            if (routeEntitySelectedIndex == i)
            {
                routeSlotUI_List[i].GetComponent<Outline>().effectColor = ColorManager.Instance.routeSlotSelected;
            }
            else
            {
                routeSlotUI_List[i].GetComponent<Outline>().effectColor = ColorManager.Instance.routeSlotNotSelected;
            }
        }
    }

    public void CleareRoute_BT_Pressed()
    {
        SelectionService.Instance.SelectedUnit.routeComponent.routeActions.Clear();
        UpdateRouteViewFor(SelectionService.Instance.SelectedUnit);
    }

    public void LoopRoute_Changed()
    {
        SelectionService.Instance.SelectedUnit.routeComponent.loopRoute = loopRouteToggle.isOn;
    }
    
    public void AddWaypoint_BT_pressed()
    {
        //Hide ui
        InfoScreenManager.Instance.HideInfoScreen();

        //Calculate exsisting path for the selected unit
        HexGridManager.Instance.CalculateRoutePath(SelectionService.Instance.SelectedUnit);

        pickupDropPanel.SetActive(false);
        addRouteType=AddRouteType.Waypoint;
        GameStateMachine.Instance.SetState(GameplayStateId.SetWaypoint);

        // To select new entery.
        routeEntitySelectedIndex = routeSlotUI_List.Count;

        //UIManager.Instance.UpdateRouteButton(SelectionService.Instance.SelectedUnit);
    }

    public void AddPickUp_BT_pressed()
    {
        //Hide ui
        InfoScreenManager.Instance.HideInfoScreen();

        pickupDropPanel.SetActive(false);
        addRouteType =AddRouteType.PickUp;
        GameStateMachine.Instance.SetState(GameplayStateId.SetPickup);
    }

    public void AddDrop_BT_Pressed()
    {
        //Hide ui
        InfoScreenManager.Instance.HideInfoScreen();

        pickupDropPanel.SetActive(false);
        addRouteType =AddRouteType.Drop;
        GameStateMachine.Instance.SetState(GameplayStateId.SetDrop);
    }

    public void Add_BT_Pressed()
    {
        switch (addRouteType)
        {   
            case AddRouteType.Waypoint:

                break;
            case AddRouteType.PickUp:
                AddPickUpItem();
                break;
            case AddRouteType.Drop:
                AddDropItem();
                break;
            default:
                break;
        }
        UpdateRouteViewFor(SelectionService.Instance.SelectedUnit);
    }

    public void LoadAllItemsToDroppdown()
    {
        itemDropDown.ClearOptions();
        var options = new List<string>();
        foreach (var obj in allItemsList) options.Add(obj.name);
        itemDropDown.AddOptions(options);
    }

    public void LoadAllItemsList()
    {
        allItemsList.Clear();
        allItemsList.AddRange(Resources.LoadAll<ItemDefinition>("Modules"));
        allItemsList.AddRange(Resources.LoadAll<ItemDefinition>("Ore"));
        allItemsList.AddRange(Resources.LoadAll<ItemDefinition>("Ships"));
        allItemsList.AddRange(Resources.LoadAll<ItemDefinition>("Units"));
        allItemsList.AddRange(Resources.LoadAll<ItemDefinition>("Weapons"));
    }

    // Create actions for a specific unit (UI should pass the unit)
    public void AddWaypointToRoute(Unit unit, Vector3 waypointPosition)
    {
        var rc = unit.GetComponent<RouteComponent>();
        if (rc == null) return;

        var move = new MoveToAction
        {
            waypointPosition = waypointPosition,
            waitIfActionAreNotComplete = true
        };
        rc.routeActions.Add(move);
        UpdateRouteViewFor(unit);
    }

    public void AddWaypointToObjectRoute(Unit unit, GameObject targetGO)
    {
        var rc = unit.GetComponent<RouteComponent>();
        if (rc == null) return;

        var move = new MoveToAction
        {
            waypointPosition = targetGO.transform.position,
            waypointObject = targetGO,
            waitIfActionAreNotComplete = true
        };
        rc.routeActions.Add(move);
        UpdateRouteViewFor(unit);
    }

    public void PickUpDropObjectSelected(Inventory pickupFromInventory)
    {
        //Set target inventory for pickup/drop action
        selectedTargetInventory = pickupFromInventory;

        //Change the text in the UI to show the name of the target unit
        Unit target_Unit = pickupFromInventory.GetComponent<Unit>();
        if (target_Unit == null)
        {
            Debug.LogWarning("Pickup inventory does not have a Unit component. Cannot set ObjectText.");
        }
        else
        {
            ObjectText.text = $"PickUp/Drop to: {target_Unit.name}";
        }

        pickupDropPanel.SetActive(true);
    }

    public void AddPickUpItem()
    {
        var rc = SelectionService.Instance.SelectedUnit.GetComponent<RouteComponent>();
        if (rc == null) return;

        var newPick = new PickupAction
        {
            pickupInventory = selectedTargetInventory,
            itemDefinition = allItemsList[itemDropDown.value],
            itemQuantity = itemQuantityInputField == null ? 1 : int.Parse(itemQuantityInputField.text),
            allAvalibuleItems = allAvalibuleItemsToggle.isOn,
            waitIfActionAreNotComplete = waitingForItems.isOn
        };

        rc.routeActions.Add(newPick);
        UpdateRouteViewFor(SelectionService.Instance.SelectedUnit);
    }

    public void AddDropItem()
    {
        var rc = SelectionService.Instance.SelectedUnit.GetComponent<RouteComponent>();
        if (rc == null) return;

        var newDrop = new DropAction
        {
            dropOfInventory = selectedTargetInventory,
            itemDefinition = allItemsList[itemDropDown.value],
            itemQuantity = itemQuantityInputField == null ? 1 : int.Parse(itemQuantityInputField.text),
            allAvalibuleItems = allAvalibuleItemsToggle.isOn,
            waitIfActionAreNotComplete = waitingForItems.isOn
        };

        rc.routeActions.Add(newDrop);
        UpdateRouteViewFor(SelectionService.Instance.SelectedUnit);
    }
    public enum AddRouteType
    {
        Waypoint,
        PickUp,
        Drop
    }
    public void OnNoWatingForItemToggleChanged()
    {
        waitingForItems.isOn = !noWaitingForItems.isOn;
    }
    public void OnWatingForItemToggleChanged()
    {
        noWaitingForItems.isOn = !waitingForItems.isOn;
    }
}

/*
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static RouteComponent;
using static UnityEditor.Progress;

public class RouteManager : MonoBehaviour
{
    public static RouteManager Instance;

    public GameObject routeScrollViewContentGO;
    public GameObject routeSlotPrefab;
    public TMP_Dropdown itemDropDown;
    public TMP_InputField itemQuantityInputField;
    public Toggle allAvalibuleItemsToggle;
    public Toggle noWaitingForItems;
    public Toggle waitingForItems;
    public RouteComponent routeComponent;
    public List<ItemDefinition> allItemsList = new List<ItemDefinition>();
    public bool isRouteRuning;

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

    public IEnumerator ExecuteAllRoutes()
    {
        Debug.Log("ExecuteAllRoutes START");
        GameStateMachine.Instance.SetState(GameplayStateId.BlockPlayerInput);

        List<Unit> unitsWithRoutes = GetUnitsWithRoutes();

        foreach (var unit in unitsWithRoutes)
        {
            Debug.Log("Executing route for " + unit.unitName);

            // Move camera to this unit
            yield return CameraManager.Instance.MoveCameraTo(unit.transform.position, 1, false);

            // Execute this unit's route
            yield return unit.routeComponent.ExecuteRoute();
        }

        GameStateMachine.Instance.SetState(GameplayStateId.Selecting);
        Debug.Log("Finished all routes");
    }

    List<Unit> GetUnitsWithRoutes()
    {
        List<Unit> result = new List<Unit>();

        foreach (var unit in GameManager.Instance.playerUnits)
        {
            if (unit.routeComponent == null)
            {
                Debug.LogWarning($"Unit {unit.unitName} does not have a RouteComponent.");
                continue;
            }
            else
            {
                if (unit.routeComponent.HasActiveRoute())
                {
                    result.Add(unit);
                }
            }
        }

        return result;
    }



    public void UpdateRouteView()
    {
        //Populate the route scroll view with the current route actions of the selected ship
        if (InputManager.Instance.selectedObject_Unit_Script == null)
        {
            Debug.LogError("No selected ship to show route for!");
            return;
        }

        //Delete all old route actions
        foreach (Transform child in routeScrollViewContentGO.transform)
        {
            Destroy(child.gameObject);
        }

        //Get route data
        routeComponent = InputManager.Instance.selectedObject.GetComponent<RouteComponent>();

        //instatiate ROUTESLOTS
        if (routeComponent != null)
        {
            foreach (var action in routeComponent.routeActions)
            {
                GameObject routeSlot = Instantiate(routeSlotPrefab, routeScrollViewContentGO.transform);

                RouteSlotUI slotUI = routeSlot.GetComponent<RouteSlotUI>();
                slotUI.Setup(action);
            }
        }
        else
        {
            Debug.LogError("Selected ship does not have a RouteComponent!");
        }

        //Load all items into the dropdown
        LoadAllItemsList();

        //Load Items to droppdownlist
        LoadAllItemsToDroppdown();
    }

    public void LoadAllItemsToDroppdown()
    {
        // Clear existing options
        itemDropDown.ClearOptions();

        List<string> options = new List<string>();

        foreach (var obj in allItemsList)
        {
            options.Add(obj.name); // Use object name as label
        }

        // Add to dropdown
        itemDropDown.AddOptions(options);
    }

    public void LoadAllItemsList()
    {
        // Clear the list first
        allItemsList.Clear();

        //Load all items from resourse folder
        ItemDefinition[] module_Items = Resources.LoadAll<ItemDefinition>("Modules");
        ItemDefinition[] ore_Items = Resources.LoadAll<ItemDefinition>("Ore");
        ItemDefinition[] ship_Items = Resources.LoadAll<ItemDefinition>("Ships");
        ItemDefinition[] unit_Items = Resources.LoadAll<ItemDefinition>("Units");
        ItemDefinition[] weapon_Items = Resources.LoadAll<ItemDefinition>("Weapons");

        // Add the loaded items to the allItemsList
        allItemsList.AddRange(module_Items);
        allItemsList.AddRange(ore_Items);
        allItemsList.AddRange(ship_Items);
        allItemsList.AddRange(unit_Items);
        allItemsList.AddRange(weapon_Items);
    }

    public void AddWaypoint_BT_pressed()
    {
        // Set state
        GameStateMachine.Instance.SetState(GameplayStateId.SetWaypoint);

        //Hide ui
        InfoScreenManager.Instance.HideInfoScreen();
    }

    public void AddWaypointToRoute(Vector3 waypointPosition)
    {
        // Get the selected ship's RouteComponent
        RouteComponent routeComponent = InputManager.Instance.selectedObject.GetComponent<RouteComponent>();
        if (routeComponent != null)
        {
            // Create a new waypoint action
            MoveToAction move = new MoveToAction
            {
                waypointPosition = waypointPosition
            };
            move.waypointObject = null; // No specific GameObject associated with this waypoint
            move.ownerUnit = InputManager.Instance.selectedObject_Unit_Script;
            move.waitIfActionAreNotComplete = true; //This so when ship do not reatch the waypoint in one turne the moveaction gets called retry the next turn.
            routeComponent.routeActions.Add(move);
        }

        UpdateRouteView();
    }
   
    public void AddWaypointToObjectRoute(GameObject targetGO)
    {
        // Get the selected ship's RouteComponent
        RouteComponent routeComponent = InputManager.Instance.selectedObject.GetComponent<RouteComponent>();
        if (routeComponent != null)
        {
            // Create a new waypoint action
            MoveToAction move = new MoveToAction
            {
                waypointPosition = targetGO.transform.position
            };
            move.waypointObject = targetGO;
            move.ownerUnit = InputManager.Instance.selectedObject_Unit_Script;
            move.waitIfActionAreNotComplete = true; //This so when ship do not reatch the waypoint in one turne the moveaction gets called retry the next turn.
            routeComponent.routeActions.Add(move);
        }

        UpdateRouteView();
    }

    public void AddPickUp_BT_pressed()
    {
        // Set state
        GameStateMachine.Instance.SetState(GameplayStateId.SetPickup);

        //Hide ui
        InfoScreenManager.Instance.HideInfoScreen();
    }

    public void AddPickUpItem(Inventory pickUpFromInventory)
    {
        //Create pickup8 action
        PickupAction newPickUpAction = new PickupAction();

        //add Item
        newPickUpAction.itemDefinition = allItemsList[itemDropDown.value];

        //add amount
        if(itemQuantityInputField == null)
            newPickUpAction.itemQuantity = 1;
        else
            newPickUpAction.itemQuantity = int.Parse(itemQuantityInputField.text);

        //add settings
        newPickUpAction.allAvalibuleItems = allAvalibuleItemsToggle.isOn;
        newPickUpAction.waitIfActionAreNotComplete = waitingForItems.isOn;
        newPickUpAction.pickupInventory = pickUpFromInventory;
        newPickUpAction.ownerUnit = InputManager.Instance.selectedObject_Unit_Script;

        //add To route list
        routeComponent.routeActions.Add(newPickUpAction);

        //Update UI
        UpdateRouteView();
    }

    public void AddDropItem()
    {
        DropAction newDropAction = new DropAction();
        newDropAction.itemDefinition = allItemsList[itemDropDown.value];
        if (itemQuantityInputField == null)
            newDropAction.itemQuantity = 1;
        else
            newDropAction.itemQuantity = int.Parse(itemQuantityInputField.text);
        newDropAction.allAvalibuleItems = allAvalibuleItemsToggle.isOn;
        newDropAction.waitForAvalibuleItems = waitingForItems.isOn;
        routeComponent.routeActions.Add(newDropAction);

        UpdateRouteView();
    }

    public void OnNoWatingForItemToggleChanged()
    {
        waitingForItems.isOn = !noWaitingForItems.isOn;
    }
    public void OnWatingForItemToggleChanged()
    {
        noWaitingForItems.isOn = !waitingForItems.isOn;
    }
}*/