//RailgunAbility.cs
using UnityEngine;

public class RailgunAbility : BaseAbilityModule
{
    [Header("Railgun Settings")]
    public float railgunDamage = 500f;
    public float maxRange = 1000f;

    protected override void OnAbilityActivated()
    {
        FireRailgun();
    }

    private void FireRailgun()
    {
        Vector3 direction = weapon.CalculateBaseDirection();
        Vector3 startPos = weapon.FirePoint.position;

        // Create visual beam
        CreateRailgunBeam(startPos, direction);

        // Raycast through everything
        RaycastHit[] hits = Physics.RaycastAll(startPos, direction, maxRange);

        foreach (var hit in hits)
        {
            var damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                DamageInfo railgunDamage = new DamageInfo
                {
                    damage = this.railgunDamage,
                    projectileType = ProjectileType.Tachyon,
                    bypassTypes = new ShieldBypassType[] { ShieldBypassType.AllShields, ShieldBypassType.Walls },
                    hitPoint = hit.point,
                    hitNormal = hit.normal
                };

                damageable.TakeDamage(railgunDamage);
            }
        }

        weapon.SetAnimationTrigger("RailgunFire");
    }

    private void CreateRailgunBeam(Vector3 start, Vector3 direction)
    {
        GameObject beam = new GameObject("RailgunBeam");
        LineRenderer line = beam.AddComponent<LineRenderer>();

        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = Color.cyan;
        line.endColor = Color.cyan;
        line.startWidth = 0.2f;
        line.endWidth = 0.1f;
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, start + direction * maxRange);

        Destroy(beam, 0.5f);
    }
}
//end