//AutoFireModule.cs
using UnityEngine;
using System.Collections;

public class AutoFireModule : MonoBehaviour, IFireModule
{
    [Header("Auto Fire Settings")]
    public float fireRate = 0.1f;

    private ModularWeapon weapon;
    private bool isFirePressed = false;
    private bool wasFirePressed = false;
    private float lastFireTime = -1f;
    private bool isFiring = false;
    private Coroutine fireCoroutine;

    public void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;
        Debug.Log("AutoFireModule initialized");
    }

    public void OnWeaponActivated()
    {
        isFiring = false;
        if (fireCoroutine != null)
        {
            StopCoroutine(fireCoroutine);
            fireCoroutine = null;
        }
    }

    public void OnWeaponDeactivated()
    {
        isFiring = false;
        if (fireCoroutine != null)
        {
            StopCoroutine(fireCoroutine);
            fireCoroutine = null;
        }
    }

    public void OnUpdate() { }

    public bool CanFire()
    {
        bool hasAmmo = weapon.GetAmmoModule()?.GetCurrentAmmo() > 0;
        bool fireRateOK = Time.time >= lastFireTime + fireRate;
        return hasAmmo && fireRateOK;
    }

    public void OnFireInput(bool isPressed, bool wasPressed)
    {
        isFirePressed = isPressed;
        wasFirePressed = wasPressed;

        if (isPressed && CanFire())
        {
            if (fireCoroutine == null)
            {
                fireCoroutine = StartCoroutine(Fire());
            }
        }
        else if (!isPressed)
        {
            if (fireCoroutine != null)
            {
                StopCoroutine(fireCoroutine);
                fireCoroutine = null;
            }
            isFiring = false;
        }
    }

    public IEnumerator Fire()
    {
        isFiring = true;

        var projectileModule = weapon.GetProjectileModule();
        var targetingModule = weapon.GetTargetingModule();
        var ammoModule = weapon.GetAmmoModule();

        while (isFirePressed && ammoModule?.GetCurrentAmmo() > 0)
        {
            if (Time.time >= lastFireTime + fireRate)
            {
                lastFireTime = Time.time;

                if (ammoModule.ConsumeAmmo())
                {
                    Vector3 baseDirection = weapon.CalculateBaseDirection();
                    Vector3 finalDirection = targetingModule?.CalculateDirection(baseDirection) ?? baseDirection;

                    projectileModule?.CreateProjectile(
                        weapon.FirePoint.position,
                        finalDirection,
                        100f
                    );
                }
            }

            yield return null;
        }

        fireCoroutine = null;
        isFiring = false;
    }
}
//end