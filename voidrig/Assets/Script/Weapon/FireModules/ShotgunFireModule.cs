// ShotgunFireModule.cs — true horizontal spread + proper tilt
using System.Collections;
using UnityEngine;

public class ShotgunFireModule : MonoBehaviour, IFireModule
{
    [Header("Shotgun Settings")]
    public int pelletCount = 8;
    [Tooltip("Half-angle of horizontal spread in degrees (e.g. 10 → –10° to +10°)")]
    public float spreadIntensity = 10f;
    [Tooltip("Rotate the entire spread fan around the barrel axis (0 = true horizontal, 90 = vertical)")]
    public float tiltAngle = 0f;
    [Tooltip(">0 overrides bullet lifetime")]
    public float bulletLifetimeOverride = -1f;

    private ModularWeapon weapon;
    private bool readyToShoot = true;
    private float lastShotTime;
    private bool isFiring = false;

    public void Initialize(ModularWeapon weapon) => this.weapon = weapon;
    public void OnWeaponActivated() { readyToShoot = true; isFiring = false; }
    public void OnWeaponDeactivated() => isFiring = false;
    public void OnUpdate() { }

    public void OnFireInput(bool isPressed, bool wasPressed)
    {
        if (!weapon.isActiveWeapon) return;
        if (wasPressed && CanFire()) StartCoroutine(Fire());
    }

    public bool CanFire()
    {
        if (!weapon.isActiveWeapon || weapon.WeaponData == null) return false;
        if (Time.time < lastShotTime + weapon.WeaponData.fireRate) return false;
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

        // out-of-ammo check
        if (ammo != null && ammo.GetCurrentAmmo() <= 0)
        {
            if (weapon.WeaponSound?.emptyClip != null)
                weapon.PlaySound(weapon.WeaponSound.emptyClip);
            weapon.SetAnimationTrigger("RecoilRecover");
            isFiring = false;
            readyToShoot = true;
            yield break;
        }

        if (proj != null)
        {
            // compute a fixed “baseDir” so tilt always behaves consistently
            Ray aimRay = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            Vector3 farTarget = aimRay.GetPoint(100f);
            Vector3 baseDir = (farTarget - weapon.FirePoint.position).normalized;

            // fire each pellet in turn
            for (int i = 0; i < pelletCount; i++)
            {
                // angle from –spreadIntensity to +spreadIntensity
                float t = pelletCount > 1 ? (float)i / (pelletCount - 1) : 0.5f;
                float yawOffset = Mathf.Lerp(-spreadIntensity, spreadIntensity, t);

                // first yaw around camera up → fan
                Quaternion yaw = Quaternion.AngleAxis(yawOffset, Camera.main.transform.up);
                Vector3 dir = yaw * baseDir;

                // then tilt that entire fan around the barrel axis
                dir = Quaternion.AngleAxis(tiltAngle, baseDir) * dir;

                // create projectile
                GameObject b = proj.CreateProjectile(
                    weapon.FirePoint.position,
                    dir,
                    weapon.WeaponData.bulletVelocity
                );

                // override lifetime if needed
                if (b != null && bulletLifetimeOverride > 0f)
                {
                    var old = b.GetComponent<DestroyAfterTime>();
                    if (old != null) Destroy(old);
                    Destroy(b, bulletLifetimeOverride);
                }
            }

            // consume one shell
            if (ammo != null) ammo.ConsumeAmmo(1);

            // recoil & sound
            weapon.SetAnimationTrigger("Recoil");
            if (weapon.WeaponSound?.shootClip != null)
                weapon.PlaySound(weapon.WeaponSound.shootClip);
        }

        // cooldown + recover
        yield return new WaitForSeconds(weapon.WeaponData.fireRate);
        weapon.SetAnimationTrigger("RecoilRecover");
        readyToShoot = true;
        isFiring = false;
    }

    // helper class as before
    public class DestroyAfterTime : MonoBehaviour
    {
        public float lifetime = 5f;
        private void Start() => Destroy(gameObject, lifetime);
    }
}
