//BurstFireModule.cs
using UnityEngine;
using System.Collections;

public class BurstFireModule : BaseFireModule
{
    [Header("Burst Settings")]
    public int burstCount = 3;
    public float burstInterval = 0.1f;

    protected override bool ShouldFire()
    {
        return wasFirePressed;
    }

    public override IEnumerator Fire()
    {
        isFiring = true;
        lastFireTime = Time.time;

        int actualBurstCount = weapon.WeaponData?.bulletsPerBurst ?? burstCount;

        var projectileModule = weapon.GetProjectileModule();
        var targetingModule = weapon.GetTargetingModule();
        var ammoModule = weapon.GetAmmoModule();

        for (int i = 0; i < actualBurstCount && ammoModule?.GetCurrentAmmo() > 0; i++)
        {
            Vector3 baseDirection = weapon.CalculateBaseDirection();
            Vector3 finalDirection = targetingModule?.CalculateDirection(baseDirection) ?? baseDirection;

            projectileModule?.CreateProjectile(
                weapon.FirePoint.position,
                finalDirection,
                weapon.WeaponData.bulletVelocity
            );

            ammoModule.ConsumeAmmo();

            weapon.SetAnimationTrigger("Recoil");
            weapon.PlaySound(weapon.WeaponSound?.shootClip);

            if (i < actualBurstCount - 1)
                yield return new WaitForSeconds(burstInterval);
        }

        yield return new WaitForSeconds(weapon.WeaponData.fireRate);
        isFiring = false;
    }
}

//end