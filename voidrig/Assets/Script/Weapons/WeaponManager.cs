//WeaponManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponManager : MonoBehaviour
{
    public static WeaponManager Instance { get; private set; }

    [Header("Weapon Slots")]
    public List<GameObject> weaponSlots; // List of weapon slot GameObjects
    public GameObject activeWeaponSlot; // Currently active slot

    [Header("References")]
    public GameObject player; // Player GameObject for drop positioning
    public GameObject gunDataHolder; // GameObject with GunData component

    private PlayerInput playerInput;
    private InputAction switchWeaponAction;

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Get player input
        playerInput = GetComponentInParent<PlayerInput>();

        // Find GunDataHolder if not assigned
        if (gunDataHolder == null)
        {
            gunDataHolder = GameObject.Find("GunDataHolder");
        }
    }

    private void Start()
    {
        // Set first slot as active
        if (weaponSlots.Count > 0)
        {
            activeWeaponSlot = weaponSlots[0];
        }

        // Setup switch weapon action
        if (playerInput?.actions != null)
        {
            switchWeaponAction = playerInput.actions["SwitchWeapon"];
        }
    }

    private void Update()
    {
        // Manage slot visibility and weapon activation
        foreach (GameObject weaponSlot in weaponSlots)
        {
            bool isActive = weaponSlot == activeWeaponSlot;
            weaponSlot.SetActive(isActive);

            // Activate/deactivate weapons in slots
            if (weaponSlot.transform.childCount > 0)
            {
                Weapon weaponScript = weaponSlot.transform.GetChild(0).GetComponent<Weapon>();
                if (weaponScript != null)
                {
                    weaponScript.isActiveWeapon = isActive;
                }
            }
        }

        // Handle input
        HandleInput();
    }

    // Handle all input for weapon switching and dropping
    private void HandleInput()
    {
        // Scroll wheel switching
        if (switchWeaponAction?.WasPressedThisFrame() == true)
        {
            SwitchToNextSlot();
        }

        // Number key switching (1-5)
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchToSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchToSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchToSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchToSlot(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SwitchToSlot(4);

        // Drop weapon (G key)
        if (Input.GetKeyDown(KeyCode.G))
        {
            DropItem();
        }
    }

    // Switch to specific slot by index
    private void SwitchToSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < weaponSlots.Count)
        {
            DeactivateAllWeapons();
            activeWeaponSlot = weaponSlots[slotIndex];
        }
    }

    // Switch to next slot (scroll wheel)
    private void SwitchToNextSlot()
    {
        if (weaponSlots.Count <= 1) return;

        DeactivateAllWeapons();
        int currentIndex = weaponSlots.IndexOf(activeWeaponSlot);
        int nextIndex = (currentIndex + 1) % weaponSlots.Count;
        activeWeaponSlot = weaponSlots[nextIndex];
    }

    // Deactivate all weapons and clear UI
    private void DeactivateAllWeapons()
    {
        foreach (GameObject slot in weaponSlots)
        {
            if (slot.transform.childCount > 0)
            {
                Weapon weapon = slot.transform.GetChild(0).GetComponent<Weapon>();
                if (weapon != null)
                {
                    weapon.isActiveWeapon = false;
                }
            }
        }

        // Clear ammo display
        if (AmmoManager.Instance?.ammoDisplay != null)
        {
            AmmoManager.Instance.ammoDisplay.text = "-- / --";
        }
    }

    // Drop current weapon with physics
    public void DropItem()
    {
        if (!HasWeaponInActiveSlot()) return;

        GameObject weaponToDrop = GetCurrentWeapon();
        Weapon weaponScript = weaponToDrop.GetComponent<Weapon>();

        // Deactivate weapon
        if (weaponScript != null)
        {
            weaponScript.isActiveWeapon = false;
        }

        // Clear UI
        if (AmmoManager.Instance?.ammoDisplay != null)
        {
            AmmoManager.Instance.ammoDisplay.text = "-- / --";
        }

        // Remove from slot
        weaponToDrop.transform.SetParent(null);

        // Position in front of player
        Vector3 dropPos = player.transform.position + player.transform.forward * 2f + Vector3.up * 1f;
        weaponToDrop.transform.position = dropPos;
        weaponToDrop.transform.rotation = Quaternion.identity;

        // Disable animator
        Animator animator = weaponToDrop.GetComponent<Animator>();
        if (animator != null) animator.enabled = false;

        // Enable physics
        Rigidbody rb = weaponToDrop.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 0.5f;

            // Add throw force
            Vector3 force = player.transform.forward * 4f + Vector3.up * 3f;
            rb.AddForce(force, ForceMode.Impulse);

            // Add random spin
            Vector3 torque = new Vector3(
                Random.Range(-2f, 2f),
                Random.Range(-2f, 2f),
                Random.Range(-2f, 2f)
            );
            rb.AddTorque(torque, ForceMode.Impulse);
        }

        // Enable collider
        Collider col = weaponToDrop.GetComponent<Collider>();
        if (col != null) col.enabled = true;
    }

    // Called by InteractionManager when picking up weapon
    public void PickupWeapon(GameObject weaponObject)
    {
        AddWeaponIntoActiveSlot(weaponObject);
    }

    // Add weapon to currently active slot
    public void AddWeaponIntoActiveSlot(GameObject weaponObject)
    {
        // Check if slot is empty
        if (activeWeaponSlot.transform.childCount > 0) return;

        Weapon weapon = weaponObject.GetComponent<Weapon>();
        if (weapon == null) return;

        // Disable physics
        Rigidbody rb = weaponObject.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // Parent to active slot
        weaponObject.transform.SetParent(activeWeaponSlot.transform, false);

        // Setup weapon data
        weapon.SetGunDataHolder(gunDataHolder);
        weapon.InitializeAmmo();

        // Position weapon in hand
        weaponObject.transform.localPosition = weapon.spawnPosition;
        weaponObject.transform.localRotation = Quaternion.Euler(weapon.spawnRotation);

        // Enable animator
        Animator animator = weaponObject.GetComponent<Animator>();
        if (animator != null) animator.enabled = true;

        // Activate weapon and setup input
        weapon.isActiveWeapon = true;
        weapon.SetupInput();

        // Update UI
        StartCoroutine(UpdateAmmoDisplayNextFrame(weapon));

        // Disable outline
        Outline outline = weaponObject.GetComponent<Outline>();
        if (outline != null) outline.enabled = false;
    }

    // Update ammo display on next frame to ensure proper timing
    private IEnumerator UpdateAmmoDisplayNextFrame(Weapon weapon)
    {
        yield return null;

        if (AmmoManager.Instance?.ammoDisplay != null && weapon.isActiveWeapon)
        {
            AmmoManager.Instance.ammoDisplay.text = $"{weapon.CurrentAmmo} / {weapon.TotalAmmo}";
        }
    }

    // Utility methods
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

    public int GetActiveSlotIndex()
    {
        return weaponSlots.IndexOf(activeWeaponSlot);
    }
}