// BurstFireModule.cs
using UnityEngine;
using System.Collections;

public class BurstFireModule : MonoBehaviour, IFireModule
{
    [Header("Burst Settings")]
    public int burstCount = 3;
    public float burstInterval = 0.1f;
    public float burstCooldown = 0.3f;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    private ModularWeapon weapon;
    private bool isFirePressed = false;
    private bool wasFirePressed = false;
    private float lastFireTime = -1f;
    private bool isFiring = false;
    private bool isBursting = false;

    public void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;
        DebugLog("BurstFireModule initialized");
    }

    public void OnWeaponActivated()
    {
        isFiring = false;
        isBursting = false;
        DebugLog("Burst fire module activated");
    }

    public void OnWeaponDeactivated()
    {
        isFiring = false;
        isBursting = false;
        StopAllCoroutines();
        DebugLog("Burst fire module deactivated");
    }

    public void OnUpdate() { }

    public bool CanFire()
    {
        // Cannot fire while reloading
        var ammoModule = weapon.GetAmmoModule() as StandardAmmoModule;
        if (ammoModule != null && ammoModule.IsReloading())
        {
            DebugLog("Cannot fire - weapon is reloading");
            return false;
        }

        bool hasAmmo = weapon.GetAmmoModule()?.GetCurrentAmmo() > 0;
        bool fireRateOK = Time.time >= lastFireTime + burstCooldown;

        return hasAmmo && fireRateOK && !isFiring;
    }

    public void OnFireInput(bool isPressed, bool wasPressed)
    {
        isFirePressed = isPressed;
        wasFirePressed = wasPressed;

        DebugLog($"Fire input - WasPressed: {wasPressed}, CanFire: {CanFire()}, IsBursting: {isBursting}");

        if (wasFirePressed && CanFire() && !isBursting)
        {
            StartCoroutine(Fire());
        }
    }

    public IEnumerator Fire()
    {
        if (!CanFire()) yield break;

        isFiring = true;
        isBursting = true;
        lastFireTime = Time.time;

        var projectileModule = weapon.GetProjectileModule();
        var targetingModule = weapon.GetTargetingModule();
        var ammoModule = weapon.GetAmmoModule();

        DebugLog($"Starting burst fire - {burstCount} shots");

        for (int i = 0; i < burstCount && ammoModule?.GetCurrentAmmo() > 0; i++)
        {
            if (ammoModule.ConsumeAmmo())
            {
                Vector3 baseDirection = weapon.CalculateBaseDirection();
                Vector3 finalDirection = targetingModule?.CalculateDirection(baseDirection) ?? baseDirection;

                projectileModule?.CreateProjectile(
                    weapon.FirePoint.position,
                    finalDirection,
                    100f // Default velocity
                );

                DebugLog($"Burst shot {i + 1}/{burstCount}");

                if (i < burstCount - 1)
                    yield return new WaitForSeconds(burstInterval);
            }
        }

        yield return new WaitForSeconds(burstCooldown);
        isBursting = false;
        isFiring = false;
        DebugLog("Burst complete");
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[BurstFireModule] {message}");
    }
}
// end