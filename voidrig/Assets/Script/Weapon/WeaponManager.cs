//WeaponManager.cs - Extended but Compatible Version
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponManager : MonoBehaviour
{
    public static WeaponManager Instance { get; private set; }

    [Header("Weapon Prefabs - Add as many as you want")]
    public List<GameObject> weaponPrefabs = new List<GameObject>();
    public GameObject player;

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
                instantiatedWeapons.Add(weapon);
                Debug.Log($"Instantiated weapon: {weapon.name}");
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