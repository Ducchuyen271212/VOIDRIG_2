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
    public GameObject gunDataHolder;

    private PlayerInput playerInput;
    private InputAction switchWeaponAction;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        playerInput = GetComponentInParent<PlayerInput>();
        if (gunDataHolder == null)
        {
            gunDataHolder = GameObject.Find("GunDataHolder");
        }
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
                    modularWeapon.isActiveWeapon = isActive;
                }
            }
        }

        HandleInput();
    }

    private void HandleInput()
    {
        if (switchWeaponAction?.WasPressedThisFrame() == true) SwitchToNextSlot();

        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchToSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchToSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchToSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchToSlot(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SwitchToSlot(4);
    }

    private void SwitchToSlot(int index)
    {
        if (index >= 0 && index < weaponSlots.Count)
        {
            DeactivateAllWeapons();
            activeWeaponSlot = weaponSlots[index];
        }
    }

    private void SwitchToNextSlot()
    {
        if (weaponSlots.Count <= 1) return;

        int currentIndex = weaponSlots.IndexOf(activeWeaponSlot);
        int nextIndex = (currentIndex + 1) % weaponSlots.Count;
        DeactivateAllWeapons();
        activeWeaponSlot = weaponSlots[nextIndex];
    }

    private void DeactivateAllWeapons()
    {
        AimingManager.Instance?.ForceStopAiming();

        foreach (var slot in weaponSlots)
        {
            if (slot.transform.childCount > 0)
            {
                var mw = slot.transform.GetChild(0).GetComponent<ModularWeapon>();
                if (mw != null)
                {
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

        Debug.Log($"Picking up weapon: {modularWeapon.weaponName}");

        // Disable physics
        Rigidbody rb = weaponObject.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // Parent to active slot
        weaponObject.transform.SetParent(activeWeaponSlot.transform, false);

        // Set data FIRST
        Debug.Log("Setting gun data holder...");
        modularWeapon.SetGunDataHolder(gunDataHolder);

        // Wait a frame for initialization to complete
        StartCoroutine(CompleteWeaponPickup(modularWeapon, weaponObject));
    }

    private IEnumerator CompleteWeaponPickup(ModularWeapon modularWeapon, GameObject weaponObject)
    {
        yield return null; // Wait one frame

        // Initialize ammo
        Debug.Log("Initializing ammo...");
        modularWeapon.InitializeAmmo();

        // Position weapon
        weaponObject.transform.localPosition = modularWeapon.spawnPosition;
        weaponObject.transform.localRotation = Quaternion.Euler(modularWeapon.spawnRotation);

        // Re-enable animator (it might have been disabled when dropped)
        Animator animator = weaponObject.GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = true;
        }

        // Re-setup input actions for this weapon
        modularWeapon.SetupInput();

        // Activate weapon
        modularWeapon.isActiveWeapon = true;

        // Update UI
        yield return UpdateAmmoUI(modularWeapon);

        // Disable outline
        Outline outline = weaponObject.GetComponent<Outline>();
        if (outline != null) outline.enabled = false;

        Debug.Log($"Weapon pickup complete for {modularWeapon.weaponName}");
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