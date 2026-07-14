using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;
#endif

public class ActionButtonsManager : MonoBehaviour
{
    public static ActionButtonsManager Instance { get; private set; }

    public GameObject actionPanel;

    public List<GameObject> ActionButtonPool = new List<GameObject>();

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
    /*
    public void UpdateActionButtons(Unit unit_script)
    {
        HideAllButtons();
        actionPanel.SetActive(true);
        if (unit_script.unitType == Unit.UnitType.Astroid)
        {
            /*GameObject InfoBt = InstatiateActionBT("Info");

            InfoBt.GetComponent<Button>().onClick.AddListener(() =>
            {
                //SelectedObjectView.Instance.UpdateView(selectedObjectUnitScript);
                sateliteSurvayPanel.SetActive(true);
                CenterUIelement(sateliteSurvayPanel);
            });*/
     /*   }
        else if (unit_script.unitType == Unit.UnitType.Ship)
        {
            GetPoolButton(0, "Set Target");
            ActionButtonPool[0].GetComponent<Button>().onClick.AddListener(() =>
            {
                //InputManager.Instance.OnPlanPathButtonPressed();
            });
            GetPoolButton(1, "Info");
            ActionButtonPool[1].GetComponent<Button>().onClick.AddListener(() =>
            {
                InfoScreenManager.Instance.ShowInfoScreen();
            });
            GetPoolButton(2, "Attack"); 
            ActionButtonPool[1].GetComponent<Button>().onClick.AddListener(() =>
            {
                AttackManager.Instance.SelectTargetToAttack();
            });
            /*
            GameObject InfoBt = InstatiateActionBT("Info");
            InfoBt.GetComponent<Button>().onClick.AddListener(() =>
            {
                SelectedObjectView.Instance.UpdateView(selectedObjectUnitScript);
                ShipInfoPanelManager.Instance.SlideOut();
            });
        }
        else if (selectedObjectUnitScript.unitType == Unit.UnitType.cargoHualer)
        {
            GameObject MoveBt = InstatiateActionBT("Move");
            MoveBt.GetComponent<Button>().onClick.AddListener(() =>
            {
                SelectedObjectView.Instance.UpdateView(selectedObjectUnitScript);
                MoveUnitMode();
            });

            GameObject InfoBt = InstatiateActionBT("Info");
            InfoBt.GetComponent<Button>().onClick.AddListener(() =>
            {
                SelectedObjectView.Instance.UpdateView(selectedObjectUnitScript);
                cargoHualerPanelManager.Instance.ShowCargoHualerMenue();
            });

            CargoHualer cargoHualerScript = selectedObject.GetComponent<CargoHualer>();
            GameObject StartRoutBt = InstatiateActionBT("Start Route");
            Button startRouteButton = StartRoutBt.GetComponent<Button>();
            startRouteButton.interactable = false;
            startRouteButton.onClick.AddListener(() =>
            {
                cargoHualerScript.StartRouteBT();
            });
            GameObject StopRoutBt = InstatiateActionBT("Stop Route");
            Button stopRouteButton = StopRoutBt.GetComponent<Button>();
            stopRouteButton.interactable = false;
            stopRouteButton.onClick.AddListener(() =>
            {
                cargoHualerScript.StopRoute();
            });
            if (cargoHualerScript.routeActions.Count > 0)
            {
                startRouteButton.interactable = true;
                stopRouteButton.interactable = true;
            }
        }
        else if (selectedObjectUnitScript.unitType == Unit.UnitType.ScoutBeacon)
        {
            Probe probe_Script = selectedObject.GetComponent<Probe>();

            if (!probe_Script.deployd)
            {
                GameObject MoveBt = InstatiateActionBT("Move");
                MoveBt.GetComponent<Button>().onClick.AddListener(() =>
                {
                    SelectedObjectView.Instance.UpdateView(selectedObjectUnitScript);
                    MoveUnitMode();
                });
            }


            string buttonText = "";
            if (probe_Script.deployd)
            {
                buttonText = "UnDeploy";
            }
            else
            {
                buttonText = "Deploy";
            }
            GameObject DeployBt = InstatiateActionBT(buttonText);
            DeployBt.GetComponent<Button>().onClick.AddListener(() =>
            {
                selectedObject.GetComponent<Probe>().ToggleDeploy();
                UpdateActionButtons();
            });
        }
        else
        {
            Debug.Log("No action buttons for this unit type.");
            actionPanel.SetActive(false);
        }
    }
     */
    public void GetPoolButton(int posnr, string buttonText)
    {
        /*
         * GameObject newButton = Instantiate(actionBTprefab, actionPanel.transform);

        // Set button position (optional)
        RectTransform rt = newButton.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0, 0);
        */

        ActionButtonPool[posnr].gameObject.SetActive(true);
        ActionButtonPool[posnr].transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = buttonText;
        ActionButtonPool[posnr].transform.SetParent(actionPanel.transform, false);
    }

    public void HideAllButtons()
    {
        foreach (Transform child in actionPanel.transform)
        {
            child.gameObject.SetActive(false);
        }
        actionPanel.SetActive(false);
    }
}
