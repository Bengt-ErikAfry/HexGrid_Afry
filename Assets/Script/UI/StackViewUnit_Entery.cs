using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StackViewUnit_Entery : MonoBehaviour
{
    public Image unitSprite;
    public TMP_Text unitName;

    public Unit unit_script;

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
            //Select unit for other purposes
            Debug.Log("Selected " + unit_script.unitName);
            //Select unit
            //InputManager.Instance.selectedObject = unit_script.gameObject;
            //InputManager.Instance.selectedObject_Unit_Script = unit_script;
            SelectionService.Instance.SetSelectedUnit(unit_script);

            // Highlight selected tile (keep your current behavior)
            HexHighlighter.Instance.HighlightHexUnderScreenPosition(Camera.main.WorldToScreenPoint(SelectionService.Instance.SelectedUnit.transform.position));

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
                UIManager.Instance.selectedUnitImage.sprite = SelectionService.Instance.SelectedUnit.unitSprite;
                UIManager.Instance.selectedUnitNameText.text = SelectionService.Instance.SelectedUnit.unitName;
            }
            else
            {
                UIManager.Instance.selectedUnitView.SetActive(false);
            }

            //Show asteroidview if asteroid selected
            if (SelectionService.Instance.SelectedUnit.unitType == Unit.UnitType.Asteroid)
            {
                /*   --- remove/redo when new mining system
                UIManager.Instance.ShowAsteroidView();
                AsteroidManager.Instance.ShowAsteroidView();
                */
            }
            else
            {
                //Show selected unit view
                if (SelectionService.Instance.SelectedUnit != null) UIManager.Instance.ShowSelectedUnitView(SelectionService.Instance.SelectedUnit);
            }
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
