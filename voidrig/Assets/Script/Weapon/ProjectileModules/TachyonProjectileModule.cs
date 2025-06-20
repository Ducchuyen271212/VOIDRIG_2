//TachyonProjectileModule.cs
using UnityEngine;

public class TachyonProjectileModule : MonoBehaviour, IProjectileModule
{
    [Header("Tachyon Projectile Settings")]
    public GameObject projectilePrefab;
    public float damage = 60f;
    public float velocity = 500f;
    public float lifetime = 2f;

    [Header("Tachyon Properties")]
    public bool instantHit = true;
    public float phaseShiftChance = 0.3f;
    public float maxRange = 1000f;

    private ModularWeapon weapon;

    public void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;
        Debug.Log("TachyonProjectileModule initialized");
    }

    public void OnWeaponActivated() { }
    public void OnWeaponDeactivated() { }
    public void OnUpdate() { }

    public GameObject CreateProjectile(Vector3 position, Vector3 direction, float velocity)
    {
        if (instantHit)
        {
            return CreateInstantTachyonBeam(position, direction);
        }
        else
        {
            return CreateTachyonProjectile(position, direction, velocity);
        }
    }

    private GameObject CreateInstantTachyonBeam(Vector3 position, Vector3 direction)
    {
        // Create instant hit effect
        RaycastHit hit;

        if (Physics.Raycast(position, direction, out hit, maxRange))
        {
            // Instant damage at hit point
            var damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                DamageInfo damageInfo = new DamageInfo
                {
                    damage = damage,
                    projectileType = ProjectileType.Tachyon,
                    hitPoint = hit.point,
                    hitNormal = hit.normal
                };
                damageable.TakeDamage(damageInfo);
            }

            // Create beam effect
            CreateTachyonBeamEffect(position, hit.point);
        }

        return null; // No physical projectile
    }

    private GameObject CreateTachyonProjectile(Vector3 position, Vector3 direction, float velocity)
    {
        if (projectilePrefab == null) return null;

        GameObject projectile = Instantiate(projectilePrefab, position, Quaternion.LookRotation(direction));

        // Set damage
        var bullet = projectile.GetComponent<Bullet>();
        if (bullet != null) bullet.damage = damage;

        var modularBullet = projectile.GetComponent<ModularCompatibleBullet>();
        if (modularBullet != null)
        {
            modularBullet.damage = damage;
            modularBullet.projectileType = ProjectileType.Tachyon;
        }

        var fullBullet = projectile.GetComponent<ModularBullet>();
        if (fullBullet != null)
        {
            fullBullet.SetTachyonProperties(phaseShiftChance);
        }

        // Apply very high velocity
        var rb = projectile.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = direction * velocity * 10f;

        AddTachyonEffects(projectile);

        Destroy(projectile, lifetime);
        return projectile;
    }

    private void CreateTachyonBeamEffect(Vector3 start, Vector3 end)
    {
        GameObject beamEffect = new GameObject("TachyonBeam");
        LineRenderer line = beamEffect.AddComponent<LineRenderer>();
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = Color.cyan;
        line.endColor = Color.cyan;
        line.startWidth = 0.1f;
        line.endWidth = 0.05f;
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);

        Destroy(beamEffect, 0.1f);
    }

    private void AddTachyonEffects(GameObject projectile)
    {
        var renderer = projectile.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.cyan;
            renderer.material.EnableKeyword("_EMISSION");
            renderer.material.SetColor("_EmissionColor", Color.cyan * 4f);
        }
    }

    public ProjectileType GetProjectileType() => ProjectileType.Tachyon;
}
//end