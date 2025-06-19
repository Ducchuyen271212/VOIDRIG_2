//FlexibleFireModule.cs - Complete flexible fire system
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FlexibleFireModule : MonoBehaviour, IFireModule
{
    [Header("Available Fire Modes - Check the ones you want")]
    public bool allowSingle = true;
    public bool allowBurst = true;
    public bool allowAuto = false;

    [Header("Current Settings")]
    [SerializeField] private FireMode currentMode = FireMode.Single;
    [SerializeField] private int currentModeIndex = 0;

    [Header("Burst Settings")]
    public int burstCount = 3;
    public float burstInterval = 0.1f;

    [Header("Auto Settings")]
    public float autoFireRate = 0.1f;

    // Internal state
    private ModularWeapon weapon;
    private List<FireMode> availableModes = new List<FireMode>();
    private bool isFirePressed = false;
    private bool wasFirePressed = false;
    private float lastFireTime = -1f;
    private bool isFiring = false;
    private bool isBursting = false;
    private Coroutine autoFireCoroutine;

    public void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;
        SetupAvailableModes();

        // Get settings from weapon data if available
        if (weapon.WeaponData != null)
        {
            burstCount = weapon.WeaponData.bulletsPerBurst;
            burstInterval = weapon.WeaponData.burstFireInterval;
            autoFireRate = weapon.WeaponData.fireRate;
        }

        Debug.Log($"FlexibleFireModule initialized with {availableModes.Count} modes: {string.Join(", ", availableModes)}");
        Debug.Log($"Starting mode: {currentMode}");
    }

    public void OnWeaponActivated()
    {
        isFiring = false;
        lastFireTime = -1f;
        StopAutoFire();
    }

    public void OnWeaponDeactivated()
    {
        isFiring = false;
        StopAutoFire();
    }

    public void OnUpdate() { }

    private void SetupAvailableModes()
    {
        availableModes.Clear();

        if (allowSingle) availableModes.Add(FireMode.Single);
        if (allowBurst) availableModes.Add(FireMode.Burst);
        if (allowAuto) availableModes.Add(FireMode.Auto);

        if (availableModes.Count == 0)
        {
            Debug.LogWarning("No fire modes enabled! Adding Single mode as fallback.");
            availableModes.Add(FireMode.Single);
            allowSingle = true;
        }

        // Set current mode to first available
        currentMode = availableModes[0];
        currentModeIndex = 0;
    }

    public bool CanFire()
    {
        if (weapon?.WeaponData == null) return false;

        // Cannot fire while reloading
        var ammoModule = weapon.GetAmmoModule() as StandardAmmoModule;
        if (ammoModule != null && ammoModule.IsReloading())
        {
            return false;
        }

        bool hasAmmo = weapon.GetAmmoModule()?.GetCurrentAmmo() > 0;
        bool fireRateOK = Time.time >= lastFireTime + GetCurrentFireRate();

        return hasAmmo && fireRateOK && !isFiring;
    }

    public void OnFireInput(bool isPressed, bool wasPressed)
    {
        isFirePressed = isPressed;
        wasFirePressed = wasPressed;

        Debug.Log($"Fire input - Pressed: {isPressed}, WasPressed: {wasPressed}, Mode: {currentMode}");

        // Handle mode switching
        if (Input.GetKeyDown(KeyCode.T))
        {
            SwitchMode();
        }

        // Handle firing based on current mode
        switch (currentMode)
        {
            case FireMode.Single:
                if (wasPressed && CanFire())
                {
                    StartCoroutine(FireSingle());
                }
                break;

            case FireMode.Burst:
                if (wasPressed && CanFire() && !isBursting)
                {
                    StartCoroutine(FireBurst());
                }
                break;

            case FireMode.Auto:
                if (isPressed && CanFire())
                {
                    if (autoFireCoroutine == null)
                    {
                        autoFireCoroutine = StartCoroutine(FireAuto());
                    }
                }
                else if (!isPressed)
                {
                    StopAutoFire();
                }
                break;
        }
    }

    public IEnumerator Fire()
    {
        // This method is called by the interface, but we handle firing in OnFireInput
        yield break;
    }

    private IEnumerator FireSingle()
    {
        Debug.Log("Firing single shot");
        isFiring = true;

        if (FireProjectile())
        {
            lastFireTime = Time.time;
            PlayFireEffects();
        }

        yield return new WaitForSeconds(GetCurrentFireRate());
        isFiring = false;
    }

    private IEnumerator FireBurst()
    {
        Debug.Log($"Starting burst fire - {burstCount} rounds");
        isFiring = true;
        isBursting = true;

        for (int i = 0; i < burstCount; i++)
        {
            var ammoModule = weapon.GetAmmoModule();
            if (ammoModule?.GetCurrentAmmo() <= 0)
            {
                Debug.Log("Burst interrupted - out of ammo");
                break;
            }

            if (FireProjectile())
            {
                PlayFireEffects();
                Debug.Log($"Burst shot {i + 1}/{burstCount}");
            }

            if (i < burstCount - 1) // Don't wait after the last shot
            {
                yield return new WaitForSeconds(burstInterval);
            }
        }

        lastFireTime = Time.time;
        yield return new WaitForSeconds(GetCurrentFireRate());

        isBursting = false;
        isFiring = false;
        Debug.Log("Burst complete");
    }

    private IEnumerator FireAuto()
    {
        Debug.Log("Starting auto fire");
        isFiring = true;

        while (isFirePressed)
        {
            var ammoModule = weapon.GetAmmoModule();
            if (ammoModule?.GetCurrentAmmo() <= 0)
            {
                Debug.Log("Auto fire stopped - out of ammo");
                break;
            }

            if (Time.time >= lastFireTime + autoFireRate)
            {
                if (FireProjectile())
                {
                    PlayFireEffects();
                    lastFireTime = Time.time;
                    Debug.Log("Auto fire shot");
                }
            }

            yield return null; // Wait one frame
        }

        StopAutoFire();
        Debug.Log("Auto fire stopped");
    }

    private void StopAutoFire()
    {
        if (autoFireCoroutine != null)
        {
            StopCoroutine(autoFireCoroutine);
            autoFireCoroutine = null;
        }
        isFiring = false;
    }

    private bool FireProjectile()
    {
        var projectileModule = weapon.GetProjectileModule();
        var targetingModule = weapon.GetTargetingModule();
        var ammoModule = weapon.GetAmmoModule();

        if (projectileModule == null || ammoModule == null)
        {
            Debug.LogError("Missing required modules for firing");
            return false;
        }

        if (!ammoModule.ConsumeAmmo())
        {
            Debug.Log("Failed to consume ammo");
            return false;
        }

        Vector3 baseDirection = weapon.CalculateBaseDirection();
        Vector3 finalDirection = targetingModule?.CalculateDirection(baseDirection) ?? baseDirection;

        GameObject projectile = projectileModule.CreateProjectile(
            weapon.FirePoint.position,
            finalDirection,
            weapon.WeaponData?.bulletVelocity ?? 100f
        );

        return projectile != null;
    }

    private void PlayFireEffects()
    {
        weapon.SetAnimationTrigger("Recoil");
        weapon.PlaySound(weapon.WeaponSound?.shootClip);
    }

    private float GetCurrentFireRate()
    {
        return weapon.WeaponData?.fireRate ?? 0.1f;
    }

    public void SwitchMode()
    {
        if (availableModes.Count <= 1)
        {
            Debug.Log("Only one fire mode available");
            return;
        }

        currentModeIndex = (currentModeIndex + 1) % availableModes.Count;
        currentMode = availableModes[currentModeIndex];

        // Stop any ongoing firing when switching modes
        StopAutoFire();
        isBursting = false;
        isFiring = false;

        Debug.Log($"=== SWITCHED TO MODE: {currentMode} ({currentModeIndex + 1}/{availableModes.Count}) ===");
    }

    public string GetCurrentModeText()
    {
        return $"{currentMode.ToString().ToUpper()} ({currentModeIndex + 1}/{availableModes.Count})";
    }

    // Method to change available modes at runtime
    public void SetAvailableModes(bool single, bool burst, bool auto)
    {
        allowSingle = single;
        allowBurst = burst;
        allowAuto = auto;
        SetupAvailableModes();
    }

    private void OnValidate()
    {
        // Ensure at least one mode is selected
        if (!allowSingle && !allowBurst && !allowAuto)
        {
            allowSingle = true;
        }
    }
}
//end