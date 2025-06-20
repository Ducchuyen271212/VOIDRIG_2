// ScatterFireModule.cs
using System.Collections;
using UnityEngine;

public class ScatterFireModule : MonoBehaviour, IFireModule
{
    [Header("Scatter Settings")]
    public int pelletCount = 8;
    public float spreadAngle = 15f;
    public float fireRate = 0.5f;

    private ModularWeapon weapon;
    private bool readyToShoot = true;
    private float lastShotTime;
    private bool isFiring = false;

    public void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;
        Debug.Log("ScatterFireModule initialized");
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
        var projectileModule = weapon.GetProjectileModule();
        if (projectileModule == null) return false;

        bool canFireByRate = Time.time - lastShotTime >= fireRate;
        var ammoModule = weapon.GetAmmoModule();
        bool hasAmmo = ammoModule == null || ammoModule.GetCurrentAmmo() > 0;

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

        if (ammoModule != null && ammoModule.GetCurrentAmmo() <= 0)
        {
            Debug.Log("Out of ammo!");
            isFiring = false;
            readyToShoot = true;
            yield break;
        }

        if (projectileModule != null && ammoModule != null)
        {
            Vector3 baseDirection = weapon.CalculateBaseDirection();

            // Fire all pellets
            for (int i = 0; i < pelletCount; i++)
            {
                Vector3 spreadDirection = CalculateSpreadDirection(baseDirection);
                Vector3 finalDirection = targetingModule?.CalculateDirection(spreadDirection) ?? spreadDirection;

                GameObject projectile = projectileModule.CreateProjectile(
                    weapon.FirePoint.position,
                    finalDirection,
                    100f
                );
            }

            bool ammoConsumed = ammoModule.ConsumeAmmo(1);
        }

        yield return new WaitForSeconds(fireRate);
        readyToShoot = true;
        isFiring = false;
    }

    private Vector3 CalculateSpreadDirection(Vector3 baseDirection)
    {
        Quaternion spreadRotation = Quaternion.Euler(
            Random.Range(-spreadAngle, spreadAngle),
            Random.Range(-spreadAngle, spreadAngle),
            0
        );

        return spreadRotation * baseDirection;
    }
}

// end