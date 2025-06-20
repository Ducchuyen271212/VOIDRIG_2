//AimingManager.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class AimingManager : MonoBehaviour
{
    public static AimingManager Instance { get; private set; }

    [Header("Aiming Settings")]
    public float accuracyMultiplier = 0.5f;

    [Header("Basic Aiming (NO zoom - for weapons without scope)")]
    public Vector3 basicAimOffset = new Vector3(0, 0, 0.2f);
    public float aimSpeed = 5f;

    [Header("Input")]
    public bool isAiming { get; private set; } = false;

    private ModularWeapon currentWeapon;
    private Camera playerCamera;
    private float originalFOV;

    // For non-scoped weapons - NO FOV CHANGE
    private Vector3 originalWeaponPosition;
    private Vector3 originalWeaponRotation;

    // InputSystem
    private PlayerInput playerInput;
    private InputAction aimAction;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera != null)
        {
            originalFOV = playerCamera.fieldOfView;
        }

        // Setup InputSystem
        playerInput = FindFirstObjectByType<PlayerInput>();
        if (playerInput?.actions != null)
        {
            try
            {
                aimAction = playerInput.actions["Aim"];
                aimAction?.Enable();
                Debug.Log($"Aim action found: {aimAction != null}");
            }
            catch
            {
                Debug.LogError("Aim action not found in InputSystem");
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F4)) // Debug key
        {
            Debug.Log("=== AIMING DEBUG ===");
            Debug.Log($"isAiming: {isAiming}");
            Debug.Log($"Current Weapon: {currentWeapon?.name ?? "NULL"}");

            if (currentWeapon != null)
            {
                var scope = currentWeapon.GetModule<ScopeAbility>();
                Debug.Log($"Scope Module: {scope?.GetType().Name ?? "NULL"}");
                Debug.Log($"Scope Enabled: {scope?.enabled ?? false}");
                Debug.Log($"Is Scoped: {scope?.IsScoped() ?? false}");
                Debug.Log($"Has Enabled Scope: {HasEnabledScope()}");
            }
        }

        HandleAimInput();
        UpdateBasicAiming();
    }

    private void HandleAimInput()
    {
        // Use InputSystem if available, fallback to hardcoded input
        bool aimInput = aimAction?.IsPressed() ?? Input.GetMouseButton(1);

        if (aimInput && !isAiming)
        {
            StartAiming();
        }
        else if (!aimInput && isAiming)
        {
            StopAiming();
        }
    }

    public void StartAiming()
    {
        if (isAiming) return;

        isAiming = true;

        // Get current weapon
        if (WeaponManager.Instance != null)
        {
            var weaponObj = WeaponManager.Instance.GetCurrentWeapon();
            if (weaponObj != null)
            {
                currentWeapon = weaponObj.GetComponent<ModularWeapon>();

                // Store original weapon position for non-scoped weapons
                if (currentWeapon != null)
                {
                    originalWeaponPosition = currentWeapon.transform.localPosition;
                    originalWeaponRotation = currentWeapon.transform.localEulerAngles;
                }
            }
        }

        Debug.Log($"Started aiming - Has scope: {HasEnabledScope()}");
    }

    public void StopAiming()
    {
        if (!isAiming) return;

        isAiming = false;
        Debug.Log("Stopped aiming");
    }

    public void ForceStopAiming()
    {
        StopAiming();
    }

    private void UpdateBasicAiming()
    {
        if (playerCamera == null || currentWeapon == null) return;

        // IMPORTANT: Only handle basic aiming if weapon does NOT have enabled scope
        if (HasEnabledScope())
        {
            // Weapon has enabled scope - let ScopeAbility handle EVERYTHING
            return;
        }

        // For weapons WITHOUT scope ability - ONLY weapon positioning, NO FOV change
        Vector3 targetPosition = isAiming ? originalWeaponPosition + basicAimOffset : originalWeaponPosition;
        Vector3 targetRotation = originalWeaponRotation; // No rotation change for basic aim

        currentWeapon.transform.localPosition = Vector3.Lerp(currentWeapon.transform.localPosition, targetPosition, Time.deltaTime * aimSpeed);
        currentWeapon.transform.localEulerAngles = Vector3.Lerp(currentWeapon.transform.localEulerAngles, targetRotation, Time.deltaTime * aimSpeed);

        // NO FOV CHANGE - keep original FOV always for non-scoped weapons
    }

    private bool HasEnabledScope()
    {
        if (currentWeapon == null) return false;

        var scopeAbility = currentWeapon.GetModule<ScopeAbility>();
        bool hasScope = scopeAbility != null && scopeAbility.enabled;

        return hasScope;
    }

    public float GetAccuracyMultiplier()
    {
        return isAiming ? accuracyMultiplier : 1f;
    }

    public void SetCurrentWeapon(ModularWeapon weapon)
    {
        currentWeapon = weapon;
    }

    // Public method to check if current weapon can scope
    public bool CanCurrentWeaponScope()
    {
        return HasEnabledScope();
    }

    private void OnDestroy()
    {
        aimAction?.Disable();
    }
}
// end