//ChargeFireModule.cs
using UnityEngine;
using System.Collections;

public class ChargeFireModule : BaseFireModule
{
    [Header("Charge Settings")]
    public float maxChargeTime = 3f;
    public float minChargeTime = 0.5f;
    public AnimationCurve chargeDamageCurve = AnimationCurve.Linear(0, 1, 1, 3);

    private float chargeStartTime = 0f;
    private bool isCharging = false;

    protected override bool ShouldFire()
    {
        return false; // handled manually
    }

    public override void OnFireInput(bool isPressed, bool wasPressed)
    {
        isFirePressed = isPressed;
        wasFirePressed = wasPressed;

        if (wasPressed && CanFire())
        {
            isCharging = true;
            chargeStartTime = Time.time;
            weapon.SetAnimationTrigger("ChargeStart");
        }
        else if (!isPressed && isCharging)
        {
            float chargeTime = Time.time - chargeStartTime;
            if (chargeTime >= minChargeTime)
            {
                StartCoroutine(Fire());
            }
            isCharging = false;
        }
    }

    public override IEnumerator Fire()
    {
        isFiring = true;
        lastFireTime = Time.time;

        float chargeTime = Time.time - chargeStartTime;
        float chargeRatio = Mathf.Clamp01(chargeTime / maxChargeTime);
        float damageMultiplier = chargeDamageCurve.Evaluate(chargeRatio);

        var projectileModule = weapon.GetProjectileModule();
        var targetingModule = weapon.GetTargetingModule();
        var ammoModule = weapon.GetAmmoModule();

        if (projectileModule != null && ammoModule != null)
        {
            Vector3 baseDirection = weapon.CalculateBaseDirection();
            Vector3 finalDirection = targetingModule?.CalculateDirection(baseDirection) ?? baseDirection;

            GameObject projectile = projectileModule.CreateProjectile(
                weapon.FirePoint.position,
                finalDirection,
                weapon.WeaponData.bulletVelocity
            );

            ModularBullet bullet = projectile.GetComponent<ModularBullet>();
            if (bullet != null)
            {
                bullet.SetDamageMultiplier(damageMultiplier);
                bullet.SetChargeLevel(chargeRatio);
            }

            ammoModule.ConsumeAmmo();

            weapon.SetAnimationTrigger("ChargeFire");
            weapon.PlaySound(weapon.WeaponSound?.shootClip);
        }

        yield return new WaitForSeconds(0.3f);
        isFiring = false;
    }
}

//end