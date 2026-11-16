using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OrbitViewManager : MonoBehaviour
{
    public GameObject OrbitalInventortyEntery_Prefab;
    public GameObject OrbitalUnitentery_Prefab;
    public List<Unit> unitsInOrbit = new List<Unit>();

    public static OrbitViewManager Instance { get; private set; }

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

    public void UpdateUnitInOrbitView()
    {
        //Clear Lsit
        foreach (Transform child in AsteroidManager.Instance.unitInOrbitListContent)
        {
            Destroy(child.gameObject);
        }

        //Populate the list with units that are currently in orbit
        foreach (var unit in unitsInOrbit.Where(u => u != null && u.gameObject != null))
        {
          /*  var unitInOrbitListEnery = Instantiate(unitInOrbitPrefab, unitInOrbitListContent);
            OrbitingUnitPrefab_Entery entry = unitInOrbitListEnery.GetComponent<OrbitingUnitPrefab_Entery>();
            if (entry != null)
            {
                entry.SetUnit(unit);
            }
            else
            {
                Debug.LogError("OrbitingUnitPrefab_Entery component not found on the prefab.");
            }
          */
        }
    }
}
