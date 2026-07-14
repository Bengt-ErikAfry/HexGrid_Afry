using JetBrains.Annotations;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using static UnityEditor.Progress;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;
#endif

public class InfoScreenManager : MonoBehaviour
{
    public static InfoScreenManager Instance { get; private set; }

    public RectTransform infoScreenRectTransform;
    public Vector2 infoScreenStartPos;
    public Unit unit_script;

    [Header("Info screen start values")]
    public TMP_Text unitName;
    public TMP_Text unitMovment;
    public TMP_Text unitMovedThisTurn;
    public TMP_Text unitDetectionRange;
    public TMP_Text unitEnergyConsumtion;
    public TMP_Text unitEnergyProduction;
    public TMP_Text unitEnergyStorage;

    [Header("Modules area")]
    public RectTransform ModuleView_GameObject;
    public RectTransform ModuleViewMessageBox;

    [Header("Storage")]
    public RectTransform storageView_GameObject;
    public InventoryUI storageInventoryUI_scipt;

    [Header("Factory")]
    public RectTransform factoryView_GameObject;
    public InventoryFactoryUI factoryInventoryUI_scipt;

    [Header("Hanger")]
    public RectTransform hangerView_Recttransform;

    [Header("Route")]
    public RectTransform routeView_Recttransform;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        infoScreenStartPos = infoScreenRectTransform.transform.localPosition;
    }

    public void UpdateInfoScreen_Start()
    {         
        if (unit_script != null)
        {
            unitName.text = $"Name: {unit_script.unitName}";
            unitMovment.text = $"Movement: {unit_script.shipRuntimeData.currentMovmentRange}";
            unitMovedThisTurn.text = $"Moved this turn: {unit_script.movedThisTurn}";
            unitDetectionRange.text = $"Detection Range: {unit_script.detectionRange}";



            unitEnergyConsumtion.text = $"Energy Consumption: {unit_script.shipRuntimeData.energyConsumptionThisTurn}/turn";
            unitEnergyProduction.text = $"Energy Production: +{unit_script.shipRuntimeData.currentEnergyProduction}/turn";
            unitEnergyStorage.text = $"Energy Storage (cur/max): {unit_script.shipRuntimeData.energyStorage}/{unit_script.shipRuntimeData.currentEnergyMaxStorage}";
        }
    }

    public void ShowBaseInfo()
    {
        UpdateInfoScreen_Start();
        HideAll();
    }

    public void ShowModules()
    {
        //Hide all others
        HideAll();

        //Show module view
        ModuleView_GameObject.offsetMax = new Vector2(0, 0);
        ModuleView_GameObject.offsetMin = new Vector2(0, 0);
        ModuleView_GameObject.gameObject.SetActive(true);

        //Check if player are aming or modifying modules.
        if (GameStateMachine.Instance.CurrentId == GameplayStateId.Selecting)
        {
            ModuleViewManager.Instance.modifyModuleView.SetActive(true);
            ModuleViewManager.Instance.attackView.SetActive(false);

            unit_script= SelectionService.Instance.SelectedUnit;

            ModuleViewManager.Instance.ShowForUnit(SelectionService.Instance.SelectedUnit);
        }
        else
        {
            ModuleViewManager.Instance.modifyModuleView.SetActive(false);
            ModuleViewManager.Instance.attackView.SetActive(true);

            unit_script = SelectionService.Instance.SelectedUnit.target_Unit_Script;

            ModuleViewManager.Instance.ShowForUnit(SelectionService.Instance.SelectedUnit.target_Unit_Script);
        }
    }

    public void CloseModuleView()   //The cansel button on the module view
    {
        if(unit_script.isPlayerControlled)
        {
            MessageBoxManager.Instance.ShowChoice("Changes will take affect next trune. Do you want that?", onYes, onNo);
        }
        else
        {
            HideInfoScreen();
        }
    }
    public void onYes()
    {
        Debug.Log("Yes button clicked. Changes will take effect next turn.");
        ModuleViewMessageBox.gameObject.SetActive(false);
        HideAll();
    }
    public void onNo()
    {
        ResetModuleValues();
        UpdateInfoScreen_Start();
        ModuleViewMessageBox.gameObject.SetActive(false);
        HideAll();
    }

    public void ResetModuleValues()     //Resset button on the messagebox
    {
        foreach (var module in SelectionService.Instance.SelectedUnit.moduleRuntimeList)
        {
            module.nextTurnDamage = module.currentDamage;
            module.nextTurnRange = module.currentRange;
        }
    }

    public void ShowStorage()
    {
        HideAll();

        storageView_GameObject.offsetMax = new Vector2(0, 0);
        storageView_GameObject.offsetMin = new Vector2(0, 0);
        storageView_GameObject.gameObject.SetActive(true);
        if(unit_script.unitInventory == null) { Debug.LogError("Unit inventory is null!"); return;}
        storageInventoryUI_scipt.RefreshUI(unit_script.unitInventory);
    }

    public void ShowFactory()
    {
        HideAll();

        factoryView_GameObject.offsetMax = new Vector2(0, 0);
        factoryView_GameObject.offsetMin = new Vector2(0, 0);
        factoryView_GameObject.gameObject.SetActive(true);
        if (unit_script.unitInventory == null) { Debug.LogError("Unit inventory is null!"); return; }
        factoryInventoryUI_scipt.RefreshUI(GameManager.Instance.playerBluePrints ,unit_script.unitInventory);
    }

    public void ShowHanger()
    {
        //Hide all others
        HideAll();

        //Show View
        hangerView_Recttransform.offsetMax = new Vector2(0, 0);
        hangerView_Recttransform.offsetMin = new Vector2(0, 0);
        hangerView_Recttransform.gameObject.SetActive(true);

        //Show hanger.
        HangerManager.Instance.ShowHangerView();
    }

    public void ShowRoute()
    {
        //Hide all others
        HideAll();

        //Show View
        routeView_Recttransform.offsetMax = new Vector2(0, 0);
        routeView_Recttransform.offsetMin = new Vector2(0, 0);
        routeView_Recttransform.gameObject.SetActive(true);

        //Init View
        RouteManager.Instance.UpdateRouteViewFor(SelectionService.Instance.SelectedUnit);
    }

    //Called from UI button INFO
    public void ShowInfoScreen()
    {
        if (SelectionService.Instance.SelectedUnit == null) return;

        if (SelectionService.Instance.SelectedUnit.minableComponent == null)
        { 
            infoScreenRectTransform.localPosition = Vector2.zero;
            unit_script = SelectionService.Instance.SelectedUnit;
            UpdateInfoScreen_Start();

            //Hide Selected unit view
            UIManager.Instance.HideSelectedUnitView();
        }
        else
        {
            //Minable Object
            MiningUIManager.Instance.ShowMinable(SelectionService.Instance.SelectedUnit.minableComponent);
        }
    }

    public void HideInfoScreen()
    {
        infoScreenRectTransform.localPosition = infoScreenStartPos;
        HideAll();
    }

    public void CloseInfocressnButton_Pressed()
    {
        infoScreenRectTransform.localPosition = infoScreenStartPos;
        HideAll();
        if (GameStateMachine.Instance.CurrentId == GameplayStateId.SetWaypoint || GameStateMachine.Instance.CurrentId == GameplayStateId.SetPickup)
        {
            GameStateMachine.Instance.SetState(GameplayStateId.Selecting);
        }

        UIManager.Instance.ActivateSelectedUnit();
    }

    public void HideAll()
    {
        ModuleViewMessageBox.gameObject.SetActive(false);
        ModuleView_GameObject.gameObject.SetActive(false);
        storageView_GameObject.gameObject.SetActive(false);
        factoryView_GameObject.gameObject.SetActive(false);
        hangerView_Recttransform.gameObject.SetActive(false);
        routeView_Recttransform.gameObject.SetActive(false);
    }
}
