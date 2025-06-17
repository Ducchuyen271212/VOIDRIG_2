//InteractionManager.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }
    public Weapon hoveredWeapon = null;

    private PlayerInput playerInput;
    private InputAction interactAction;

    private void Awake()
    {
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
        // Get PlayerInput component (should be on Player or parent)
        playerInput = FindFirstObjectByType<PlayerInput>();

        if (playerInput != null)
        {
            interactAction = playerInput.actions["Interact"];
            interactAction?.Enable();
        }
        else
        {
            Debug.LogError("PlayerInput not found! Make sure it exists in the scene.");
        }
    }

    private void Update()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hitInfo;

        if (Physics.Raycast(ray, out hitInfo))
        {
            GameObject objectHitByRaycast = hitInfo.transform.gameObject;
            if (objectHitByRaycast.GetComponent<Weapon>())
            {
                hoveredWeapon = objectHitByRaycast.gameObject.GetComponent<Weapon>();
                hoveredWeapon.GetComponent<Outline>().enabled = true;

                // Changed from KeyCode.E to Input System
                if (interactAction?.WasPressedThisFrame() == true)
                {
                    WeaponManager.Instance.PickupWeapon(objectHitByRaycast.gameObject);
                }
            }
            else
            {
                if (hoveredWeapon)
                {
                    hoveredWeapon.GetComponent<Outline>().enabled = false;
                    hoveredWeapon = null; // Clear the reference
                }
            }
        }
        else
        {
            // Clear hover when not looking at anything
            if (hoveredWeapon)
            {
                hoveredWeapon.GetComponent<Outline>().enabled = false;
                hoveredWeapon = null;
            }
        }
    }

    private void OnDestroy()
    {
        // Clean up
        interactAction?.Disable();
    }
}