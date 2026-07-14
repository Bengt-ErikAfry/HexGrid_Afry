using System.Collections.Generic;
using UnityEngine;

// called from InputReader. When player taps/drags/pinches, the intrface are called. All classes inhereted from this 
public interface IGameState
{
    void Enter();
    void Exit();

    // Input hooks:
    void OnTap(Vector2 screenPos);
    void OnDrag(Vector2 delta);
    void OnPinch(float amount);

    // Optional tick:
    void Tick(float dt);
}

public enum GameplayStateId
{
    Selecting,    // Tap selects a unit/hex
    PlanningPath, // Tap adds waypoint / confirms move
    Attacking,    // Tap chooses target
    UIOnly,       // Temporary (dialogs/menus) block world actions
    EnemyTurn,     // Enemy AI is doing stuff, player input is ignore
    Cinematic,
    BlockPlayerInput, // For cutscenes or other moments where you want to freeze the player but still show the world
    SelectingAsteroid,
    Boarding,   //When attacking and boarding is possible, player can choose to board instead of attack. Tap selects target for boarding instead of attacking.
    SetWaypoint, // When player is adding waypoints to a unit, tap adds waypoint instead of selecting unit
    SetPickup,
    SetDrop,
    ShowMiningObjectUI
}


// This keeps track of the current gameplay state and routes input to it. It also handles state transitions and tick updates.

public class GameStateMachine : MonoBehaviour
{
    public static GameStateMachine Instance { get; private set; }

    private readonly Dictionary<GameplayStateId, IGameState> _states = new();
    public IGameState Current { get; private set; }
    public GameplayStateId CurrentId { get; private set; }

    [SerializeField] private GameplayStateId debugCurrentId;
    [SerializeField] private string debugCurrentState;


    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Register states
        
        _states[GameplayStateId.Selecting] = new SelectingState(this);
        _states[GameplayStateId.PlanningPath] = new PlanningPathState(this);
        _states[GameplayStateId.Cinematic] = new CinematicState(this);
        _states[GameplayStateId.EnemyTurn] = new EnemyState(this);
        _states[GameplayStateId.Attacking] = new AttackingState(this);
        _states[GameplayStateId.UIOnly] = new UIOnlyState(this);
        _states[GameplayStateId.BlockPlayerInput] = new BlockPlayerInputState(this);
        _states[GameplayStateId.Boarding] = new BoardingState(this);
        _states[GameplayStateId.SetWaypoint] = new SetWaypointState(this);
        _states[GameplayStateId.SetPickup] = new SetPickUpState(this);
        _states[GameplayStateId.SetDrop] = new SetDropState(this);
        _states[GameplayStateId.ShowMiningObjectUI] = new ShowMiningObjectUIState(this);

        //STARTING stage
        SetState(GameplayStateId.Selecting);
    }

    public void SetState(GameplayStateId next)
    {
        Debug.Log($"Transitioning from {_states[CurrentId]} to {next} state");
        if (Current != null) Current.Exit();
        CurrentId = next;
        Current = _states[next];
        Current.Enter();

        debugCurrentId = CurrentId;
        debugCurrentState = Current.GetType().Name;
    }

    void Update() => Current?.Tick(Time.deltaTime);
}
