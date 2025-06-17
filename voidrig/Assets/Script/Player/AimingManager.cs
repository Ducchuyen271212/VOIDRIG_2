//AimingManager.cs - With Full Debugging and WeaponSpawn Integration
using UnityEngine;
using UnityEngine.InputSystem;

public class AimingManager : MonoBehaviour
{
    public static AimingManager Instance { get; private set; }

    [Header("Aiming Settings")]
    public float aimSpeed = 8f;
    public float aimMouseSensitivityMultiplier = 0.5f;

    [Header("Crosshair")]
    public GameObject normalCrosshair;
    public GameObject aimCrosshair;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    // State
    public bool isAiming { get; private set; } = false;

    // References
    private Camera playerCamera;
    private PlayerInput playerInput;
    private InputAction aimAction;
    private Weapon currentWeapon;
    private MouseMovement mouseMovement;

    // Current weapon aiming settings
    private GunData.Attribute currentWeaponData;
    private float originalFOV;
    private float originalMouseSensitivity;
    private Vector3 originalWeaponPosition;
    private Vector3 originalWeaponRotation;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DebugLog("AimingManager: Instance created successfully");
        }
        else
        {
            DebugLog("AimingManager: Duplicate instance destroyed");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        DebugLog("AimingManager: Starting initialization...");

        // Get camera
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            DebugLog("ERROR: No main camera found!");
            return;
        }
        DebugLog($"Camera found: {playerCamera.name}");

        // Store original FOV FIRST
        originalFOV = playerCamera.fieldOfView;
        DebugLog($"Original camera FOV: {originalFOV}");

        // Get player input - try multiple locations
        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            // Try parent
            playerInput = GetComponentInParent<PlayerInput>();
        }
        if (playerInput == null)
        {
            // Try finding it anywhere
            playerInput = FindObjectOfType<PlayerInput>();
        }

        if (playerInput == null)
        {
            DebugLog("ERROR: No PlayerInput component found anywhere! Please add AimingManager to the same GameObject as PlayerInput.");
            return;
        }
        DebugLog($"PlayerInput found on: {playerInput.gameObject.name}");

        // Get mouse movement (should be on camera or child)
        mouseMovement = FindObjectOfType<MouseMovement>();
        if (mouseMovement == null)
        {
            DebugLog("WARNING: MouseMovement component not found!");
        }
        else
        {
            DebugLog($"MouseMovement found on: {mouseMovement.gameObject.name}");
            originalMouseSensitivity = mouseMovement.mouseSensitivity;
            DebugLog($"Original mouse sensitivity: {originalMouseSensitivity}");
        }

        // Setup input actions
        if (playerInput?.actions != null)
        {
            try
            {
                aimAction = playerInput.actions["Aim"];
                if (aimAction != null)
                {
                    aimAction.Enable();
                    DebugLog("Aim action found and enabled successfully!");
                }
                else
                {
                    DebugLog("ERROR: 'Aim' action not found in Input Actions! Please add it.");
                }
            }
            catch (System.Exception e)
            {
                DebugLog($"ERROR setting up aim action: {e.Message}");
            }
        }
        else
        {
            DebugLog("ERROR: PlayerInput actions are null!");
        }

        // Setup crosshairs
        SetCrosshairVisibility(false);
        DebugLog("AimingManager initialization complete!");
    }

    private void Update()
    {
        // Update current weapon info
        UpdateCurrentWeapon();

        // Handle aiming input
        HandleAimInput();

        // Update aiming transitions
        UpdateAiming();
    }

    private void UpdateCurrentWeapon()
    {
        Weapon previousWeapon = currentWeapon;
        currentWeapon = null;
        currentWeaponData = null;

        if (WeaponManager.Instance?.GetCurrentWeapon() != null)
        {
            currentWeapon = WeaponManager.Instance.GetCurrentWeapon().GetComponent<Weapon>();

            if (currentWeapon != null)
            {
                currentWeaponData = GetWeaponAimingData(currentWeapon);

                // Log when weapon changes
                if (previousWeapon != currentWeapon)
                {
                    DebugLog($"Current weapon changed to: {currentWeapon.gameObject.name}");
                    DebugLog($"Weapon tag: {currentWeapon.gameObject.tag}");
                    DebugLog($"Can aim: {currentWeaponData?.canAim}");
                    if (currentWeaponData != null)
                    {
                        DebugLog($"Aim FOV: {currentWeaponData.aimFOV}");
                        DebugLog($"Aim accuracy multiplier: {currentWeaponData.aimAccuracyMultiplier}");
                    }
                }
            }
        }
        else if (previousWeapon != null)
        {
            DebugLog("No current weapon");
        }
    }

    private GunData.Attribute GetWeaponAimingData(Weapon weapon)
    {
        if (WeaponManager.Instance?.gunDataHolder == null)
        {
            DebugLog("ERROR: WeaponManager.gunDataHolder is null!");
            return null;
        }

        GunData gunData = WeaponManager.Instance.gunDataHolder.GetComponent<GunData>();
        if (gunData == null)
        {
            DebugLog("ERROR: GunData component not found on gunDataHolder!");
            return null;
        }

        string weaponTag = weapon.gameObject.tag;
        GunData.Attribute weaponData = weaponTag switch
        {
            "MachineGun" => gunData.machineGun,
            "ShotGun" => gunData.shotGun,
            "Sniper" => gunData.sniper,
            "HandGun" => gunData.handGun,
            "SMG" => gunData.smg,
            "BurstRifle" => gunData.burstRifle,
            _ => null
        };

        if (weaponData == null)
        {
            DebugLog($"WARNING: No weapon data found for tag '{weaponTag}'");
        }

        return weaponData;
    }

    private void HandleAimInput()
    {
        if (aimAction == null)
        {
            // No input available - make sure we're not aiming
            if (isAiming)
            {
                DebugLog("No aim input available - stopping aim");
                StopAiming();
            }
            return;
        }

        if (currentWeaponData?.canAim != true)
        {
            if (isAiming)
            {
                DebugLog("Stopping aim - weapon cannot aim");
                StopAiming();
            }
            return;
        }

        bool wantsToAim = aimAction.IsPressed();

        // Debug input state changes
        bool lastWantedToAim = false;
        if (wantsToAim != lastWantedToAim)
        {
            DebugLog($"Aim input changed: {wantsToAim} (Right mouse {(wantsToAim ? "pressed" : "released")})");
            lastWantedToAim = wantsToAim;
        }

        if (wantsToAim && !isAiming)
        {
            StartAiming();
        }
        else if (!wantsToAim && isAiming)
        {
            StopAiming();
        }
    }

    private void StartAiming()
    {
        if (currentWeaponData?.canAim != true || isAiming) return;

        DebugLog($"=== STARTING AIM ===");
        DebugLog($"Weapon: {currentWeapon.gameObject.name}");
        DebugLog($"Target FOV: {currentWeaponData.aimFOV} (from {originalFOV})");

        isAiming = true;

        // Store original weapon transform
        if (currentWeapon != null)
        {
            originalWeaponPosition = currentWeapon.transform.localPosition;
            originalWeaponRotation = currentWeapon.transform.localEulerAngles;
            DebugLog($"Original weapon position: {originalWeaponPosition}");
            DebugLog($"Original weapon rotation: {originalWeaponRotation}");
            DebugLog($"Aim position offset: {currentWeaponData.aimPositionOffset}");
            DebugLog($"Aim rotation offset: {currentWeaponData.aimRotationOffset}");
        }

        // Reduce mouse sensitivity
        if (mouseMovement != null)
        {
            float newSensitivity = originalMouseSensitivity * aimMouseSensitivityMultiplier;
            mouseMovement.mouseSensitivity = newSensitivity;
            DebugLog($"Mouse sensitivity: {originalMouseSensitivity} -> {newSensitivity}");
        }

        // Update crosshair
        SetCrosshairVisibility(true);
        DebugLog("=== AIM STARTED ===");
    }

    private void StopAiming()
    {
        if (!isAiming) return;

        DebugLog("=== STOPPING AIM ===");

        isAiming = false;

        // Restore mouse sensitivity
        if (mouseMovement != null)
        {
            mouseMovement.mouseSensitivity = originalMouseSensitivity;
            DebugLog($"Mouse sensitivity restored to: {originalMouseSensitivity}");
        }

        // Update crosshair
        SetCrosshairVisibility(false);
        DebugLog("=== AIM STOPPED ===");
    }

    private void UpdateAiming()
    {
        if (playerCamera == null) return;

        // Only do aiming transitions if we have a weapon that can aim
        if (currentWeaponData?.canAim != true)
        {
            // If weapon can't aim, make sure we're not in aiming state
            if (isAiming)
            {
                isAiming = false;
                SetCrosshairVisibility(false);
            }
            return;
        }

        // Smooth FOV transition
        float targetFOV = isAiming ? currentWeaponData.aimFOV : originalFOV;
        float oldFOV = playerCamera.fieldOfView;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, aimSpeed * Time.deltaTime);

        // Debug FOV changes (only when significant)
        if (Mathf.Abs(oldFOV - playerCamera.fieldOfView) > 0.1f)
        {
            DebugLog($"FOV transitioning: {playerCamera.fieldOfView:F1} -> {targetFOV:F1}");
        }

        // Only adjust weapon position if we're actually aiming or transitioning
        if (currentWeapon != null && (isAiming || Vector3.Distance(currentWeapon.transform.localPosition, currentWeapon.spawnPosition) > 0.01f))
        {
            Vector3 targetPos = isAiming ?
                (currentWeapon.spawnPosition + currentWeaponData.aimPositionOffset) :
                currentWeapon.spawnPosition;

            Vector3 targetRot = isAiming ?
                (currentWeapon.spawnRotation + currentWeaponData.aimRotationOffset) :
                currentWeapon.spawnRotation;

            Vector3 oldPos = currentWeapon.transform.localPosition;

            // Apply the positioning directly
            currentWeapon.transform.localPosition = Vector3.Lerp(
                currentWeapon.transform.localPosition,
                targetPos,
                aimSpeed * Time.deltaTime
            );

            currentWeapon.transform.localRotation = Quaternion.Lerp(
                currentWeapon.transform.localRotation,
                Quaternion.Euler(targetRot),
                aimSpeed * Time.deltaTime
            );

            // Debug position changes (only when significant)
            if (Vector3.Distance(oldPos, currentWeapon.transform.localPosition) > 0.001f)
            {
                DebugLog($"Weapon positioning - Current: {currentWeapon.transform.localPosition} Target: {targetPos} IsAiming: {isAiming}");
            }
        }
    }

    private void SetCrosshairVisibility(bool aiming)
    {
        if (normalCrosshair != null)
        {
            normalCrosshair.SetActive(!aiming);
            DebugLog($"Normal crosshair: {!aiming}");
        }
        if (aimCrosshair != null)
        {
            aimCrosshair.SetActive(aiming);
            DebugLog($"Aim crosshair: {aiming}");
        }
    }

    public float GetAccuracyMultiplier()
    {
        float multiplier = (isAiming && currentWeaponData != null) ? currentWeaponData.aimAccuracyMultiplier : 1f;
        if (isAiming) DebugLog($"Accuracy multiplier: {multiplier}");
        return multiplier;
    }

    public void ForceStopAiming()
    {
        if (isAiming)
        {
            DebugLog("Force stopping aim");
            StopAiming();
        }
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[AimingManager] {message}");
        }
    }

    private void OnDestroy()
    {
        aimAction?.Disable();
    }

    // Public method to check aiming status in inspector
    [System.Serializable]
    public class DebugInfo
    {
        public bool isAiming;
        public string currentWeapon;
        public bool canCurrentWeaponAim;
        public float currentFOV;
        public float targetFOV;
        public bool aimActionExists;
        public bool mouseMovementFound;
    }

    public DebugInfo GetDebugInfo()
    {
        return new DebugInfo
        {
            isAiming = this.isAiming,
            currentWeapon = currentWeapon?.gameObject.name ?? "None",
            canCurrentWeaponAim = currentWeaponData?.canAim ?? false,
            currentFOV = playerCamera?.fieldOfView ?? 0f,
            targetFOV = isAiming ? (currentWeaponData?.aimFOV ?? 0f) : originalFOV,
            aimActionExists = aimAction != null,
            mouseMovementFound = mouseMovement != null
        };
    }
}