//WeaponManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponManager : MonoBehaviour
{
    public static WeaponManager Instance { get; private set; }

    [Header("Weapon Slots")]
    public List<GameObject> weaponSlots;
    public GameObject activeWeaponSlot;

    [Header("References")]
    public GameObject player;
    public GameObject gunDataHolder;

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
                Debug.LogError("WeaponManager: No GunDataHolder found!");
                return;
            }
        }

        if (gunDataHolder.GetComponent<GunData>() == null)
        {
            Debug.LogError("WeaponManager: GunDataHolder GameObject doesn't have a GunData component!");
            return;
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
        // Manage weapon slot visibility and activation
        foreach (GameObject weaponSlot in weaponSlots)
        {
            if (weaponSlot == activeWeaponSlot)
            {
                weaponSlot.SetActive(true);

                // Activate weapon in current slot
                if (weaponSlot.transform.childCount > 0)
                {
                    GameObject activeWeapon = weaponSlot.transform.GetChild(0).gameObject;
                    Weapon activeWeaponScript = activeWeapon.GetComponent<Weapon>();
                    if (activeWeaponScript != null)
                    {
                        activeWeaponScript.isActiveWeapon = true;
                    }
                }
            }
            else
            {
                weaponSlot.SetActive(false);

                // Deactivate weapons in inactive slots
                if (weaponSlot.transform.childCount > 0)
                {
                    GameObject inactiveWeapon = weaponSlot.transform.GetChild(0).gameObject;
                    Weapon inactiveWeaponScript = inactiveWeapon.GetComponent<Weapon>();
                    if (inactiveWeaponScript != null)
                    {
                        inactiveWeaponScript.isActiveWeapon = false;
                    }
                }
            }
        }

        // Handle weapon switching
        if (switchWeaponAction?.WasPressedThisFrame() == true)
        {
            SwitchToNextSlot();
        }

        // Number key switching
        if (Input.GetKeyDown(KeyCode.Alpha1)) slot1();
        if (Input.GetKeyDown(KeyCode.Alpha2)) slot2();
        if (Input.GetKeyDown(KeyCode.Alpha3)) slot3();
        if (Input.GetKeyDown(KeyCode.Alpha4)) slot4();
        if (Input.GetKeyDown(KeyCode.Alpha5)) slot5();

        // Drop weapon
        if (Input.GetKeyDown(KeyCode.G))
        {
            DropItem();
        }
    }

    // Slot switching methods
    public void slot1() { SwitchToSlot(0); }
    public void slot2() { SwitchToSlot(1); }
    public void slot3() { SwitchToSlot(2); }
    public void slot4() { SwitchToSlot(3); }
    public void slot5() { SwitchToSlot(4); }

    private void SwitchToSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < weaponSlots.Count)
        {
            DeactivateAllWeapons();
            activeWeaponSlot = weaponSlots[slotIndex];
            Debug.Log($"Switched to weapon slot {slotIndex + 1}");
        }
    }

    private void SwitchToNextSlot()
    {
        if (weaponSlots.Count <= 1) return;

        DeactivateAllWeapons();
        int currentSlotIndex = weaponSlots.IndexOf(activeWeaponSlot);
        int nextSlotIndex = (currentSlotIndex + 1) % weaponSlots.Count;
        activeWeaponSlot = weaponSlots[nextSlotIndex];

        Debug.Log($"Switched to weapon slot {nextSlotIndex + 1}");
    }

    private void DeactivateAllWeapons()
    {
        foreach (GameObject weaponSlot in weaponSlots)
        {
            if (weaponSlot.transform.childCount > 0)
            {
                GameObject weapon = weaponSlot.transform.GetChild(0).gameObject;
                Weapon weaponScript = weapon.GetComponent<Weapon>();
                if (weaponScript != null)
                {
                    weaponScript.isActiveWeapon = false;
                }
            }
        }

        // Clear ammo display when no weapon is active
        if (AmmoManager.Instance?.ammoDisplay != null)
        {
            AmmoManager.Instance.ammoDisplay.text = "-- / --";
        }
    }

    public void DropItem()
    {
        if (!HasWeaponInActiveSlot())
        {
            Debug.Log("No weapon to drop!");
            return;
        }

        GameObject weaponToDrop = GetCurrentWeapon();
        Weapon weaponScript = weaponToDrop.GetComponent<Weapon>();

        // Deactivate weapon
        if (weaponScript != null)
        {
            weaponScript.isActiveWeapon = false;
        }

        // Remove from slot
        weaponToDrop.transform.SetParent(null);

        // Position in front of player
        Vector3 dropPosition = player.transform.position + player.transform.forward * 2f + Vector3.up * 1f;
        weaponToDrop.transform.position = dropPosition;
        weaponToDrop.transform.rotation = Quaternion.identity;

        // Disable animator
        Animator weaponAnimator = weaponToDrop.GetComponent<Animator>();
        if (weaponAnimator != null)
        {
            weaponAnimator.enabled = false;
        }

        // Enable physics
        Rigidbody weaponRb = weaponToDrop.GetComponent<Rigidbody>();
        if (weaponRb != null)
        {
            weaponRb.isKinematic = false;
            weaponRb.useGravity = true;
            weaponRb.drag = 0.5f;
            weaponRb.angularDrag = 0.5f;

            // Add drop force
            Vector3 throwForce = player.transform.forward * 4f + Vector3.up * 3f;
            weaponRb.AddForce(throwForce, ForceMode.Impulse);

            // Add random rotation
            Vector3 randomTorque = new Vector3(
                Random.Range(-2f, 2f),
                Random.Range(-2f, 2f),
                Random.Range(-2f, 2f)
            );
            weaponRb.AddTorque(randomTorque, ForceMode.Impulse);
        }

        // Enable collider
        Collider weaponCollider = weaponToDrop.GetComponent<Collider>();
        if (weaponCollider != null)
        {
            weaponCollider.enabled = true;
        }

        Debug.Log($"Dropped weapon: {weaponToDrop.name}");
    }

    public void PickupWeapon(GameObject pickedupWeapon)
    {
        AddWeaponIntoActiveSlot(pickedupWeapon);
    }

    public void AddWeaponIntoActiveSlot(GameObject pickedupWeapon)
    {
        if (activeWeaponSlot.transform.childCount > 0)
        {
            Debug.Log("Active weapon slot is occupied! Drop current weapon first or switch slots.");
            return;
        }

        Weapon weapon = pickedupWeapon.GetComponent<Weapon>();
        if (weapon == null)
        {
            Debug.LogError("Picked up object doesn't have a Weapon component!");
            return;
        }

        // Disable physics
        Rigidbody weaponRb = pickedupWeapon.GetComponent<Rigidbody>();
        if (weaponRb != null)
        {
            weaponRb.isKinematic = true;
        }

        // Parent to slot
        pickedupWeapon.transform.SetParent(activeWeaponSlot.transform, false);

        // Set data holder
        weapon.SetGunDataHolder(gunDataHolder);

        // Position weapon using spawn values
        pickedupWeapon.transform.localPosition = weapon.spawnPosition;
        pickedupWeapon.transform.localRotation = Quaternion.Euler(weapon.spawnRotation);

        // Enable animator
        Animator weaponAnimator = pickedupWeapon.GetComponent<Animator>();
        if (weaponAnimator != null)
        {
            weaponAnimator.enabled = true;
        }

        // Initialize ammo
        weapon.InitializeAmmo();

        // Disable outline
        Outline outline = pickedupWeapon.GetComponent<Outline>();
        if (outline != null)
        {
            outline.enabled = false;
        }

        Debug.Log($"Picked up {pickedupWeapon.name}");
    }

    // Utility methods
    public GameObject GetCurrentWeapon()
    {
        if (activeWeaponSlot != null && activeWeaponSlot.transform.childCount > 0)
        {
            return activeWeaponSlot.transform.GetChild(0).gameObject;
        }
        return null;
    }

    public bool HasWeaponInActiveSlot()
    {
        return activeWeaponSlot != null && activeWeaponSlot.transform.childCount > 0;
    }

    public int GetActiveSlotIndex()
    {
        return weaponSlots.IndexOf(activeWeaponSlot);
    }
}