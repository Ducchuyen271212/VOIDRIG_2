//InteractionManager.cs - Updated for ModularWeapons
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }

    [Header("Interaction Settings")]
    public float maxInteractionDistance = 5f; // Max distance to highlight weapons

    public GameObject hoveredWeapon = null; // Currently highlighted weapon (could be old or new)
    private PlayerInput playerInput;
    private InputAction interactAction;

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        // Get PlayerInput and setup interact action
        playerInput = FindFirstObjectByType<PlayerInput>();
        if (playerInput != null)
        {
            interactAction = playerInput.actions["Interact"];
            interactAction?.Enable();
        }
    }

    private void Update()
    {
        // Raycast from camera center
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, maxInteractionDistance))
        {
            // Check if hit object is a weapon (old or new system)
            GameObject weaponObject = null;

            // Try ModularWeapon first
            ModularWeapon modularWeapon = hit.transform.GetComponent<ModularWeapon>();
            if (modularWeapon != null && IsWeaponOnGround(hit.transform.gameObject))
            {
                weaponObject = hit.transform.gameObject;
            }
            else
            {
                // Try old Weapon system
                Weapon oldWeapon = hit.transform.GetComponent<Weapon>();
                if (oldWeapon != null && IsWeaponOnGround(hit.transform.gameObject))
                {
                    weaponObject = hit.transform.gameObject;
                }
            }

            if (weaponObject != null)
            {
                // Highlight weapon
                HighlightWeapon(weaponObject);

                // Handle pickup input
                if (interactAction?.WasPressedThisFrame() == true)
                {
                    WeaponManager.Instance.PickupWeapon(weaponObject);
                }
            }
            else
            {
                ClearHighlight();
            }
        }
        else
        {
            // Nothing in range
            ClearHighlight();
        }
    }

    // Check if weapon is on ground (not in player's hands)
    private bool IsWeaponOnGround(GameObject weaponObject)
    {
        return weaponObject.transform.parent == null ||
               !weaponObject.transform.parent.name.Contains("Slot");
    }

    // Highlight the weapon
    private void HighlightWeapon(GameObject weapon)
    {
        // Clear previous highlight
        if (hoveredWeapon != null && hoveredWeapon != weapon)
        {
            ClearHighlight();
        }

        // Set new highlight
        hoveredWeapon = weapon;
        Outline outline = weapon.GetComponent<Outline>();
        if (outline != null)
        {
            outline.enabled = true;
        }
    }

    // Clear weapon highlight
    private void ClearHighlight()
    {
        if (hoveredWeapon != null)
        {
            Outline outline = hoveredWeapon.GetComponent<Outline>();
            if (outline != null)
            {
                outline.enabled = false;
            }
            hoveredWeapon = null;
        }
    }

    private void OnDestroy()
    {
        // Clean up input action
        interactAction?.Disable();
    }
}