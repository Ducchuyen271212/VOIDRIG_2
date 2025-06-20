//ExoticProjectileModule.cs
using UnityEngine;

public class ExoticProjectileModule : MonoBehaviour, IProjectileModule
{
    [Header("Exotic Projectile Settings")]
    public GameObject projectilePrefab;
    public float damage = 50f;
    public float velocity = 180f;
    public float lifetime = 4f;
    public Color exoticColor = Color.magenta;

    [Header("Exotic Properties")]
    public bool hasNegativeMass = true;
    public float gravityReversalStrength = 2f;
    public bool phasesToDifferentDimension = true;

    private ModularWeapon weapon;

    public void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;
        Debug.Log("ExoticProjectileModule initialized");
    }

    public void OnWeaponActivated() { }
    public void OnWeaponDeactivated() { }
    public void OnUpdate() { }

    public GameObject CreateProjectile(Vector3 position, Vector3 direction, float velocity)
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
            modularBullet.projectileType = ProjectileType.Exotic;
        }

        var fullBullet = projectile.GetComponent<ModularBullet>();
        if (fullBullet != null)
        {
            fullBullet.SetExoticProperties(hasNegativeMass, gravityReversalStrength, phasesToDifferentDimension);
        }

        // Apply exotic physics
        ApplyExoticPhysics(projectile, direction, velocity);

        // Add exotic effects
        AddExoticEffects(projectile);

        Destroy(projectile, lifetime);
        return projectile;
    }

    private void ApplyExoticPhysics(GameObject projectile, Vector3 direction, float velocity)
    {
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = direction * velocity;

            if (hasNegativeMass)
            {
                rb.useGravity = false;
                // Add upward force to simulate negative mass
                rb.AddForce(Vector3.up * gravityReversalStrength, ForceMode.Force);
            }
        }
    }

    private void AddExoticEffects(GameObject projectile)
    {
        // Add reality distortion effects
        var renderer = projectile.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = exoticColor;
            renderer.material.EnableKeyword("_EMISSION");
            renderer.material.SetColor("_EmissionColor", exoticColor * 2f);
        }

        // Add particle effects for dimensional phasing
        var particles = projectile.GetComponentInChildren<ParticleSystem>();
        if (particles != null)
        {
            var main = particles.main;
            main.startColor = exoticColor;
            var emission = particles.emission;
            emission.rateOverTime = 50f;
        }

        // Add light
        var light = projectile.GetComponent<Light>();
        if (light == null) light = projectile.AddComponent<Light>();
        light.color = exoticColor;
        light.intensity = 3f;
        light.range = 5f;
    }

    public ProjectileType GetProjectileType() => ProjectileType.Exotic;
}
//end