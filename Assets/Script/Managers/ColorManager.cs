using UnityEngine;

public class ColorManager : MonoBehaviour
{
    [Header("InfoScreen Module")]
    public Color moduleOnline;
    public Color moduleOffline;
    public Color moduleDestroyd;
    public Color moduleBroken;

    public Color routeSlotSelected;
    public Color routeSlotNotSelected;
    public Color hex_Select_Empty;

    public Color hex_AttackRange;
    public Color hex_MovementRange;
    public Color tile_UnExplored;
    public Color tile_Explored;


    public static ColorManager Instance { get; private set; }
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
}
