
// SingleFireModule.cs
using UnityEngine;
using System.Collections;

public class SingleFireModule : MonoBehaviour, IFireModule
{
    [Header("Single Fire Settings")]
    public float fireRate = 0.3f;

    private ModularWeapon weapon;
    private bool isFirePressed = false;
    private bool wasFirePressed = false;
    private float lastFireTime = -1f;
    private bool isFiring = false;

    public void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;
        Debug.Log("SingleFireModule initialized");
    }

    public void OnWeaponActivated() { isFiring = false; }
    public void OnWeaponDeactivated() { isFiring = false; }
    public void OnUpdate() { }

    public bool CanFire()
    {
        bool hasAmmo = weapon.GetAmmoModule()?.GetCurrentAmmo() > 0;
        bool fireRateOK = Time.time >= lastFireTime + fireRate;
        return hasAmmo && fireRateOK && !isFiring;
    }

    public void OnFireInput(bool isPressed, bool wasPressed)
    {
        isFirePressed = isPressed;
        wasFirePressed = wasPressed;

        if (wasPressed && CanFire())
        {
            StartCoroutine(Fire());
        }
    }

    public IEnumerator Fire()
    {
        if (!CanFire()) yield break;

        isFiring = true;
        lastFireTime = Time.time;

        var projectileModule = weapon.GetProjectileModule();
        var targetingModule = weapon.GetTargetingModule();
        var ammoModule = weapon.GetAmmoModule();

        if (ammoModule != null && ammoModule.ConsumeAmmo())
        {
            Vector3 baseDirection = weapon.CalculateBaseDirection();
            Vector3 finalDirection = targetingModule?.CalculateDirection(baseDirection) ?? baseDirection;

            projectileModule?.CreateProjectile(
                weapon.FirePoint.position,
                finalDirection,
                100f
            );
        }

        yield return new WaitForSeconds(fireRate);
        isFiring = false;
    }
}
//end