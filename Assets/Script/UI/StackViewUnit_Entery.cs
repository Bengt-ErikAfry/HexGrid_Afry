using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StackViewUnit_Entery : MonoBehaviour
{
    public Image unitSprite;
    public TMP_Text unitName;

    public Unit unit_script;
    public AsteroidComponent asteroid_script;

    public void OnClick()
    {
        //Hide stack view
        UIManager.Instance.stackViewRectTransform.gameObject.SetActive(false);

        if(GameStateMachine.Instance.CurrentId == GameplayStateId.Attacking)
        {            
            //Attack
            
            //Set target
            SelectionService.Instance.SelectedUnit.target_Unit_Script = unit_script;

            //Show Aming UI
            InfoScreenManager.Instance.ShowModules();

            //GameManager.Instance.PlayerTryToAttack(InputManager.Instance.selectedObject_Unit_Script, unit_script.gameObject);
        }
        else if(GameStateMachine.Instance.CurrentId == GameplayStateId.Selecting)
        {
            if(unit_script != null)
            {
                //Select unit
                SelectionService.Instance.SetSelectedUnit(unit_script);

                // Show Route if existing
                if (unit_script.routeComponent != null && unit_script.routeComponent.routeActions.Count > 0)
                {
                    HexGridManager.Instance.CalculateRoutePath(unit_script);
                }

                TileManager.Instance.HighlightTileWorldPosition(unit_script.transform.position);
                Debug.Log($"SelectState OnTap: single unit '{unit_script.unitName}' selected.");
            }
            else if (asteroid_script != null)
            {
                //Select asteroid
                // If there are multiple asteroids (unlikely) pick the first
                SelectionService.Instance.SetSelectedAsteroid(asteroid_script);
                TileManager.Instance.HighlightTileWorldPosition(asteroid_script.transform.position);
                Debug.Log($"SelectState OnTap: asteroid '{asteroid_script.asteroidJsonFileName}' selected.");
            }




            /*
            //Select unit for other purposes
            Debug.Log("Selected " + unit_script.unitName);
            //Select unit
            //InputManager.Instance.selectedObject = unit_script.gameObject;
            //InputManager.Instance.selectedObject_Unit_Script = unit_script;
            SelectionService.Instance.SetSelectedUnit(unit_script);

            // Highlight selected tile (keep your current behavior)
            TileManager.Instance.HighlightTileWorldPosition(SelectionService.Instance.SelectedUnit.transform.position);

            Debug.Log("after SelectState invoke" + GameStateMachine.Instance.Current);

            //Show/Hide UI buttons
            if (SelectionService.Instance.SelectedUnit.isPlayerControlled)
            {
                //Player unit selected

                UIManager.Instance.ResetUI();
                if (SelectionService.Instance.SelectedUnit.movedThisTurn >= SelectionService.Instance.SelectedUnit.shipRuntimeData.currentMovmentRange)
                {
                    UIManager.Instance.HideUnitMovmentControlls();
                }
                if (SelectionService.Instance.SelectedUnit.haveAttackedThisTurn)
                {
                    UIManager.Instance.HideUnitAttackControlls();
                }
            }
            else
            {
                //Enemy unit selected

                UIManager.Instance.HideUnitMovmentControlls();
                UIManager.Instance.HideUnitAttackControlls();
            }

            if (SelectionService.Instance.SelectedUnit != null)
            {
                UIManager.Instance.selectedUnitView.SetActive(true);
                UIManager.Instance.selectedUnitImage.sprite = ShipClassesManager.Instance.GetShipIcon(SelectionService.Instance.SelectedUnit.shipType);
                UIManager.Instance.selectedUnitNameText.text = SelectionService.Instance.SelectedUnit.unitName;
            }
            else
            {
                UIManager.Instance.selectedUnitView.SetActive(false);
            }*/
        }
        else if(GameStateMachine.Instance.CurrentId == GameplayStateId.SetWaypoint)
        {
            //Set waypoint for selected unit
            RouteManager.Instance.AddWaypointToRoute(SelectionService.Instance.SelectedUnit, unit_script.transform.position);
        }
        else if (GameStateMachine.Instance.CurrentId == GameplayStateId.SetPickup)
        {
            //Set pickup for selected unit
            RouteManager.Instance.PickUpDropObjectSelected(unit_script.unitInventory);
        }
        else if (GameStateMachine.Instance.CurrentId == GameplayStateId.SetDrop)
        {
            //Set drop for selected unit
            RouteManager.Instance.PickUpDropObjectSelected(unit_script.unitInventory);
        }
    }
}
