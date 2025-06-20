//ChargeFireModule.cs
using UnityEngine;
using System.Collections;

public class ChargeFireModule : MonoBehaviour, IFireModule
{
    [Header("Charge Settings")]
    public float maxChargeTime = 3f;
    public float minChargeTime = 0.5f;
    public AnimationCurve chargeDamageCurve = AnimationCurve.Linear(0, 1, 1, 3);

    private ModularWeapon weapon;
    private float chargeStartTime = 0f;
    private bool isCharging = false;
    private bool isFirePressed = false;
    private bool wasFirePressed = false;
    private float lastFireTime = -1f;
    private bool isFiring = false;

    public void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;
        Debug.Log("ChargeFireModule initialized");
    }

    public void OnWeaponActivated() { isCharging = false; isFiring = false; }
    public void OnWeaponDeactivated() { isCharging = false; isFiring = false; }
    public void OnUpdate() { }

    public bool CanFire()
    {
        bool hasAmmo = weapon.GetAmmoModule()?.GetCurrentAmmo() > 0;
        return hasAmmo && !isFiring;
    }

    public void OnFireInput(bool isPressed, bool wasPressed)
    {
        isFirePressed = isPressed;
        wasFirePressed = wasPressed;

        if (wasPressed && CanFire())
        {
            isCharging = true;
            chargeStartTime = Time.time;
            Debug.Log("Charge started");
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

    public IEnumerator Fire()
    {
        isFiring = true;
        lastFireTime = Time.time;

        float chargeTime = Time.time - chargeStartTime;
        float chargeRatio = Mathf.Clamp01(chargeTime / maxChargeTime);
        float damageMultiplier = chargeDamageCurve.Evaluate(chargeRatio);

        var projectileModule = weapon.GetProjectileModule();
        var targetingModule = weapon.GetTargetingModule();
        var ammoModule = weapon.GetAmmoModule();

        if (projectileModule != null && ammoModule != null && ammoModule.ConsumeAmmo())
        {
            Vector3 baseDirection = weapon.CalculateBaseDirection();
            Vector3 finalDirection = targetingModule?.CalculateDirection(baseDirection) ?? baseDirection;

            GameObject projectile = projectileModule.CreateProjectile(
                weapon.FirePoint.position,
                finalDirection,
                100f
            );

            var bullet = projectile?.GetComponent<ModularBullet>();
            if (bullet != null)
            {
                bullet.SetDamageMultiplier(damageMultiplier);
                bullet.SetChargeLevel(chargeRatio);
            }

            Debug.Log($"Charged shot fired with {chargeRatio:F2} charge level");
        }

        yield return new WaitForSeconds(0.3f);
        isFiring = false;
    }
}
//end