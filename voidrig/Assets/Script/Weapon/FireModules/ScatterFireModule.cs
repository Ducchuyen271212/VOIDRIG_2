// ScatterFireModule.cs - Fixed Version
using System.Collections;
using UnityEngine;

public class ScatterFireModule : MonoBehaviour, IFireModule
{
    [Header("Scatter Settings")]
    public int pelletCount = 8;
    public float spreadAngle = 15f;

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
        if (wasPressed && CanFire())
        {
            StartCoroutine(Fire());
        }
    }

    public bool CanFire()
    {
        if (weapon?.WeaponData == null)
        {
            Debug.Log("Cannot fire - no weapon data");
            return false;
        }

        var projectileModule = weapon.GetProjectileModule();
        if (projectileModule == null)
        {
            Debug.LogError("Cannot fire - no projectile module found!");
            return false;
        }

        float fireCooldown = weapon.WeaponData.fireRate;
        bool canFireByRate = Time.time - lastShotTime >= fireCooldown;

        var ammoModule = weapon.GetAmmoModule();
        bool hasAmmo = ammoModule == null || ammoModule.GetCurrentAmmo() > 0;

        if (!canFireByRate) Debug.Log("Cannot fire - fire rate cooldown");
        if (!hasAmmo) Debug.Log($"Cannot fire - no ammo (current: {ammoModule?.GetCurrentAmmo()})");
        if (isFiring) Debug.Log("Cannot fire - already firing");
        if (!readyToShoot) Debug.Log("Cannot fire - not ready to shoot");

        return readyToShoot && canFireByRate && hasAmmo && !isFiring;
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

        if (projectileModule != null && ammoModule != null)
        {
            Vector3 baseDirection = weapon.CalculateBaseDirection();
            int actualPellets = weapon.WeaponData?.bulletsPerBurst ?? pelletCount;

            Debug.Log($"Firing {actualPellets} pellets, projectile module: {projectileModule.GetType().Name}");

            // Check if projectile module can create projectiles
            var physicalProjectile = projectileModule as PhysicalProjectileModule;
            if (physicalProjectile != null && physicalProjectile.projectilePrefab == null)
            {
                Debug.LogError("PhysicalProjectileModule has no projectile prefab assigned! Trying to find bullet prefab...");

                // Try to find a bullet prefab in the weapon
                var bulletPrefab = weapon.GetComponent<Weapon>()?.bulletPrefab;
                if (bulletPrefab != null)
                {
                    physicalProjectile.projectilePrefab = bulletPrefab;
                    Debug.Log($"Found bullet prefab: {bulletPrefab.name}");
                }
                else
                {
                    Debug.LogError("No bullet prefab found! Cannot fire.");
                    isFiring = false;
                    readyToShoot = true;
                    yield break; // Use yield break instead of return in coroutine
                }
            }

            // Fire all pellets
            for (int i = 0; i < actualPellets; i++)
            {
                Vector3 spreadDirection = CalculateSpreadDirection(baseDirection);
                Vector3 finalDirection = targetingModule?.CalculateDirection(spreadDirection) ?? spreadDirection;

                GameObject projectile = projectileModule.CreateProjectile(
                    weapon.FirePoint.position,
                    finalDirection,
                    weapon.WeaponData.bulletVelocity
                );

                if (projectile == null)
                {
                    Debug.LogError($"Failed to create projectile {i}!");
                }
                else
                {
                    Debug.Log($"Created projectile {i}: {projectile.name}");
                }
            }

            // Consume one shot worth of ammo
            bool ammoConsumed = ammoModule.ConsumeAmmo(1);
            Debug.Log($"Ammo consumed: {ammoConsumed}, remaining: {ammoModule.GetCurrentAmmo()}");

            // Play effects
            weapon.SetAnimationTrigger("Recoil");
            weapon.PlaySound(weapon.WeaponSound?.shootClip);

            // Create muzzle flash if available
            if (weapon.WeaponData != null)
            {
                // Look for muzzle effect on weapon
                Transform muzzle = weapon.FirePoint.Find("MuzzleEffect");
                if (muzzle != null)
                {
                    ParticleSystem muzzlePS = muzzle.GetComponent<ParticleSystem>();
                    muzzlePS?.Play();
                }
            }
        }
        else
        {
            Debug.LogError($"Missing modules - Projectile: {projectileModule != null}, Ammo: {ammoModule != null}");
        }

        // Wait for fire cooldown
        float cooldown = weapon.WeaponData?.fireRate ?? 0.5f;
        yield return new WaitForSeconds(cooldown);

        // Recovery
        weapon.SetAnimationTrigger("RecoilRecover");
        readyToShoot = true;
        isFiring = false;
    }

    private Vector3 CalculateSpreadDirection(Vector3 baseDirection)
    {
        float actualSpread = weapon.WeaponData?.spreadIntensity ?? spreadAngle;

        // Apply aiming accuracy if available
        if (AimingManager.Instance != null && AimingManager.Instance.isAiming)
        {
            actualSpread *= AimingManager.Instance.GetAccuracyMultiplier();
        }

        Quaternion spreadRotation = Quaternion.Euler(
            Random.Range(-actualSpread, actualSpread),
            Random.Range(-actualSpread, actualSpread),
            0
        );

        return spreadRotation * baseDirection;
    }
}
// end