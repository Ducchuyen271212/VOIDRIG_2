//TachyonProjectileModule.cs
using UnityEngine;

public class TachyonProjectileModule : BaseProjectileModule
{
    [Header("Tachyon Settings")]
    public bool instantHit = true;
    public float phaseShiftChance = 0.3f; // Chance to phase through obstacles

    public override GameObject CreateProjectile(Vector3 position, Vector3 direction, float velocity)
    {
        if (instantHit)
        {
            // Instant raycast hit
            return CreateInstantTachyonBeam(position, direction);
        }
        else
        {
            GameObject projectile = InstantiateProjectile(position, direction, velocity * 10f); // Much faster

            if (projectile != null)
            {
                var bullet = projectile.GetComponent<ModularBullet>();
                bullet?.SetTachyonProperties(phaseShiftChance);

                // Add tachyon effects
                AddTachyonEffects(projectile);
            }

            return projectile;
        }
    }

    private GameObject CreateInstantTachyonBeam(Vector3 position, Vector3 direction)
    {
        // Create instant hit effect
        RaycastHit hit;
        float maxDistance = 1000f;

        if (Physics.Raycast(position, direction, out hit, maxDistance))
        {
            // Instant damage at hit point
            var damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                DamageInfo damageInfo = new DamageInfo
                {
                    damage = projectileData.damage,
                    projectileType = ProjectileType.Tachyon,
                    bypassTypes = projectileData.bypassTypes,
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

    private void CreateTachyonBeamEffect(Vector3 start, Vector3 end)
    {
        // Create visual beam effect
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

        // Destroy effect after short time
        Destroy(beamEffect, 0.1f);
    }

    private void AddTachyonEffects(GameObject projectile)
    {
        // Add distortion effects
        var renderer = projectile.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.cyan;
            renderer.material.SetFloat("_Metallic", 1f);
            renderer.material.SetFloat("_Smoothness", 1f);
        }
    }
}
//end