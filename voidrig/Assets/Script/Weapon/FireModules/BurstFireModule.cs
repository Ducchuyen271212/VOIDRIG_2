// BurstFireModule.cs
using UnityEngine;
using System.Collections;

public class BurstFireModule : MonoBehaviour, IFireModule
{
    [Header("Burst Settings")]
    [Tooltip("Number of shots per burst")]
    public int burstCount = 3;
    [Tooltip("Time between shots in a burst")]
    public float burstInterval = 0.1f;
    [Tooltip("Time before next burst can be fired")]
    public float burstCooldown = 0.3f;

    private ModularWeapon weapon;
    private bool isFiring = false;
    private float lastFireTime = -1f;

    public void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;
        Debug.Log($"BurstFireModule ready - Burst Count: {burstCount}");
    }

    public void OnWeaponActivated()
    {
        isFiring = false;
        Debug.Log($"BurstFireModule activated - Will fire {burstCount} shots per burst");
    }

    public void OnWeaponDeactivated()
    {
        isFiring = false;
        StopAllCoroutines();
    }

    public void OnUpdate() { }

    public bool CanFire()
    {
        if (!weapon.isActiveWeapon) return false;
        if (weapon.GetAmmoModule()?.GetCurrentAmmo() <= 0) return false;
        if (Time.time < lastFireTime + burstCooldown) return false;
        if (isFiring) return false;
        return true;
    }

    public void OnFireInput(bool isPressed, bool wasPressed)
    {
        if (wasPressed)
        {
            Debug.Log($"BurstFireModule received fire input - Burst Count: {burstCount}");
            Debug.Log($"CanFire: {CanFire()}");
            Debug.Log($"isFiring: {isFiring}");
        }

        if (wasPressed && CanFire())
        {
            Debug.Log($"=== STARTING BURST FIRE ({burstCount} shots)! ===");
            StartCoroutine(Fire());
        }
    }

    public IEnumerator Fire()
    {
        isFiring = true;
        lastFireTime = Time.time;

        var projectileModule = weapon.GetProjectileModule();
        var targetingModule = weapon.GetTargetingModule();
        var ammoModule = weapon.GetAmmoModule();

        // Fire the configured number of shots
        for (int i = 0; i < burstCount && ammoModule.GetCurrentAmmo() > 0; i++)
        {
            if (ammoModule.ConsumeAmmo())
            {
                Vector3 baseDirection = weapon.CalculateBaseDirection();
                Vector3 finalDirection = targetingModule?.CalculateDirection(baseDirection) ?? baseDirection;

                projectileModule?.CreateProjectile(
                    weapon.FirePoint.position,
                    finalDirection,
                    100f
                );

                Debug.Log($"Burst shot {i + 1}/{burstCount} fired");

                // Add delay between shots (except after last shot)
                if (i < burstCount - 1)
                    yield return new WaitForSeconds(burstInterval);
            }
            else
            {
                Debug.Log($"Out of ammo at shot {i + 1}");
                break;
            }
        }

        // Wait for cooldown before allowing next burst
        yield return new WaitForSeconds(burstCooldown);
        isFiring = false;
        Debug.Log($"Burst complete - Ready for next burst");
    }

    // Preset configurations
    [ContextMenu("Preset: 3-Round Burst")]
    public void Preset3RoundBurst()
    {
        burstCount = 3;
        burstInterval = 0.1f;
        burstCooldown = 0.3f;
    }

    [ContextMenu("Preset: 5-Round Burst")]
    public void Preset5RoundBurst()
    {
        burstCount = 5;
        burstInterval = 0.08f;
        burstCooldown = 0.4f;
    }

    [ContextMenu("Preset: 2-Round Burst")]
    public void Preset2RoundBurst()
    {
        burstCount = 2;
        burstInterval = 0.05f;
        burstCooldown = 0.2f;
    }
}
// end