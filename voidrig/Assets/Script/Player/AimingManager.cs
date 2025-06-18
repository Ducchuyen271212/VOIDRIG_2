//AimingManager.cs - Clean Version Without Debug
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

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Get camera
        playerCamera = Camera.main;
        if (playerCamera == null) return;

        // Store original FOV
        originalFOV = playerCamera.fieldOfView;

        // Get player input
        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            playerInput = GetComponentInParent<PlayerInput>();
        }
        if (playerInput == null)
        {
            playerInput = FindObjectOfType<PlayerInput>();
        }

        if (playerInput == null) return;

        // Get mouse movement
        mouseMovement = FindObjectOfType<MouseMovement>();
        if (mouseMovement != null)
        {
            originalMouseSensitivity = mouseMovement.mouseSensitivity;
        }

        // Setup input actions
        if (playerInput?.actions != null)
        {
            aimAction = playerInput.actions["Aim"];
            aimAction?.Enable();
        }

        // Setup crosshairs
        SetCrosshairVisibility(false);
    }

    private void Update()
    {
        UpdateCurrentWeapon();
        HandleAimInput();
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
            }
        }

        // If we lost the weapon (dropped), stop aiming and reset FOV
        if (previousWeapon != null && currentWeapon == null)
        {
            ForceStopAiming();
            ResetCameraFOV();
        }
    }

    private GunData.Attribute GetWeaponAimingData(Weapon weapon)
    {
        if (WeaponManager.Instance?.gunDataHolder == null) return null;

        GunData gunData = WeaponManager.Instance.gunDataHolder.GetComponent<GunData>();
        if (gunData == null) return null;

        return weapon.gameObject.tag switch
        {
            "MachineGun" => gunData.machineGun,
            "ShotGun" => gunData.shotGun,
            "Sniper" => gunData.sniper,
            "HandGun" => gunData.handGun,
            "SMG" => gunData.smg,
            "BurstRifle" => gunData.burstRifle,
            _ => null
        };
    }

    private void HandleAimInput()
    {
        if (aimAction == null)
        {
            if (isAiming)
            {
                StopAiming();
            }
            return;
        }

        if (currentWeaponData?.canAim != true)
        {
            if (isAiming)
            {
                StopAiming();
            }
            return;
        }

        bool wantsToAim = aimAction.IsPressed();

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

        isAiming = true;

        // Reduce mouse sensitivity
        if (mouseMovement != null)
        {
            mouseMovement.mouseSensitivity = originalMouseSensitivity * aimMouseSensitivityMultiplier;
        }

        SetCrosshairVisibility(true);
    }

    private void StopAiming()
    {
        if (!isAiming) return;

        isAiming = false;

        // Restore mouse sensitivity
        if (mouseMovement != null)
        {
            mouseMovement.mouseSensitivity = originalMouseSensitivity;
        }

        SetCrosshairVisibility(false);
    }

    // Force reset camera FOV to original
    private void ResetCameraFOV()
    {
        if (playerCamera != null)
        {
            playerCamera.fieldOfView = originalFOV;
        }
    }

    private void UpdateAiming()
    {
        if (playerCamera == null) return;

        // FOV transition
        if (currentWeaponData?.canAim == true)
        {
            float targetFOV = isAiming ? currentWeaponData.aimFOV : originalFOV;
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, aimSpeed * Time.deltaTime);
        }

        // Weapon slot positioning
        if (currentWeapon?.transform.parent != null && currentWeaponData?.canAim == true)
        {
            Transform weaponSlot = currentWeapon.transform.parent;

            Vector3 targetPos = isAiming ? currentWeaponData.aimPositionOffset : Vector3.zero;
            Vector3 targetRot = isAiming ? currentWeaponData.aimRotationOffset : Vector3.zero;

            weaponSlot.localPosition = Vector3.Lerp(weaponSlot.localPosition, targetPos, aimSpeed * Time.deltaTime);
            weaponSlot.localRotation = Quaternion.Lerp(weaponSlot.localRotation, Quaternion.Euler(targetRot), aimSpeed * Time.deltaTime);
        }
    }

    private void SetCrosshairVisibility(bool aiming)
    {
        if (normalCrosshair != null) normalCrosshair.SetActive(!aiming);
        if (aimCrosshair != null) aimCrosshair.SetActive(aiming);
    }

    public float GetAccuracyMultiplier()
    {
        return (isAiming && currentWeaponData != null) ? currentWeaponData.aimAccuracyMultiplier : 1f;
    }

    public void ForceStopAiming()
    {
        if (isAiming)
        {
            StopAiming();
        }
    }

    private void OnDestroy()
    {
        aimAction?.Disable();
    }
}