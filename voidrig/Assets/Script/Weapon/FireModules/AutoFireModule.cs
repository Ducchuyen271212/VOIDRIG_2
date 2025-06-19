//AutoFireModule.cs
using UnityEngine;
using System.Collections;

public class AutoFireModule : BaseFireModule
{
    protected override bool ShouldFire()
    {
        return isFirePressed;
    }

    public override IEnumerator Fire()
    {
        isFiring = true;
        lastFireTime = Time.time;

        var projectileModule = weapon.GetProjectileModule();
        var targetingModule = weapon.GetTargetingModule();
        var ammoModule = weapon.GetAmmoModule();

        while (isFirePressed && ammoModule?.GetCurrentAmmo() > 0)
        {
            if (Time.time >= lastFireTime + weapon.WeaponData.fireRate)
            {
                lastFireTime = Time.time;

                Vector3 baseDirection = weapon.CalculateBaseDirection();
                Vector3 finalDirection = targetingModule?.CalculateDirection(baseDirection) ?? baseDirection;

                projectileModule?.CreateProjectile(
                    weapon.FirePoint.position,
                    finalDirection,
                    weapon.WeaponData.bulletVelocity
                );

                ammoModule?.ConsumeAmmo();

                weapon.SetAnimationTrigger("Recoil");
                weapon.PlaySound(weapon.WeaponSound?.shootClip);
            }

            yield return null;
        }

        isFiring = false;
    }
}

//end