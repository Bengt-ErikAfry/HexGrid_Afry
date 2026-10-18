using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;
using System;


public class AsteroidManager : MonoBehaviour
{
    public RectTransform asteroidViewRectTransform;
    public TMP_Text satelitesInOrbit;
    public TMP_Text satelitesSurvayingAsteroid;
    public TMP_Text progresText;
    public Button addSatelitesToSurvayBtn;

    private int scanProgress;

    public static AsteroidManager Instance { get; private set; }

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

    public void ShowAsteroidView(AsteroidComponent asteroid)
    {
        //Move panel
        asteroidViewRectTransform.localPosition = Vector2.zero;

        //UpdateText
        satelitesInOrbit.text = $"Nr of Satelites in orbit {GetAmountOfSatelitesInOrbit(asteroid)}";
        satelitesSurvayingAsteroid.text = $"Nr Satelites Survaying Asteroid: {asteroid.nrOfSatelitesSurvaying}";

        int progressProcent = Math.Round((asteroid.turneItTakesToSurvay / asteroid.nrOfTurnesSuveyed) * 100, 2);

    progresText.text = $"Scan progress: {(Math.Round((double)asteroid.turneItTakesToSurvay / asteroid.nrOfTurnesSuveyed, 2)) * 100}% ({asteroid.turneItTakesToSurvay-asteroid.nrOfTurnesSuveyed} turnes left)";

        //Button not active if no satelites in orbit.
        addSatelitesToSurvayBtn.interactable = GetAmountOfSatelitesInOrbit(asteroid) > 0;
    }

    public int GetAmountOfSatelitesInOrbit(AsteroidComponent asteroid)
    {
        //Get Asteroid tile Axial.
        Vector2Int asteroidTileAxial = TileManager.Instance.WorldToAxial(asteroid.transform.position);

        //Create a list to collect satelites in orbit.
        List<Unit> AllSatelitesInOrbit = new List<Unit> { };

        //Compare with all units.
        foreach (var unitInUnitList in GameManager.Instance.unitsList)
        {
            if(TileManager.Instance.WorldToAxial(unitInUnitList.transform.position) == asteroidTileAxial &&
               unitInUnitList.shipType == ShipType.Stelite)
            {
                //Same tile and a satelite
                foreach (var module in unitInUnitList.moduleRuntimeList)
                {
                    if(module.ItemDefinition.moduleType == ModuleType.SurvayArray &&
                        module.isOnline &&
                        !module.isBroken &&
                        !module.isDestroyd)
                    {
                        //Have a survay module that is online and not broken.
                        AllSatelitesInOrbit.Add(unitInUnitList);
                    }    
                    else
                    {
                        Debug.Log($"{unitInUnitList.unitName} missing a survayArray module or isOnline={module.isOnline} or isBroken={module.isBroken} or isDestroyd{module.isDestroyd}.");
                    }
                }
            }
            else
            {
                Debug.Log($"{unitInUnitList.unitName} are not in same tile or not a satelite");
            }
        }

        return AllSatelitesInOrbit.Count;
    }
}
