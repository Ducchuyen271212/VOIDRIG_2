// ShotgunFireModule.cs
using System.Collections;
using UnityEngine;

public class ShotgunFireModule : MonoBehaviour, IFireModule
{
    [Header("Shotgun Settings")]
    public int pelletCount = 8;
    public float spreadIntensity = 10f;
    public float tiltAngle = 0f;
    public float bulletLifetimeOverride = -1f;
    public float fireRate = 0.8f;

    private ModularWeapon weapon;
    private bool readyToShoot = true;
    private float lastShotTime;
    private bool isFiring = false;

    public void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;
        Debug.Log("ShotgunFireModule initialized");
    }

    public void OnWeaponActivated() { readyToShoot = true; isFiring = false; }
    public void OnWeaponDeactivated() => isFiring = false;
    public void OnUpdate() { }

    public void OnFireInput(bool isPressed, bool wasPressed)
    {
        if (wasPressed && CanFire())
            StartCoroutine(Fire());
    }

    public bool CanFire()
    {
        if (Time.time < lastShotTime + fireRate) return false;
        var ammo = weapon.GetAmmoModule();
        if (ammo != null && ammo.GetCurrentAmmo() <= 0) return false;
        return readyToShoot && !isFiring;
    }

    public IEnumerator Fire()
    {
        if (!CanFire()) yield break;

        isFiring = true;
        readyToShoot = false;
        lastShotTime = Time.time;

        var proj = weapon.GetProjectileModule();
        var ammo = weapon.GetAmmoModule();

        if (ammo != null && ammo.GetCurrentAmmo() <= 0)
        {
            Debug.Log("Out of ammo!");
            isFiring = false;
            readyToShoot = true;
            yield break;
        }

        if (proj != null)
        {
            // Compute base direction
            Ray aimRay = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            Vector3 farTarget = aimRay.GetPoint(100f);
            Vector3 baseDir = (farTarget - weapon.FirePoint.position).normalized;

            // Fire each pellet
            for (int i = 0; i < pelletCount; i++)
            {
                float t = pelletCount > 1 ? (float)i / (pelletCount - 1) : 0.5f;
                float yawOffset = Mathf.Lerp(-spreadIntensity, spreadIntensity, t);

                Quaternion yaw = Quaternion.AngleAxis(yawOffset, Camera.main.transform.up);
                Vector3 dir = yaw * baseDir;
                dir = Quaternion.AngleAxis(tiltAngle, baseDir) * dir;

                GameObject b = proj.CreateProjectile(
                    weapon.FirePoint.position,
                    dir,
                    100f
                );

                if (b != null && bulletLifetimeOverride > 0f)
                {
                    Destroy(b, bulletLifetimeOverride);
                }
            }

            if (ammo != null) ammo.ConsumeAmmo(1);
        }

        yield return new WaitForSeconds(fireRate);
        readyToShoot = true;
        isFiring = false;
    }
}
// end