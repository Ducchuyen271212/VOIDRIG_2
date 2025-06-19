// ShotgunFireModule.cs - With Multiple Spread Patterns
using System.Collections;
using UnityEngine;

public class ShotgunFireModule : MonoBehaviour, IFireModule
{
    [Header("Shotgun Settings")]
    public int pelletCount = 8;
    public float spreadAngle = 15f;
    public SpreadPattern spreadPattern = SpreadPattern.Horizontal;
    public float bulletLifetimeOverride = -1f; // -1 = use weapon data, otherwise override

    public enum SpreadPattern
    {
        Horizontal,    // Spread more horizontally (realistic shotgun)
        Circular,      // Standard circular spread
        Vertical,      // Spread more vertically
        Star,          // Evenly distributed star pattern
        Random         // Completely random spread
    }

    private ModularWeapon weapon;
    private bool readyToShoot = true;
    private float lastShotTime;
    private bool isFiring = false;

    public void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;
    }

    public void OnWeaponActivated()
    {
        readyToShoot = true;
        isFiring = false;
    }

    public void OnWeaponDeactivated()
    {
        isFiring = false;
    }

    public void OnUpdate() { }

    public void OnFireInput(bool isPressed, bool wasPressed)
    {
        // Double check weapon is active before processing any input
        if (!weapon.isActiveWeapon)
        {
            return;
        }

        if (wasPressed && CanFire())
        {
            StartCoroutine(Fire());
        }
    }

    public bool CanFire()
    {
        // Must be active weapon to fire
        if (!weapon.isActiveWeapon)
        {
            return false;
        }

        if (weapon?.WeaponData == null)
        {
            return false;
        }

        var projectileModule = weapon.GetProjectileModule();
        if (projectileModule == null)
        {
            return false;
        }

        var ammoModule = weapon.GetAmmoModule();

        // Check if reloading
        if (ammoModule != null)
        {
            var standardAmmo = ammoModule as StandardAmmoModule;
            if (standardAmmo != null && standardAmmo.IsReloading())
            {
                return false;
            }

            if (ammoModule.GetCurrentAmmo() <= 0)
            {
                return false;
            }
        }

        float fireCooldown = weapon.WeaponData.fireRate;
        bool canFireByRate = Time.time - lastShotTime >= fireCooldown;

        return readyToShoot && canFireByRate && !isFiring;
    }

    public IEnumerator Fire()
    {
        if (!CanFire()) yield break;

        isFiring = true;
        readyToShoot = false;
        lastShotTime = Time.time;

        var projectileModule = weapon.GetProjectileModule();
        var targetingModule = weapon.GetTargetingModule();
        var ammoModule = weapon.GetAmmoModule();

        // Check if we have ammo
        if (ammoModule != null && ammoModule.GetCurrentAmmo() <= 0)
        {
            // Play empty sound
            if (weapon.WeaponSound?.emptyClip != null)
            {
                weapon.PlaySound(weapon.WeaponSound.emptyClip);
            }
            weapon.SetAnimationTrigger("RecoilRecover");
            isFiring = false;
            readyToShoot = true;
            yield break;
        }

        if (projectileModule != null)
        {
            // Auto-setup bullet prefab if needed
            SetupBulletPrefab(projectileModule);

            Vector3 baseDirection = weapon.CalculateBaseDirection();
            int actualPellets = pelletCount; // Use our pellet count

            Debug.Log($"Firing shotgun with {actualPellets} pellets, pattern: {spreadPattern}");

            // Fire all pellets at once (like your original system)
            for (int i = 0; i < actualPellets; i++)
            {
                Vector3 spreadDirection = CalculateSpreadDirection(baseDirection, i, actualPellets);
                Vector3 finalDirection = targetingModule?.CalculateDirection(spreadDirection) ?? spreadDirection;

                try
                {
                    GameObject projectile = projectileModule.CreateProjectile(
                        weapon.FirePoint.position,
                        finalDirection,
                        weapon.WeaponData.bulletVelocity
                    );

                    if (projectile != null)
                    {
                        // Override bullet lifetime if specified
                        if (bulletLifetimeOverride > 0)
                        {
                            // Destroy any existing destroy timer and set our own
                            var existingDestroy = projectile.GetComponent<DestroyAfterTime>();
                            if (existingDestroy != null)
                            {
                                Destroy(existingDestroy);
                            }

                            Destroy(projectile, bulletLifetimeOverride);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to create pellet {i + 1}!");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error creating pellet {i + 1}: {e.Message}");
                }
            }

            // Consume ammo (single shot = multiple pellets)
            if (ammoModule != null)
            {
                ammoModule.ConsumeAmmo(1);
                Debug.Log($"Shotgun fired! Remaining ammo: {ammoModule.GetCurrentAmmo()}");
            }

            // Play effects
            weapon.SetAnimationTrigger("Recoil");

            if (weapon.WeaponSound?.shootClip != null)
            {
                weapon.PlaySound(weapon.WeaponSound.shootClip);
            }
        }
        else
        {
            Debug.LogError("Cannot fire - no projectile module found!");
        }

        // Wait for cooldown
        float cooldown = weapon.WeaponData?.fireRate ?? 0.8f;
        yield return new WaitForSeconds(cooldown);

        // Recovery
        weapon.SetAnimationTrigger("RecoilRecover");
        readyToShoot = true;
        isFiring = false;
    }

    private void SetupBulletPrefab(IProjectileModule projectileModule)
    {
        try
        {
            var projectileType = projectileModule.GetType();

            // Try to find and set bullet prefab if missing
            var bulletField = projectileType.GetField("bulletPrefab") ?? projectileType.GetField("projectilePrefab");
            if (bulletField != null)
            {
                var currentPrefab = bulletField.GetValue(projectileModule);
                if (currentPrefab == null)
                {
                    var weaponScript = weapon.GetComponent<Weapon>();
                    if (weaponScript?.bulletPrefab != null)
                    {
                        bulletField.SetValue(projectileModule, weaponScript.bulletPrefab);
                        Debug.Log($"Auto-assigned bullet prefab: {weaponScript.bulletPrefab.name}");
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Could not setup bullet prefab: {e.Message}");
        }
    }

    private Vector3 CalculateSpreadDirection(Vector3 baseDirection, int pelletIndex, int totalPellets)
    {
        float actualSpread = weapon.WeaponData?.spreadIntensity ?? spreadAngle;

        // Apply aiming accuracy if available
        if (AimingManager.Instance != null && AimingManager.Instance.isAiming)
        {
            actualSpread *= AimingManager.Instance.GetAccuracyMultiplier();
        }

        Vector2 spreadOffset = Vector2.zero;

        switch (spreadPattern)
        {
            case SpreadPattern.Horizontal:
                spreadOffset = GenerateHorizontalSpread(pelletIndex, totalPellets, actualSpread);
                break;
            case SpreadPattern.Circular:
                spreadOffset = GenerateCircularSpread(pelletIndex, totalPellets, actualSpread);
                break;
            case SpreadPattern.Vertical:
                spreadOffset = GenerateVerticalSpread(pelletIndex, totalPellets, actualSpread);
                break;
            case SpreadPattern.Star:
                spreadOffset = GenerateStarSpread(pelletIndex, totalPellets, actualSpread);
                break;
            case SpreadPattern.Random:
                spreadOffset = GenerateRandomSpread(actualSpread);
                break;
        }

        // Convert spread offset to world direction (distance-based spread)
        return ApplySpreadToDirection(baseDirection, spreadOffset);
    }

    private Vector2 GenerateHorizontalSpread(int index, int total, float spread)
    {
        // Create horizontal line with some vertical variation
        float horizontalPosition = Mathf.Lerp(-spread, spread, (float)index / (total - 1));
        float verticalVariation = Random.Range(-spread * 0.3f, spread * 0.3f);

        return new Vector2(horizontalPosition, verticalVariation);
    }

    private Vector2 GenerateCircularSpread(int index, int total, float spread)
    {
        float angle = (360f / total) * index + Random.Range(-30f, 30f);
        float distance = Random.Range(0.2f, 1f) * spread;

        return new Vector2(
            Mathf.Cos(angle * Mathf.Deg2Rad) * distance,
            Mathf.Sin(angle * Mathf.Deg2Rad) * distance
        );
    }

    private Vector2 GenerateVerticalSpread(int index, int total, float spread)
    {
        // Create vertical line with some horizontal variation
        float verticalPosition = Mathf.Lerp(-spread, spread, (float)index / (total - 1));
        float horizontalVariation = Random.Range(-spread * 0.3f, spread * 0.3f);

        return new Vector2(horizontalVariation, verticalPosition);
    }

    private Vector2 GenerateStarSpread(int index, int total, float spread)
    {
        float angle = (360f / total) * index;
        float distance = spread * Random.Range(0.7f, 1f);

        return new Vector2(
            Mathf.Cos(angle * Mathf.Deg2Rad) * distance,
            Mathf.Sin(angle * Mathf.Deg2Rad) * distance
        );
    }

    private Vector2 GenerateRandomSpread(float spread)
    {
        return Random.insideUnitCircle * spread;
    }

    private Vector3 ApplySpreadToDirection(Vector3 baseDirection, Vector2 spreadOffset)
    {
        // Use degrees for spread (like your original system)
        float horizontalAngle = spreadOffset.x; // degrees
        float verticalAngle = spreadOffset.y;   // degrees

        // Create rotations around world axes for distance-based spread
        Quaternion horizontalRotation = Quaternion.AngleAxis(horizontalAngle, Vector3.up);
        Quaternion verticalRotation = Quaternion.AngleAxis(verticalAngle, Vector3.right);

        // Apply rotations to base direction
        Vector3 spreadDirection = horizontalRotation * verticalRotation * baseDirection;

        return spreadDirection.normalized;
    }

    // Helper method to set bullet lifetime
    public void SetBulletLifetime(float lifetime)
    {
        bulletLifetimeOverride = lifetime;
    }

    // Preset methods for quick setup
    [ContextMenu("Preset: Realistic Shotgun")]
    public void PresetRealisticShotgun()
    {
        pelletCount = 8;
        spreadAngle = 12f;
        spreadPattern = SpreadPattern.Horizontal;
        bulletLifetimeOverride = 1f;
    }

    [ContextMenu("Preset: Wide Spread")]
    public void PresetWideSpread()
    {
        pelletCount = 12;
        spreadAngle = 25f;
        spreadPattern = SpreadPattern.Circular;
        bulletLifetimeOverride = 0.8f;
    }

    [ContextMenu("Preset: Tight Choke")]
    public void PresetTightChoke()
    {
        pelletCount = 6;
        spreadAngle = 8f;
        spreadPattern = SpreadPattern.Star;
        bulletLifetimeOverride = 1.5f;
    }
}

// Helper component for manual bullet lifetime control
public class DestroyAfterTime : MonoBehaviour
{
    public float lifetime = 5f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }
}
// end