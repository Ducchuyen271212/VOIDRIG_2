// FlexibleFireModule.cs - Complete updated version with InputSystem
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FlexibleFireModule : MonoBehaviour, IFireModule
{
    [Header("Available Fire Modes")]
    public bool allowSingle = true;
    public bool allowBurst = true;
    public bool allowAuto = false;

    [Header("Current Settings")]
    [SerializeField] private FireMode currentMode = FireMode.Single;
    [SerializeField] private int currentModeIndex = 0;

    [Header("Fire Rate Settings")]
    public float singleFireRate = 0.3f;
    public float autoFireRate = 0.1f;
    public float burstCooldown = 0.5f;

    [Header("Burst Settings")]
    public int burstCount = 3;
    public float burstInterval = 0.1f;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    private ModularWeapon weapon;
    private List<FireMode> availableModes = new List<FireMode>();
    private bool isFirePressed = false;
    private bool wasFirePressed = false;
    private float lastFireTime = -1f;
    private bool isFiring = false;
    private bool isBursting = false;
    private Coroutine fireCoroutine;

    public void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;
        SetupAvailableModes();
        DebugLog($"FlexibleFireModule initialized with {availableModes.Count} modes - Current: {currentMode}");
    }

    private void SetupAvailableModes()
    {
        availableModes.Clear();

        if (allowSingle) availableModes.Add(FireMode.Single);
        if (allowBurst) availableModes.Add(FireMode.Burst);
        if (allowAuto) availableModes.Add(FireMode.Auto);

        if (availableModes.Count == 0)
        {
            availableModes.Add(FireMode.Single);
            allowSingle = true;
        }

        currentMode = availableModes[0];
        currentModeIndex = 0;
    }

    public void OnWeaponActivated()
    {
        StopAllFiring();
        DebugLog($"Activated in {currentMode} mode");
    }

    public void OnWeaponDeactivated()
    {
        StopAllFiring();
    }

    public void OnUpdate()
    {
        // Handle mode switching via InputSystem - check the weapon's switchModeAction
        if (weapon.PlayerInputRef?.actions != null)
        {
            var switchAction = weapon.PlayerInputRef.actions["SwitchMode"];
            if (switchAction?.WasPressedThisFrame() == true)
            {
                SwitchMode();
            }
        }
    }

    public bool CanFire()
    {
        bool hasAmmo = weapon.GetAmmoModule()?.GetCurrentAmmo() > 0;
        bool fireRateOK = Time.time >= lastFireTime + GetCurrentFireRate();
        return hasAmmo && fireRateOK && !isFiring;
    }

    public void OnFireInput(bool isPressed, bool wasPressed)
    {
        isFirePressed = isPressed;
        wasFirePressed = wasPressed;

        if (wasPressed)
        {
            DebugLog($"Fire input received - Mode: {currentMode}, CanFire: {CanFire()}");
        }

        switch (currentMode)
        {
            case FireMode.Single:
                if (wasPressed && CanFire())
                    StartCoroutine(FireSingle());
                break;

            case FireMode.Burst:
                if (wasPressed && CanFire() && !isBursting)
                    StartCoroutine(FireBurst());
                break;

            case FireMode.Auto:
                if (isPressed && CanFire())
                {
                    if (fireCoroutine == null)
                        fireCoroutine = StartCoroutine(FireAuto());
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
        yield break; // Not used - specific fire methods handle this
    }

    private IEnumerator FireSingle()
    {
        isFiring = true;

        if (FireProjectile())
        {
            lastFireTime = Time.time;
            DebugLog("Single shot fired");
        }

        yield return new WaitForSeconds(singleFireRate);
        isFiring = false;
    }

    private IEnumerator FireBurst()
    {
        isFiring = true;
        isBursting = true;

        int shotsToFire = Mathf.Min(burstCount, weapon.GetAmmoModule().GetCurrentAmmo());

        for (int i = 0; i < shotsToFire; i++)
        {
            if (weapon.GetAmmoModule().GetCurrentAmmo() <= 0) break;

            if (FireProjectile())
            {
                DebugLog($"Burst shot {i + 1}/{burstCount} fired");
            }

            if (i < shotsToFire - 1)
                yield return new WaitForSeconds(burstInterval);
        }

        lastFireTime = Time.time;
        yield return new WaitForSeconds(burstCooldown);

        isBursting = false;
        isFiring = false;
        DebugLog("Burst complete");
    }

    private IEnumerator FireAuto()
    {
        isFiring = true;
        DebugLog("Auto fire started");

        while (isFirePressed && weapon.GetAmmoModule().GetCurrentAmmo() > 0)
        {
            if (Time.time >= lastFireTime + autoFireRate)
            {
                if (FireProjectile())
                {
                    lastFireTime = Time.time;
                }
            }
            yield return null;
        }

        StopAutoFire();
        DebugLog("Auto fire stopped");
    }

    private bool FireProjectile()
    {
        var projectileModule = weapon.GetProjectileModule();
        var targetingModule = weapon.GetTargetingModule();
        var ammoModule = weapon.GetAmmoModule();

        if (!ammoModule.ConsumeAmmo(1)) return false;

        Vector3 baseDirection = weapon.CalculateBaseDirection();
        Vector3 finalDirection = targetingModule?.CalculateDirection(baseDirection) ?? baseDirection;

        projectileModule?.CreateProjectile(
            weapon.FirePoint.position,
            finalDirection,
            100f
        );

        return true;
    }

    private float GetCurrentFireRate()
    {
        switch (currentMode)
        {
            case FireMode.Single: return singleFireRate;
            case FireMode.Auto: return autoFireRate;
            case FireMode.Burst: return burstCooldown;
            default: return singleFireRate;
        }
    }

    private void StopAutoFire()
    {
        if (fireCoroutine != null)
        {
            StopCoroutine(fireCoroutine);
            fireCoroutine = null;
        }
        isFiring = false;
    }

    private void StopAllFiring()
    {
        StopAutoFire();
        isBursting = false;
        isFiring = false;
    }

    public void SwitchMode()
    {
        if (availableModes.Count <= 1) return;

        StopAllFiring();

        currentModeIndex = (currentModeIndex + 1) % availableModes.Count;
        currentMode = availableModes[currentModeIndex];

        DebugLog($"Switched to {currentMode} mode");
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[FlexibleFireModule] {message}");
    }

    // Public getters for debugging
    public string GetCurrentModeName() => currentMode.ToString();
    public int GetCurrentModeIndex() => currentModeIndex;
}
// end