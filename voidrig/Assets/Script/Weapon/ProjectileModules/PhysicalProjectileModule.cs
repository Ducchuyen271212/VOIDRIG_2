// PhysicalProjectileModule.cs
using UnityEngine;

public class PhysicalProjectileModule : MonoBehaviour, IProjectileModule
{
    [Header("Projectile Settings")]
    public GameObject bulletPrefab;
    public float bulletLifetime = 3f;
    public float bulletDamage = 25f;

    [Header("Bullet Speed")]
    [Tooltip("Speed of the bullet in units per second")]
    public float bulletSpeed = 100f;

    [Header("Trail Settings")]
    public bool enableTrails = true;
    public Color trailColor = Color.yellow;
    public float trailWidth = 0.1f;
    public float trailLength = 0.5f;

    private ModularWeapon weapon;

    public void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;
        Debug.Log($"PhysicalProjectileModule initialized - Speed: {bulletSpeed}, Damage: {bulletDamage}");
    }

    public void OnWeaponActivated() { }
    public void OnWeaponDeactivated() { }
    public void OnUpdate() { }

    public GameObject CreateProjectile(Vector3 position, Vector3 direction, float velocity)
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("No bullet prefab assigned!");
            return null;
        }

        // Create the bullet
        GameObject bullet = Instantiate(bulletPrefab, position, Quaternion.LookRotation(direction));

        // Set damage
        var bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.damage = bulletDamage;
        }

        var modularBullet = bullet.GetComponent<ModularCompatibleBullet>();
        if (modularBullet != null)
        {
            modularBullet.damage = bulletDamage;
            modularBullet.projectileType = ProjectileType.Physical;
        }

        // Apply velocity using the module's bulletSpeed instead of passed velocity
        var rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = direction * bulletSpeed;
        }

        // Add trail if enabled
        if (enableTrails)
        {
            AddTrail(bullet);
        }

        // Set lifetime
        Destroy(bullet, bulletLifetime);

        Debug.Log($"Created bullet with {bulletLifetime}s lifetime at {bulletSpeed} speed");
        return bullet;
    }

    private void AddTrail(GameObject bullet)
    {
        var trail = bullet.GetComponent<TrailRenderer>();
        if (trail == null)
        {
            trail = bullet.AddComponent<TrailRenderer>();
        }

        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.material.color = trailColor;
        trail.time = trailLength;
        trail.startWidth = trailWidth;
        trail.endWidth = trailWidth * 0.1f;
        trail.startColor = trailColor;
        trail.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
    }

    public ProjectileType GetProjectileType() => ProjectileType.Physical;

    // Preset configurations
    [ContextMenu("Preset: Assault Rifle")]
    public void PresetAssaultRifle()
    {
        bulletSpeed = 200f;
        bulletDamage = 25f;
        bulletLifetime = 3f;
        enableTrails = true;
        trailColor = Color.yellow;
    }

    [ContextMenu("Preset: Sniper Rifle")]
    public void PresetSniperRifle()
    {
        bulletSpeed = 400f;
        bulletDamage = 100f;
        bulletLifetime = 5f;
        enableTrails = true;
        trailColor = Color.cyan;
        trailLength = 1f;
    }

    [ContextMenu("Preset: SMG")]
    public void PresetSMG()
    {
        bulletSpeed = 150f;
        bulletDamage = 15f;
        bulletLifetime = 2f;
        enableTrails = true;
        trailColor = Color.white;
        trailWidth = 0.05f;
    }

    [ContextMenu("Preset: Pistol")]
    public void PresetPistol()
    {
        bulletSpeed = 120f;
        bulletDamage = 20f;
        bulletLifetime = 2.5f;
        enableTrails = false;
    }
}
// end