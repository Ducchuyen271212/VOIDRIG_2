// BaseFireModule.cs
using System.Collections;
using UnityEngine;

public abstract class BaseFireModule : MonoBehaviour, IFireModule
{
    [Header("Fire Settings")]
    public FireMode fireMode = FireMode.Single;

    protected ModularWeapon weapon;
    protected bool isFirePressed = false;
    protected bool wasFirePressed = false;
    protected float lastFireTime = -1f;
    protected bool isFiring = false;

    public virtual void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;
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
        if (weapon.WeaponData == null) return false;

        float fireRate = weapon.WeaponData.fireRate;
        bool canFireByRate = Time.time >= lastFireTime + fireRate;

        var ammoModule = weapon.GetAmmoModule();
        bool hasAmmo = ammoModule == null || ammoModule.GetCurrentAmmo() > 0;

        return canFireByRate && hasAmmo && !isFiring;
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
            // Play empty sound
            if (weapon.WeaponSound?.emptyClip != null)
            {
                weapon.PlaySound(weapon.WeaponSound.emptyClip);
            }
            isFiring = false;
            yield break;
        }

        // Trigger animations
        weapon.SetAnimationTrigger("Recoil");

        // Play fire sound
        if (weapon.WeaponSound?.shootClip != null)
        {
            weapon.PlaySound(weapon.WeaponSound.shootClip);
        }

        // Fire projectiles
        yield return FireProjectiles();

        // Recovery
        weapon.SetAnimationTrigger("RecoilRecover");

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
        float velocity = weapon.WeaponData?.bulletVelocity ?? 100f;
        projectileModule.CreateProjectile(weapon.FirePoint.position, baseDirection, velocity);

        yield return null;
    }
}