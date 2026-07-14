using NUnit.Framework.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItemUI : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    public Image itemIconImage;
    public TMP_Text itemName;
    public ItemInstance moduleInSlot;

    private CanvasGroup canvasGroup;
    private Transform originalParent;

    void Awake()
    {
        canvasGroup = transform.root.GetComponent<CanvasGroup>();
    }
    
    public void SetupSlot(ItemInstance itemInstance)
    {
        if (itemInstance == null)
        {
            Debug.LogWarning("SetupSlot wass called but without item");
            return;
        }

        //Set up icon
        itemIconImage.sprite = ShipClassesManager.Instance.GetHullIcon(itemInstance.shipType);

        //Setup name
        itemName.text = itemInstance.ItemDefinition.itemName + " (" + itemInstance.shipType + ")" ;

        //Save item for When draged
        moduleInSlot = itemInstance;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;

        // Move to top canvas so it renders above everything
        transform.SetParent(canvasGroup.transform);
        canvasGroup.blocksRaycasts = false;
        this.GetComponent<CanvasGroup>().blocksRaycasts = false;

        DragContext.CurrentItem = moduleInSlot;

        //Debug.Log($"DragContenxt.currentItem: {this}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        this.GetComponent<CanvasGroup>().blocksRaycasts = true;
        DragContext.CurrentItem = null;

        // If not dropped on a valid slot
        transform.SetParent(originalParent);
        transform.localPosition = Vector3.zero;

        //Debug.Log("OnEndDrag called");
    }
}

