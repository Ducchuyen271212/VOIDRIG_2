// ShotgunFireModule.cs - 0–3 high, consistent ray direction
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShotgunFireModule : MonoBehaviour, IFireModule
{
    [Header("Shotgun Settings")]
    public int pelletCount = 8;
    public float spreadIntensity = 1f; // 0 = no spread, 3 = very high spread
    public SpreadPattern spreadPattern = SpreadPattern.Horizontal;
    public float bulletLifetimeOverride = -1f; // -1 = use weapon data

    public enum SpreadPattern
    {
        Horizontal,  // evenly across X
        Circular,    // evenly around circle radius = spreadIntensity
        Vertical,    // evenly across Y
        Random,      // random within circle radius = spreadIntensity
        XShape       // full “X” diagonal
    }

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
        if (weapon.GetProjectileModule() == null) return false;

        var ammo = weapon.GetAmmoModule();
        if (ammo != null)
        {
            var std = ammo as StandardAmmoModule;
            if (std != null && std.IsReloading()) return false;
            if (ammo.GetCurrentAmmo() <= 0) return false;
        }

        return readyToShoot
            && Time.time - lastShotTime >= weapon.WeaponData.fireRate
            && !isFiring;
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
            if (weapon.WeaponSound?.emptyClip != null)
                weapon.PlaySound(weapon.WeaponSound.emptyClip);
            weapon.SetAnimationTrigger("RecoilRecover");
            isFiring = false;
            readyToShoot = true;
            yield break;
        }

        if (proj != null)
        {
            SetupBulletPrefab(proj);

            // Use viewport ray direction for consistency
            var ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 baseDir = ray.direction.normalized;

            // Generate offsets once
            List<Vector2> offsets = GenerateOffsets(pelletCount, spreadPattern, spreadIntensity);

            for (int i = 0; i < pelletCount; i++)
            {
                var o = offsets[i];
                Vector3 dir = Quaternion.Euler(o.y, o.x, 0f) * baseDir;

                var bullet = proj.CreateProjectile(
                    weapon.FirePoint.position,
                    dir,
                    weapon.WeaponData.bulletVelocity
                );

                if (bullet != null && bulletLifetimeOverride > 0)
                {
                    var d = bullet.GetComponent<DestroyAfterTime>();
                    if (d != null) Destroy(d);
                    Destroy(bullet, bulletLifetimeOverride);
                }
            }

            if (ammo != null)
            {
                ammo.ConsumeAmmo(1);
                Debug.Log($"Shotgun fired! Remaining ammo: {ammo.GetCurrentAmmo()}");
            }

            weapon.SetAnimationTrigger("Recoil");
            if (weapon.WeaponSound?.shootClip != null)
                weapon.PlaySound(weapon.WeaponSound.shootClip);
        }
        else
        {
            Debug.LogError("Cannot fire – no projectile module found!");
        }

        yield return new WaitForSeconds(weapon.WeaponData?.fireRate ?? 0.8f);
        weapon.SetAnimationTrigger("RecoilRecover");
        readyToShoot = true;
        isFiring = false;
    }

    // Precompute evenly spaced or random offsets
    private List<Vector2> GenerateOffsets(int count, SpreadPattern pattern, float intensity)
    {
        var list = new List<Vector2>(count);
        float spread = Mathf.Clamp(intensity, 0f, 3f);
        bool isAim = AimingManager.Instance != null && AimingManager.Instance.isAiming;
        float mul = isAim ? AimingManager.Instance.GetAccuracyMultiplier() : 1f;
        spread *= mul;

        switch (pattern)
        {
            case SpreadPattern.Horizontal:
                for (int i = 0; i < count; i++)
                {
                    float x = count > 1
                        ? Mathf.Lerp(-spread, spread, (float)i / (count - 1))
                        : 0f;
                    list.Add(new Vector2(x, 0f));
                }
                break;

            case SpreadPattern.Vertical:
                for (int i = 0; i < count; i++)
                {
                    float y = count > 1
                        ? Mathf.Lerp(-spread, spread, (float)i / (count - 1))
                        : 0f;
                    list.Add(new Vector2(0f, y));
                }
                break;

            case SpreadPattern.Circular:
                for (int i = 0; i < count; i++)
                {
                    float ang = (2 * Mathf.PI * i) / count;
                    list.Add(new Vector2(Mathf.Cos(ang) * spread, Mathf.Sin(ang) * spread));
                }
                break;

            case SpreadPattern.XShape:
                int half = (count + 1) / 2;
                bool odd = (count % 2) == 1;
                for (int i = 0; i < half; i++)
                {
                    float t = half > 1 ? (float)i / (half - 1) : 0f;
                    float d = Mathf.Lerp(-spread, spread, t);
                    list.Add(new Vector2(d, d));
                }
                for (int i = 0; i < half; i++)
                {
                    if (odd && i == 0) continue;
                    float t = half > 1 ? (float)i / (half - 1) : 0f;
                    float d = Mathf.Lerp(-spread, spread, t);
                    list.Add(new Vector2(d, -d));
                }
                break;

            case SpreadPattern.Random:
                for (int i = 0; i < count; i++)
                    list.Add(UnityEngine.Random.insideUnitCircle * spread);
                break;
        }

        return list;
    }

    private void SetupBulletPrefab(IProjectileModule proj)
    {
        var t = proj.GetType();
        var f = t.GetField("bulletPrefab") ?? t.GetField("projectilePrefab");
        if (f != null && f.GetValue(proj) == null)
        {
            var legacy = weapon.GetComponent<Weapon>();
            if (legacy?.bulletPrefab != null)
                f.SetValue(proj, legacy.bulletPrefab);
        }
    }

    public void SetBulletLifetime(float lt) => bulletLifetimeOverride = lt;

    [ContextMenu("Preset: Realistic Shotgun")]
    public void PresetRealisticShotgun()
    {
        pelletCount = 8;
        spreadIntensity = 1f;
        spreadPattern = SpreadPattern.Horizontal;
        bulletLifetimeOverride = 1f;
    }

    [ContextMenu("Preset: Wide Spread")]
    public void PresetWideSpread()
    {
        pelletCount = 12;
        spreadIntensity = 3f;
        spreadPattern = SpreadPattern.Circular;
        bulletLifetimeOverride = 0.8f;
    }

    [ContextMenu("Preset: Tight Choke")]
    public void PresetTightChoke()
    {
        pelletCount = 6;
        spreadIntensity = 0.5f;
        spreadPattern = SpreadPattern.Horizontal;
        bulletLifetimeOverride = 1.5f;
    }

    // Keep DestroyAfterTime here
    public class DestroyAfterTime : MonoBehaviour
    {
        public float lifetime = 5f;
        private void Start() => Destroy(gameObject, lifetime);
    }
}
