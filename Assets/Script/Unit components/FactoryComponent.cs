using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class FactoryComponent : MonoBehaviour
{
    public float completePercentage;
    public ItemInstance itemToProduce;
    public int currentNrTurnsProduced;
    private Unit unitScript;

    // Track whether we've subscribed so we don't subscribe/unsubscribe incorrectly.
    private bool isSubscribed = false;

    void Start()
    {
        TrySubscribe();
        unitScript = GetComponent<Unit>();
    }

    void OnEnable()
    {
        // If component is re-enabled at runtime after Start, ensure subscription exists.
        if (GameManager.Instance != null && !isSubscribed)
        {
            GameManager.Instance.OnNewTurn += FactoryProduceOneTurn;
            isSubscribed = true;
        }
    }

    void OnDisable()
    {
        if (isSubscribed && GameManager.Instance != null)
        {
            GameManager.Instance.OnNewTurn -= FactoryProduceOneTurn;
            isSubscribed = false;
        }
    }

    void TrySubscribe()
    {
        // Start() runs after all Awake() calls, so GameManager.Instance should be initialized.
        if (GameManager.Instance != null && !isSubscribed)
        {
            GameManager.Instance.OnNewTurn += FactoryProduceOneTurn;
            isSubscribed = true;
        }
    }

    public void StartProduction(ItemInstance itemToStartProducing)    //When player click "
    {
        itemToProduce = itemToStartProducing;
        currentNrTurnsProduced = 0;
        completePercentage = 0;
    }

    void FactoryProduceOneTurn()
    {
        if (itemToProduce.ItemDefinition == null)
        {
            Debug.LogWarning($"Factory {gameObject.name} has no item to produce.");
            return;
        }

        currentNrTurnsProduced++;

        // Avoid division by zero and compute correct fraction
        var requiredTurns = Mathf.Max(1, itemToProduce.ItemDefinition.craftingTime); // assume craftingTime is int/float
        float progressFraction = (float)currentNrTurnsProduced / requiredTurns;
        completePercentage = Mathf.Clamp01(progressFraction); // 0..1

        Debug.Log($"Factory {this.gameObject.name} produce +1 turn {completePercentage}");

        if (completePercentage >= 1f)
        {
            // production finished: handle completion (spawn item, notify manager, stop producing, etc.)

            string mainMessage =
                "PRODUCTION:" + unitScript.unitName + " have produced " + itemToProduce.ItemDefinition.itemName;
            string subMessage =
                "";
            MessageSystemManager.Instance.CreateMessage(mainMessage, subMessage, this.transform.position, Color.white);

            //Add Item to inventory
            if (unitScript == null) { Debug.LogError("Unit script is null!"); return; }
            if (unitScript.unitInventory == null) { Debug.LogError("Unitscript inventory is null!"); return; }
            if (itemToProduce == null) { Debug.LogError("itemToProduce is null!"); return; }
            unitScript.unitInventory.AddItem(itemToProduce, 1);

            itemToProduce = null;
            completePercentage = 0;
            currentNrTurnsProduced = 0;
        }
    }
}
