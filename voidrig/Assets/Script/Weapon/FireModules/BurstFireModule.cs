// BurstFireModule.cs
using UnityEngine;
using System.Collections;

public class BurstFireModule : MonoBehaviour, IFireModule
{
    [Header("Burst Settings")]
    public int burstCount = 3;
    public float burstInterval = 0.1f;
    public float burstCooldown = 0.3f;

    private ModularWeapon weapon;
    private bool isFiring = false;
    private float lastFireTime = -1f;

    public void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;
        Debug.Log("BurstFireModule ready to fire");
    }

    public void OnWeaponActivated()
    {
        isFiring = false;
        Debug.Log("BurstFireModule activated");
    }

    public void OnWeaponDeactivated()
    {
        isFiring = false;
        StopAllCoroutines();
    }

    public void OnUpdate() { }

    public bool CanFire()
    {
        if (!weapon.isActiveWeapon) return false;
        if (weapon.GetAmmoModule()?.GetCurrentAmmo() <= 0) return false;
        if (Time.time < lastFireTime + burstCooldown) return false;
        if (isFiring) return false;
        return true;
    }

    public void OnFireInput(bool isPressed, bool wasPressed)
    {
        if (wasPressed)
        {
            Debug.Log("BurstFireModule received fire input");
            Debug.Log($"CanFire: {CanFire()}");
            Debug.Log($"isFiring: {isFiring}");
        }

        if (wasPressed && CanFire())
        {
            Debug.Log("=== STARTING BURST FIRE! ===");
            StartCoroutine(Fire());
        }
    }

    public IEnumerator Fire()
    {
        isFiring = true;
        lastFireTime = Time.time;

        var projectileModule = weapon.GetProjectileModule();
        var ammoModule = weapon.GetAmmoModule();

        for (int i = 0; i < burstCount && ammoModule.GetCurrentAmmo() > 0; i++)
        {
            if (ammoModule.ConsumeAmmo())
            {
                Vector3 direction = weapon.CalculateBaseDirection();
                projectileModule?.CreateProjectile(weapon.FirePoint.position, direction, 100f);
                Debug.Log($"Shot {i + 1}/{burstCount} fired");

                if (i < burstCount - 1)
                    yield return new WaitForSeconds(burstInterval);
            }
        }

        yield return new WaitForSeconds(burstCooldown);
        isFiring = false;
        Debug.Log("Burst complete");
    }
}
// end