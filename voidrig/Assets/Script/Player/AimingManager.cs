//AimingManager.cs
using UnityEngine;

public class AimingManager : MonoBehaviour
{
    public static AimingManager Instance { get; private set; }

    [Header("Aiming Settings")]
    public float aimFOV = 40f;
    public float normalFOV = 60f;
    public float aimSpeed = 5f;
    public float accuracyMultiplier = 0.5f;

    [Header("Aim Position")]
    public Vector3 aimPositionOffset = new Vector3(0, 0, 0.2f);
    public Vector3 aimRotationOffset = Vector3.zero;

    public bool isAiming { get; private set; } = false;

    private Camera playerCamera;
    private ModularWeapon currentWeapon;
    private Vector3 originalWeaponPosition;
    private Vector3 originalWeaponRotation;

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
            normalFOV = playerCamera.fieldOfView;
        }
    }

    private void Update()
    {
        HandleAimInput();
        UpdateAiming();
    }

    private void HandleAimInput()
    {
        bool aimInput = Input.GetMouseButton(1); // Right mouse button

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
                if (currentWeapon != null)
                {
                    originalWeaponPosition = currentWeapon.transform.localPosition;
                    originalWeaponRotation = currentWeapon.transform.localEulerAngles;
                }
            }
        }

        Debug.Log("Started aiming");
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

    private void UpdateAiming()
    {
        if (playerCamera == null) return;

        float targetFOV = isAiming ? aimFOV : normalFOV;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * aimSpeed);

        // Update weapon position if we have one
        if (currentWeapon != null)
        {
            Vector3 targetPosition = isAiming ? originalWeaponPosition + aimPositionOffset : originalWeaponPosition;
            Vector3 targetRotation = isAiming ? originalWeaponRotation + aimRotationOffset : originalWeaponRotation;

            currentWeapon.transform.localPosition = Vector3.Lerp(currentWeapon.transform.localPosition, targetPosition, Time.deltaTime * aimSpeed);
            currentWeapon.transform.localEulerAngles = Vector3.Lerp(currentWeapon.transform.localEulerAngles, targetRotation, Time.deltaTime * aimSpeed);
        }
    }

    public float GetAccuracyMultiplier()
    {
        return isAiming ? accuracyMultiplier : 1f;
    }

    public void SetCurrentWeapon(ModularWeapon weapon)
    {
        currentWeapon = weapon;
        if (weapon != null)
        {
            originalWeaponPosition = weapon.transform.localPosition;
            originalWeaponRotation = weapon.transform.localEulerAngles;
        }
    }
}
// end