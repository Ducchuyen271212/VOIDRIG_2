//BurstFireModule.cs - Fixed with mode switching and reload blocking
using UnityEngine;
using System.Collections;

public class BurstFireModule : BaseFireModule
{
    [Header("Mode Settings")]
    public FireMode currentMode = FireMode.Burst;
    private int currentModeIndex = 0;
    private FireMode[] availableModes;

    [Header("Burst Settings")]
    public int burstCount = 3;
    public float burstInterval = 0.1f;
    private bool isBursting = false;

    public override void Initialize(ModularWeapon weapon)
    {
        base.Initialize(weapon);

        // Get available modes from weapon data
        if (weapon.WeaponData?.availableModes != null && weapon.WeaponData.availableModes.Length > 0)
        {
            availableModes = new FireMode[weapon.WeaponData.availableModes.Length];
            for (int i = 0; i < weapon.WeaponData.availableModes.Length; i++)
            {
                availableModes[i] = ConvertToFireMode(weapon.WeaponData.availableModes[i]);
            }
            currentMode = availableModes[0];
            Debug.Log($"Loaded {availableModes.Length} modes from weapon data: {string.Join(", ", availableModes)}");
        }
        else
        {
            // For BurstRifle - only 2 modes as per your GunData
            availableModes = new FireMode[] { FireMode.Burst, FireMode.Single };
            currentMode = FireMode.Burst;
            Debug.Log("Using default BurstRifle modes: Burst, Single");
        }

        if (weapon.WeaponData != null)
        {
            burstCount = weapon.WeaponData.bulletsPerBurst;
            burstInterval = weapon.WeaponData.burstFireInterval;
            Debug.Log($"Burst settings - Count: {burstCount}, Interval: {burstInterval}");
        }

        Debug.Log($"BurstFireModule initialized with mode: {currentMode}");
    }

    public override bool CanFire()
    {
        if (!base.CanFire()) return false;

        // Cannot fire while reloading
        var ammoModule = weapon.GetAmmoModule() as StandardAmmoModule;
        if (ammoModule != null && ammoModule.IsReloading())
        {
            Debug.Log("Cannot fire - weapon is reloading");
            return false;
        }

        return true;
    }

    public override void OnFireInput(bool isPressed, bool wasPressed)
    {
        // Handle mode switching FIRST
        try
        {
            var switchModeAction = weapon.GetComponent<ModularWeapon>().GetType()
                .GetField("switchModeAction", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(weapon.GetComponent<ModularWeapon>()) as UnityEngine.InputSystem.InputAction;

            if (switchModeAction?.WasPressedThisFrame() == true)
            {
                SwitchMode();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Could not access switch mode action: {e.Message}");
        }

        // Handle fire input based on current mode
        isFirePressed = isPressed;
        wasFirePressed = wasPressed;

        if (ShouldFire() && CanFire())
        {
            StartCoroutine(Fire());
        }
    }

    protected override bool ShouldFire()
    {
        switch (currentMode)
        {
            case FireMode.Single:
                return wasFirePressed;
            case FireMode.Auto:
                return isFirePressed;
            case FireMode.Burst:
                return wasFirePressed;
            default:
                return wasFirePressed;
        }
    }

    public override IEnumerator Fire()
    {
        if (!CanFire()) yield break;

        switch (currentMode)
        {
            case FireMode.Single:
                yield return FireSingle();
                break;
            case FireMode.Auto:
                yield return FireAuto();
                break;
            case FireMode.Burst:
                yield return FireBurst();
                break;
        }
    }

    private IEnumerator FireSingle()
    {
        isFiring = true;
        lastFireTime = Time.time;

        var projectileModule = weapon.GetProjectileModule();
        var targetingModule = weapon.GetTargetingModule();
        var ammoModule = weapon.GetAmmoModule();

        if (projectileModule != null && ammoModule != null && ammoModule.ConsumeAmmo())
        {
            Vector3 baseDirection = weapon.CalculateBaseDirection();
            Vector3 finalDirection = targetingModule?.CalculateDirection(baseDirection) ?? baseDirection;

            projectileModule.CreateProjectile(
                weapon.FirePoint.position,
                finalDirection,
                weapon.WeaponData.bulletVelocity
            );

            weapon.SetAnimationTrigger("Recoil");
            weapon.PlaySound(weapon.WeaponSound?.shootClip);
        }

        yield return new WaitForSeconds(weapon.WeaponData?.fireRate ?? 0.3f);
        isFiring = false;
    }

    private IEnumerator FireAuto()
    {
        isFiring = true;

        var projectileModule = weapon.GetProjectileModule();
        var targetingModule = weapon.GetTargetingModule();
        var ammoModule = weapon.GetAmmoModule();

        while (isFirePressed && ammoModule?.GetCurrentAmmo() > 0 && CanFire())
        {
            if (Time.time >= lastFireTime + (weapon.WeaponData?.fireRate ?? 0.1f))
            {
                lastFireTime = Time.time;

                if (ammoModule.ConsumeAmmo())
                {
                    Vector3 baseDirection = weapon.CalculateBaseDirection();
                    Vector3 finalDirection = targetingModule?.CalculateDirection(baseDirection) ?? baseDirection;

                    projectileModule?.CreateProjectile(
                        weapon.FirePoint.position,
                        finalDirection,
                        weapon.WeaponData.bulletVelocity
                    );

                    weapon.SetAnimationTrigger("Recoil");
                    weapon.PlaySound(weapon.WeaponSound?.shootClip);
                }
            }

            yield return null;
        }

        isFiring = false;
    }

    private IEnumerator FireBurst()
    {
        if (isBursting) yield break;

        isFiring = true;
        isBursting = true;
        lastFireTime = Time.time;

        var projectileModule = weapon.GetProjectileModule();
        var targetingModule = weapon.GetTargetingModule();
        var ammoModule = weapon.GetAmmoModule();

        int actualBurstCount = weapon.WeaponData?.bulletsPerBurst ?? burstCount;

        for (int i = 0; i < actualBurstCount && ammoModule?.GetCurrentAmmo() > 0; i++)
        {
            if (ammoModule.ConsumeAmmo())
            {
                Vector3 baseDirection = weapon.CalculateBaseDirection();
                Vector3 finalDirection = targetingModule?.CalculateDirection(baseDirection) ?? baseDirection;

                projectileModule?.CreateProjectile(
                    weapon.FirePoint.position,
                    finalDirection,
                    weapon.WeaponData.bulletVelocity
                );

                weapon.SetAnimationTrigger("Recoil");
                weapon.PlaySound(weapon.WeaponSound?.shootClip);

                if (i < actualBurstCount - 1)
                    yield return new WaitForSeconds(burstInterval);
            }
        }

        yield return new WaitForSeconds(weapon.WeaponData?.fireRate ?? 0.3f);
        isBursting = false;
        isFiring = false;
    }

    public void SwitchMode()
    {
        if (availableModes == null || availableModes.Length <= 1)
        {
            Debug.Log("No other modes available");
            return;
        }

        currentModeIndex = (currentModeIndex + 1) % availableModes.Length;
        currentMode = availableModes[currentModeIndex];

        Debug.Log($"=== MODE SWITCHED TO: {currentMode} ===");

        // You could add UI feedback here
    }

    private FireMode ConvertToFireMode(GunData.ShootingMode shootingMode)
    {
        switch (shootingMode)
        {
            case GunData.ShootingMode.Single: return FireMode.Single;
            case GunData.ShootingMode.Burst: return FireMode.Burst;
            case GunData.ShootingMode.Auto: return FireMode.Auto;
            default: return FireMode.Burst;
        }
    }

    public string GetCurrentModeText()
    {
        return currentMode.ToString().ToUpper();
    }
}
//end