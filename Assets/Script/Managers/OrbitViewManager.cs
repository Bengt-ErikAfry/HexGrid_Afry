using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OrbitViewManager : MonoBehaviour
{
    public GameObject OrbitUIView;
    public Transform unitInOrbitListContent;
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

    public void ShowOrbitUIView()
    {
        OrbitUIView.SetActive(true);
        UpdateUnitInOrbitView();
    }

    public void UpdateUnitInOrbitView()
    {
        //Clear Lsit
        foreach (Transform child in OrbitViewManager.Instance.unitInOrbitListContent)
        {
            Destroy(child.gameObject);
        }

        // Add every unit in orbit with the normal unit entry prefab.
        foreach (var unit in unitsInOrbit.Where(u => u != null && u.gameObject != null))
        {
            if (OrbitalUnitentery_Prefab == null)
            {
                Debug.LogError("OrbitViewManager: OrbitalUnitentery_Prefab is not assigned.");
                return;
            }

            GameObject entryObject = Instantiate(OrbitalUnitentery_Prefab, unitInOrbitListContent);
            OrbitingUnitPrefab_Entery entry = entryObject.GetComponent<OrbitingUnitPrefab_Entery>();

            if (entry != null)
                entry.SetUnit(unit);
            else
                Debug.LogError("OrbitingUnitPrefab_Entery component not found on the unit entry prefab.");
        }
    }
}
