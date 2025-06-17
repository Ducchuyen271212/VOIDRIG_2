//InteractionManager.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }

    [Header("Interaction Settings")]
    public float maxInteractionDistance = 5f; // Max distance to highlight weapons

    public Weapon hoveredWeapon = null; // Currently highlighted weapon
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
            // Check if hit object is a weapon
            Weapon weaponComponent = hit.transform.GetComponent<Weapon>();

            if (weaponComponent != null && IsWeaponOnGround(weaponComponent))
            {
                // Highlight weapon
                HighlightWeapon(weaponComponent);

                // Handle pickup input
                if (interactAction?.WasPressedThisFrame() == true)
                {
                    WeaponManager.Instance.PickupWeapon(hit.transform.gameObject);
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
    private bool IsWeaponOnGround(Weapon weapon)
    {
        return weapon.transform.parent == null ||
               !weapon.transform.parent.name.Contains("Slot");
    }

    // Highlight the weapon
    private void HighlightWeapon(Weapon weapon)
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