// BaseFireModule.cs - Updated to work with FireModeController
using System.Collections;
using UnityEngine;

public abstract class BaseFireManager : MonoBehaviour, IFireModule
{
    [Header("Fire Settings")]
    public FireMode fireMode = FireMode.Single;
    public float fireRate = 0.3f;

    protected ModularWeapon weapon;
    protected bool isFirePressed = false;
    protected bool wasFirePressed = false;
    protected float lastFireTime = -1f;
    protected bool isFiring = false;

    public virtual void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;
        Debug.Log($"BaseFireManager initialized with {fireMode} mode");
    }

    public virtual void OnWeaponActivated()
    {
        isFiring = false;
        lastFireTime = -1f;
    }

    public virtual void OnWeaponDeactivated()
    {
        isFiring = false;
    }

    public virtual void OnUpdate() { }

    public virtual bool CanFire()
    {
        bool hasAmmo = weapon.GetAmmoModule()?.GetCurrentAmmo() > 0;
        bool fireRateOK = Time.time >= lastFireTime + fireRate;
        return hasAmmo && fireRateOK && !isFiring;
    }

    public virtual void OnFireInput(bool isPressed, bool wasPressed)
    {
        isFirePressed = isPressed;
        wasFirePressed = wasPressed;

        if (ShouldFire() && CanFire())
        {
            StartCoroutine(Fire());
        }
    }

    protected abstract bool ShouldFire();

    public virtual IEnumerator Fire()
    {
        isFiring = true;
        lastFireTime = Time.time;

        // Consume ammo
        var ammoModule = weapon.GetAmmoModule();
        if (ammoModule != null && !ammoModule.ConsumeAmmo(1))
        {
            Debug.Log("Out of ammo!");
            isFiring = false;
            yield break;
        }

        // Fire projectiles
        yield return FireProjectiles();

        // Recovery
        yield return new WaitForSeconds(fireRate);
        isFiring = false;
    }

    protected virtual IEnumerator FireProjectiles()
    {
        var projectileModule = weapon.GetProjectileModule();
        var targetingModule = weapon.GetTargetingModule();

        if (projectileModule == null) yield break;

        // Calculate base direction
        Vector3 baseDirection = weapon.CalculateBaseDirection();

        // Apply targeting modifications
        if (targetingModule != null)
        {
            baseDirection = targetingModule.CalculateDirection(baseDirection);
        }

        // Create projectile
        projectileModule.CreateProjectile(weapon.FirePoint.position, baseDirection, 100f);

        yield return null;
    }
}
// end