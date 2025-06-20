using UnityEngine;

public class PhysicalProjectileModule : MonoBehaviour, IProjectileModule
{
    [Header("Projectile Settings")]
    public GameObject bulletPrefab;
    public float bulletLifetime = 3f;
    public float bulletDamage = 25f;

    [Header("Trail Settings")]
    public bool enableTrails = true;
    public Color trailColor = Color.yellow;
    public float trailWidth = 0.1f;
    public float trailLength = 0.5f;

    private ModularWeapon weapon;

    public void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;
        Debug.Log("PhysicalProjectileModule initialized");
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

        // Apply velocity
        var rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = direction * velocity;
        }

        // Add trail if enabled
        if (enableTrails)
        {
            AddTrail(bullet);
        }

        // Set lifetime
        Destroy(bullet, bulletLifetime);

        Debug.Log($"Created bullet with {bulletLifetime}s lifetime");
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
}
// end