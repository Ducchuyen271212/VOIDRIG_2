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
        DebugLog("BurstFireModule initialized and ready to fire");
    }

    public void OnWeaponActivated()
    {
        isFiring = false;
        isBursting = false;
        DebugLog("Burst fire module activated - READY TO FIRE");
    }

    public void OnWeaponDeactivated()
    {
        isFiring = false;
        isBursting = false;
        StopAllCoroutines();
        DebugLog("Burst fire module deactivated");
    }

    public void OnUpdate()
    {
        // Check if weapon is still active
        if (weapon != null && !weapon.isActiveWeapon)
        {
            return; // Don't process input if weapon isn't active
        }

        // Handle input directly in Update for more reliable input detection
        HandleDirectInput();
    }

    private void HandleDirectInput()
    {
        // Get input directly
        bool currentFirePressed = Input.GetMouseButton(0);
        bool currentFireWasPressed = Input.GetMouseButtonDown(0);
        bool dropPressed = Input.GetKeyDown(KeyCode.G);
        bool switchModePressed = Input.GetKeyDown(KeyCode.T);

        // Log input for debugging
        if (currentFireWasPressed)
        {
            DebugLog("FIRE INPUT DETECTED! Attempting to fire...");
        }

        if (dropPressed)
        {
            DebugLog("DROP INPUT DETECTED! (Drop should be handled by weapon system)");
        }

        if (switchModePressed)
        {
            DebugLog("MODE SWITCH INPUT DETECTED!");
        }

        // Fire logic
        if (currentFireWasPressed && CanFire() && !isBursting)
        {
            DebugLog("STARTING BURST FIRE!");
            StartCoroutine(Fire());
        }
    }

    public bool CanFire()
    {
        if (weapon == null)
        {
            DebugLog("Cannot fire - weapon is null");
            return false;
        }

        if (!weapon.isActiveWeapon)
        {
            DebugLog("Cannot fire - weapon is not active");
            return false;
        }

        // Cannot fire while reloading
        var ammoModule = weapon.GetAmmoModule() as StandardAmmoModule;
        if (ammoModule != null && ammoModule.IsReloading())
        {
            DebugLog("Cannot fire - weapon is reloading");
            return false;
        }

        bool hasAmmo = weapon.GetAmmoModule()?.GetCurrentAmmo() > 0;
        if (!hasAmmo)
        {
            DebugLog("Cannot fire - no ammo");
            return false;
        }

        bool fireRateOK = Time.time >= lastFireTime + burstCooldown;
        if (!fireRateOK)
        {
            DebugLog("Cannot fire - fire rate cooldown");
            return false;
        }

        if (isFiring)
        {
            DebugLog("Cannot fire - already firing");
            return false;
        }

        DebugLog("CAN FIRE - All conditions met!");
        return true;
    }

    public void OnFireInput(bool isPressed, bool wasPressed)
    {
        // Keep this for interface compatibility, but we handle input in Update
        isFirePressed = isPressed;
        wasFirePressed = wasPressed;

        DebugLog($"OnFireInput called - WasPressed: {wasPressed}, CanFire: {CanFire()}");
    }

    public IEnumerator Fire()
    {
        if (!CanFire())
        {
            DebugLog("Fire() called but CanFire() returned false");
            yield break;
        }

        isFiring = true;
        isBursting = true;
        lastFireTime = Time.time;

        var projectileModule = weapon.GetProjectileModule();
        var targetingModule = weapon.GetTargetingModule();
        var ammoModule = weapon.GetAmmoModule();

        DebugLog($"Starting burst fire - {burstCount} shots, Projectile Module: {projectileModule != null}, Ammo Module: {ammoModule != null}");

        if (projectileModule == null)
        {
            DebugLog("ERROR: No projectile module found!");
            isBursting = false;
            isFiring = false;
            yield break;
        }

        if (ammoModule == null)
        {
            DebugLog("ERROR: No ammo module found!");
            isBursting = false;
            isFiring = false;
            yield break;
        }

        for (int i = 0; i < burstCount && ammoModule.GetCurrentAmmo() > 0; i++)
        {
            DebugLog($"Firing shot {i + 1}/{burstCount}");

            if (ammoModule.ConsumeAmmo())
            {
                Vector3 baseDirection = weapon.CalculateBaseDirection();
                Vector3 finalDirection = targetingModule?.CalculateDirection(baseDirection) ?? baseDirection;

                DebugLog($"Creating projectile - Position: {weapon.FirePoint.position}, Direction: {finalDirection}");

                GameObject projectile = projectileModule.CreateProjectile(
                    weapon.FirePoint.position,
                    finalDirection,
                    100f // Default velocity
                );

                if (projectile != null)
                {
                    DebugLog($"Projectile created successfully: {projectile.name}");
                }
                else
                {
                    DebugLog("ERROR: Projectile creation failed!");
                }

                DebugLog($"Burst shot {i + 1}/{burstCount} fired successfully");

                if (i < burstCount - 1)
                {
                    yield return new WaitForSeconds(burstInterval);
                }
            }
            else
            {
                DebugLog($"Failed to consume ammo for shot {i + 1}");
                break;
            }
        }

        DebugLog("Burst fire complete - waiting for cooldown");
        yield return new WaitForSeconds(burstCooldown);

        isBursting = false;
        isFiring = false;
        DebugLog("Burst fire cooldown complete - ready to fire again");
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[BurstFireModule] {message}");
    }
}
// end