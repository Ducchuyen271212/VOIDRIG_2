// WeaponManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponManager : MonoBehaviour
{
    public static WeaponManager Instance { get; private set; }

    [Header("Weapon Slots")]
    public List<GameObject> weaponSlots; // slot1 to slot5
    public GameObject activeWeaponSlot;

    [Header("References")]
    public GameObject player;

    private PlayerInput playerInput;
    private InputAction switchWeaponAction;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        playerInput = GetComponentInParent<PlayerInput>();
    }

    private void Start()
    {
        if (weaponSlots.Count > 0)
        {
            activeWeaponSlot = weaponSlots[0];
        }

        if (playerInput?.actions != null)
        {
            switchWeaponAction = playerInput.actions["SwitchWeapon"];
        }
    }

    private void Update()
    {
        foreach (GameObject slot in weaponSlots)
        {
            bool isActive = slot == activeWeaponSlot;
            slot.SetActive(isActive);

            if (slot.transform.childCount > 0)
            {
                var modularWeapon = slot.transform.GetChild(0).GetComponent<ModularWeapon>();
                if (modularWeapon != null)
                {
                    bool wasActive = modularWeapon.isActiveWeapon;
                    modularWeapon.isActiveWeapon = isActive;

                    // Debug logging for unexpected deactivation
                    if (wasActive && !isActive)
                    {
                        Debug.Log($"=== WEAPON DEACTIVATED BY WEAPONMANAGER === in slot {slot.name}");
                        Debug.Log($"Active slot: {activeWeaponSlot?.name}, Current slot: {slot.name}");
                    }
                }
            }
        }

        HandleInput();
    }

    private void HandleInput()
    {
        // Switch to next weapon using InputSystem
        if (switchWeaponAction?.WasPressedThisFrame() == true)
        {
            SwitchToNextSlot();
        }

        // Direct slot switching using InputSystem
        if (playerInput?.actions != null)
        {
            if (playerInput.actions["Slot1"]?.WasPressedThisFrame() == true) SwitchToSlot(0);
            if (playerInput.actions["Slot2"]?.WasPressedThisFrame() == true) SwitchToSlot(1);
            if (playerInput.actions["Slot3"]?.WasPressedThisFrame() == true) SwitchToSlot(2);
            if (playerInput.actions["Slot4"]?.WasPressedThisFrame() == true) SwitchToSlot(3);
            if (playerInput.actions["Slot5"]?.WasPressedThisFrame() == true) SwitchToSlot(4);
        }
    }

    private void SwitchToSlot(int index)
    {
        if (index >= 0 && index < weaponSlots.Count)
        {
            Debug.Log($"Switching to slot {index}");
            DeactivateAllWeapons();
            activeWeaponSlot = weaponSlots[index];
        }
    }

    private void SwitchToNextSlot()
    {
        if (weaponSlots.Count <= 1) return;

        int currentIndex = weaponSlots.IndexOf(activeWeaponSlot);
        int nextIndex = (currentIndex + 1) % weaponSlots.Count;
        Debug.Log($"Switching from slot {currentIndex} to slot {nextIndex}");
        DeactivateAllWeapons();
        activeWeaponSlot = weaponSlots[nextIndex];
    }

    private void DeactivateAllWeapons()
    {
        Debug.Log("DeactivateAllWeapons called");
        AimingManager.Instance?.ForceStopAiming();

        foreach (var slot in weaponSlots)
        {
            if (slot.transform.childCount > 0)
            {
                var mw = slot.transform.GetChild(0).GetComponent<ModularWeapon>();
                if (mw != null)
                {
                    Debug.Log($"Deactivating weapon in slot: {slot.name}");
                    mw.isActiveWeapon = false;
                }
            }
        }

        if (AmmoManager.Instance?.ammoDisplay != null)
        {
            AmmoManager.Instance.ammoDisplay.text = "-- / --";
        }
    }

    public void PickupWeapon(GameObject weaponObject)
    {
        if (activeWeaponSlot.transform.childCount > 0)
        {
            Debug.Log("Active slot already has a weapon");
            return;
        }

        ModularWeapon modularWeapon = weaponObject.GetComponent<ModularWeapon>();
        if (modularWeapon == null)
        {
            Debug.LogError("Object is not a ModularWeapon");
            return;
        }

        Debug.Log("Picking up weapon");

        // Disable physics
        Rigidbody rb = weaponObject.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // Parent to active slot
        weaponObject.transform.SetParent(activeWeaponSlot.transform, false);

        // Wait a frame for initialization to complete
        StartCoroutine(WeaponPickup(modularWeapon, weaponObject));
    }

    private IEnumerator WeaponPickup(ModularWeapon modularWeapon, GameObject weaponObject)
    {
        yield return null; // Wait one frame

        // Position weapon
        weaponObject.transform.localPosition = modularWeapon.spawnPosition;
        weaponObject.transform.localRotation = Quaternion.Euler(modularWeapon.spawnRotation);

        // Re-enable animator
        Animator animator = weaponObject.GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = true;
        }

        // Activate weapon - this should trigger OnEnable and SetupInputActions
        modularWeapon.isActiveWeapon = true;

        // Force the weapon to setup input actions if it hasn't already
        // This ensures the weapon is fully activated on pickup
        if (modularWeapon.gameObject.activeInHierarchy)
        {
            modularWeapon.enabled = false;
            yield return null;
            modularWeapon.enabled = true;
        }

        // Update UI
        yield return UpdateAmmoUI(modularWeapon);

        // Disable outline
        Outline outline = weaponObject.GetComponent<Outline>();
        if (outline != null) outline.enabled = false;

        Debug.Log("Weapon pickup complete - input should be ready");
    }

    private IEnumerator UpdateAmmoUI(ModularWeapon weapon)
    {
        yield return null;
        var ammo = weapon.GetAmmoModule();
        if (AmmoManager.Instance?.ammoDisplay != null && ammo != null)
        {
            string ammoText = $"{ammo.GetCurrentAmmo()} / {ammo.GetTotalAmmo()}";
            AmmoManager.Instance.ammoDisplay.text = ammoText;
            Debug.Log($"Updated ammo UI: {ammoText}");
        }
        else
        {
            Debug.LogWarning("Could not update ammo UI - missing components");
        }
    }

    public GameObject GetCurrentWeapon()
    {
        if (activeWeaponSlot?.transform.childCount > 0)
        {
            return activeWeaponSlot.transform.GetChild(0).gameObject;
        }
        return null;
    }

    public bool HasWeaponInActiveSlot()
    {
        return activeWeaponSlot != null && activeWeaponSlot.transform.childCount > 0;
    }
}
// end