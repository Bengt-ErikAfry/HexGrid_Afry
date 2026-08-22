using System;
using UnityEngine;

[Serializable]
public class TileData
{
    public int tileIndexRow;
    public int tileIndexCol;
    public bool isSurveyed = false;
    public bool hasMiningOutpost = false;
    public bool hasAbandonMiningOutpost = false;
    public float oreAmount = 0f;
    public bool isPlayerTurnOver = false;
    public GameObject fogGO;

    // Reintroduced: mark tile as blocked (editor / runtime can set)
    public bool isBlocked = false;

    [Header("AbandonMingingOutpost")]
    public int health_Defence = 10;
    public int health_CommandCenter = 10;
    public int health_LeftDoor = 10;
    public int health_RightDoor = 10;
    public int health_Storage = 10;
    public int health_Drill = 10;
    public int health_PowerCore = 10;

    public int maxHealth_Defence = 10;
    public int maxHealth_CommandCenter = 10;
    public int maxHealth_LeftDoor = 10;
    public int maxHealth_RightDoor = 10;
    public int maxHealth_Storage = 10;
    public int maxHealth_Drill = 10;
    public int maxHealth_PowerCore = 10;

    public int aliens_CommandCenter = 10;
    public int aliens_Defences = 5;
    public int aliens_LeftDoor = 5;
    public int aliens_RightDoor = 5;
    public int aliens_Drill = 8;
    public int aliens_PowerCore = 7;
    public int aliens_Storage = 6;

    public int chanseToHit_Defence = 15;
    public int chanseToHit_ComandCenter = 10;
    public int chanseToHit_LeftDoor = 10;
    public int chanseToHit_RightDoor = 10;
    public int chanseToHit_Storage = 5;
    public int chanseToHit_Drill = 3;
    public int chanseToHit_PowerCore = 2;
}