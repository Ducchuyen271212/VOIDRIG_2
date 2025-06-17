//WeaponManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponManager : MonoBehaviour
{
    public static WeaponManager Instance { get; private set; }

    [Header("Weapon Prefabs - Add as many as you want")]
    public List<GameObject> weaponPrefabs = new List<GameObject>();
    public GameObject player;

    [Header("Centralized Data")]
    public GameObject gunDataHolder; // Reference to the GameObject with GunData script

    private List<GameObject> instantiatedWeapons = new List<GameObject>();
    private int currentWeaponIndex = 0;
    private bool isSwitching = false;

    private PlayerInput playerInput;
    private InputAction switchWeaponAction;

    private void Awake()
    {
        playerInput = GetComponentInParent<PlayerInput>();
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Find GunDataHolder if not assigned
        if (gunDataHolder == null)
        {
            gunDataHolder = GameObject.Find("GunDataHolder");
            if (gunDataHolder == null)
            {
                Debug.LogError("WeaponManager: No GunDataHolder found! Please assign one or add it to the scene.");
                return;
            }
        }

        // Validate that the gunDataHolder has a GunData component
        if (gunDataHolder.GetComponent<GunData>() == null)
        {
            Debug.LogError("WeaponManager: GunDataHolder GameObject doesn't have a GunData component!");
            return;
        }

        // Validate we have weapons
        if (weaponPrefabs.Count == 0)
        {
            Debug.LogError("WeaponManager: No weapon prefabs assigned!");
            return;
        }

        // Instantiate all weapons
        foreach (GameObject weaponPrefab in weaponPrefabs)
        {
            if (weaponPrefab != null)
            {
                GameObject weapon = Instantiate(weaponPrefab, transform.position, Quaternion.identity);
                weapon.transform.parent = player.transform;
                weapon.transform.position = transform.position;
                weapon.SetActive(false); // Start all disabled

                // IMPORTANT: Set the centralized gun data holder for each weapon
                Weapon weaponScript = weapon.GetComponent<Weapon>();
                if (weaponScript != null)
                {
                    weaponScript.SetGunDataHolder(gunDataHolder);
                }

                instantiatedWeapons.Add(weapon);
                Debug.Log($"Instantiated weapon: {weapon.name} with centralized data");
            }
        }
    }

    private void Start()
    {
        switchWeaponAction = playerInput.actions["SwitchWeapon"];

        // IMPORTANT: Start with first weapon active
        if (instantiatedWeapons.Count > 0)
        {
            instantiatedWeapons[0].SetActive(true);
            currentWeaponIndex = 0;
            Debug.Log($"Started with weapon: {instantiatedWeapons[0].name}");
        }
    }

    private void Update()
    {
        if (switchWeaponAction.WasPressedThisFrame() && !isSwitching)
        {
            Debug.Log("Switching Weapon");
            SwitchToNextWeapon();
        }
    }

    private void SwitchToNextWeapon()
    {
        if (instantiatedWeapons.Count <= 1) return;

        isSwitching = true;

        // Deactivate current weapon
        instantiatedWeapons[currentWeaponIndex].SetActive(false);
        Debug.Log($"Deactivated: {instantiatedWeapons[currentWeaponIndex].name}");

        // Move to next weapon (cycle back to 0 if at end)
        currentWeaponIndex = (currentWeaponIndex + 1) % instantiatedWeapons.Count;

        // Activate new weapon
        instantiatedWeapons[currentWeaponIndex].SetActive(true);
        Debug.Log($"Switched to: {instantiatedWeapons[currentWeaponIndex].name}");

        isSwitching = false;
    }

    // Utility methods
    public GameObject GetCurrentWeapon()
    {
        if (currentWeaponIndex >= 0 && currentWeaponIndex < instantiatedWeapons.Count)
            return instantiatedWeapons[currentWeaponIndex];
        return null;
    }

    public int GetCurrentWeaponIndex() => currentWeaponIndex;
    public int GetTotalWeaponCount() => instantiatedWeapons.Count;
}