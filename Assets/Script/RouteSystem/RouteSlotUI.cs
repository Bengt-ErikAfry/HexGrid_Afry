using System;
using TMPro;
using UnityEngine;

public class RouteSlotUI : MonoBehaviour
{
    public TextMeshProUGUI label;
    public int index;

    public void Setup(RouteAction action, int index)
    {
        this.index = index;

        if (action is MoveToAction moveTo)
        {
            //Vector3 dest = moveTo.destination;
            //Debug.Log("Destination = " + dest);
            if (moveTo.waypointObject != null)
            {
                label.text = $"Waypoint: {moveTo.waypointObject.name}";
                return;
            }
            else
            {
                label.text = $"Waypoint: {HexGridLinesBaker.Instance.GetGridPosFromWorldPos(moveTo.waypointPosition)}";
            }
        }

        if(action is PickupAction pickUp)
        {
            label.text = $"Pick Up: {pickUp.itemDefinition.itemName}.";
            if (pickUp.allAvalibuleItems)
            {
                label.text += " All.";
            }
            else
            {
                label.text += $" x{pickUp.itemQuantity}";
            }
            if(pickUp.waitIfActionAreNotComplete)
            {
                label.text += " Wait";
            }
            else
            {
                label.text += " No wait";
            }
            return;
        }

        if(action is DropAction drop)
        {
            label.text = $"Drop Item: {drop.itemDefinition.itemName} x{drop.itemQuantity}";
            return;
        }
    }

    public void SlotSelected()
    {
        RouteManager.Instance.SelectRouteSlot(index);
    }
    public void MoveUp()
    {
        RouteManager.Instance.MoveSlotUp(index);
    }
    public void MoveDown()
    {
        RouteManager.Instance.MoveSlotDown(index);
    }
    public void Delete()
    {
        RouteManager.Instance.DeleteSlotRoute(index);
    }
}
