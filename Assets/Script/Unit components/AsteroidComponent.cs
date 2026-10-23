using NUnit.Framework.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Progress;
using Random = UnityEngine.Random;

public class AsteroidComponent : MonoBehaviour
{
    public string asteroidID;   //Used when saving insted of using a object reference.
    public string asteroidName; //For desplying in UI. Set in inspector.
    public Vector2Int currentTileCoord; //Set when the asteroid is spawned. Used to find the asteroid when loading a save file.
    public string asteroidTileSatusSave = "hex_asteridTemp.json"; //created when player first show the planet. Then stays the same.
    public string unitsOnSurfaceSave = "units_on_surface.json"; //created when player first show the planet. Then stays the same.
    public Sprite asteroidIcon; //For desplying in UI. Set in inspector.

    private void Awake()
    {
        //Wait an let the asteroid spawn before getting the tile coord. Otherwise it will be 0,0.
        StartCoroutine(waitOneFrame());
    }

    public void Start()
    {
        //Add to GameManager list.
        if(!GameManager.Instance.asteroidsList.Contains(this))
        GameManager.Instance.asteroidsList.Add(this);
    }

    private IEnumerator waitOneFrame()
    {
        yield return null; // Wait for one frame
        currentTileCoord = TileManager.Instance.WorldToAxial(this.transform.position);
        StopCoroutine(waitOneFrame());
    }

    public IEnumerator InitAsteroid()
    {
        // Wait until TileManager exists and has built its lookup
        yield return new WaitUntil(() => TileManager.Instance != null && TileManager.Instance.HasBuiltLookup);

        //Set cordinates for this unit. Used for pathfinding and other tile-based logic.
        currentTileCoord = TileManager.Instance.WorldToAxial(this.transform.position);

        //Set ship icon in selection view
        this.transform.GetChild(0).GetComponent<SpriteRenderer>().sprite = asteroidIcon;

        //GameObject name for debuging
        this.gameObject.name = asteroidName + " (" + asteroidID + ")";
    }
}