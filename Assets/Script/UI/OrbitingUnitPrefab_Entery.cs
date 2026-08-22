using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OrbitingUnitPrefab_Entery : MonoBehaviour
{
    public Unit unit_Script;
    public Image icon;
    public TMP_Text nametext;

    private Outline outline;

    private void Start()
    {
        outline = GetComponent<Outline>();
    }

    public void SetUnit(Unit unit)
    {
        unit_Script = unit;
        if (unit != null)
        {
            nametext.text = unit.unitName;
            icon.sprite = unit.unitSprite;
        }
    }

    public void OnClick()
    {
        if (unit_Script != null)
        {
            Debug.Log($"OrbitingUnitPrefab_Entery: {unit_Script.unitName} is clicked.");

            //Move game object to the minabelobject view.
            MiningUIManager.Instance.selectedOrbitalUnit = unit_Script;

            GameStateMachine.Instance.SetState(GameplayStateId.PlaceOrbitalUnit);
            //unit_Script.gameObject.transform.parent = MiningUIManager.Instance.tileParent.root;

            // select unit via selection service
            SelectionService.Instance.SetSelectedUnit(unit_Script);

            //Hide buttons
            UIManager.Instance.HideAllUI();

            //Show turnebuttons
            UIManager.Instance.ShowTruenButtons();
        }
    }
}
