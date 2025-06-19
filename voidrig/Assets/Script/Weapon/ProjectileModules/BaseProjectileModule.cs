// BaseProjectileModule.cs
using UnityEngine;

public abstract class BaseProjectileModule : MonoBehaviour, IProjectileModule
{
    [Header("Projectile Settings")]
    public GameObject projectilePrefab;
    public ProjectileType projectileType = ProjectileType.Physical;
    public ProjectileData projectileData;


    protected ModularWeapon weapon;

    public virtual void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;
    }

    public virtual void OnWeaponActivated() { }
    public virtual void OnWeaponDeactivated() { }
    public virtual void OnUpdate() { }

    public virtual GameObject CreateProjectile(Vector3 position, Vector3 direction, float velocity)
    {
        if (projectilePrefab == null) return null;

        GameObject projectile = Instantiate(projectilePrefab, position, Quaternion.LookRotation(direction));

        // Set damage if using your existing Bullet system
        var bullet = projectile.GetComponent<Bullet>();
        if (bullet != null && weapon.WeaponData != null)
        {
            bullet.damage = weapon.WeaponData.damage;
        }

        // Set damage if using ModularCompatibleBullet
        var modularBullet = projectile.GetComponent<ModularCompatibleBullet>();
        if (modularBullet != null && weapon.WeaponData != null)
        {
            modularBullet.damage = weapon.WeaponData.damage;
            modularBullet.projectileType = projectileType;
        }

        // Set damage if using ModularBullet
        var fullModularBullet = projectile.GetComponent<ModularBullet>();
        if (fullModularBullet != null)
        {
            // Create projectile data
            ProjectileData data = new ProjectileData
            {
                type = projectileType,
                damage = weapon.WeaponData?.damage ?? 10f,
                velocity = velocity,
                lifetime = weapon.WeaponData?.bulletLifeTime ?? 5f
            };
            fullModularBullet.Initialize(data, weapon.WeaponData);
        }

        // Apply velocity
        var rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = direction * velocity;
        }

        // Destroy after lifetime
        float lifetime = weapon.WeaponData?.bulletLifeTime ?? 5f;
        Destroy(projectile, lifetime);

        return projectile;
    }

    public ProjectileType GetProjectileType() => projectileType;

    protected GameObject InstantiateProjectile(Vector3 position, Vector3 direction, float velocity)
    {
        if (projectilePrefab == null) return null;

        GameObject instance = Instantiate(projectilePrefab, position, Quaternion.LookRotation(direction));
        Rigidbody rb = instance.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = direction * velocity;
        }

        ModularBullet bullet = instance.GetComponent<ModularBullet>();
        if (bullet != null)
        {
            bullet.Initialize(projectileData, weapon?.WeaponData);
        }

        return instance;
    }

}