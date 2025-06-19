//ExoticProjectileModule.cs
using UnityEngine;

public class ExoticProjectileModule : BaseProjectileModule
{
    [Header("Exotic Settings")]
    public bool hasNegativeMass = true;
    public float gravityReversalStrength = 2f;
    public bool phasesToDifferentDimension = true;

    public override GameObject CreateProjectile(Vector3 position, Vector3 direction, float velocity)
    {
        GameObject projectile = InstantiateProjectile(position, direction, velocity);

        if (projectile != null)
        {
            var bullet = projectile.GetComponent<ModularBullet>();
            bullet?.SetExoticProperties(hasNegativeMass, gravityReversalStrength, phasesToDifferentDimension);

            // Exotic physics
            ApplyExoticPhysics(projectile);
            AddExoticEffects(projectile);
        }

        return projectile;
    }

    private void ApplyExoticPhysics(GameObject projectile)
    {
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null && hasNegativeMass)
        {
            rb.useGravity = false;
            // Add upward force to simulate negative mass
            rb.AddForce(Vector3.up * gravityReversalStrength, ForceMode.Force);
        }
    }

    private void AddExoticEffects(GameObject projectile)
    {
        // Add reality distortion effects
        var renderer = projectile.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.magenta;
            // Add shader effects for reality distortion
        }

        // Add particle effects for dimensional phasing
        var particles = projectile.GetComponentInChildren<ParticleSystem>();
        if (particles != null)
        {
            var main = particles.main;
            main.startColor = Color.magenta;
            var emission = particles.emission;
            emission.rateOverTime = 50f;
        }
    }
}
//end