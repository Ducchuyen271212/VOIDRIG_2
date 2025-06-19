// ModularCompatibleBullet.cs - Fixed to ignore weapons
using UnityEngine;

public class ModularCompatibleBullet : MonoBehaviour
{
    public float damage = 10f;
    public ProjectileType projectileType = ProjectileType.Physical;

    private void OnCollisionEnter(Collision objectHit)
    {
        GameObject hitObject = objectHit.gameObject;
        ContactPoint contact = objectHit.contacts[0];

        // === IGNORE ALL WEAPONS ===
        if (IsWeapon(hitObject))
        {
            Debug.Log($"Bullet ignoring weapon: {hitObject.name}");
            return; // Don't process collision with weapons
        }

        // Try modular system first (IDamageable)
        var damageable = hitObject.GetComponent<IDamageable>();
        if (damageable != null)
        {
            DamageInfo damageInfo = new DamageInfo
            {
                damage = damage,
                projectileType = projectileType,
                hitPoint = contact.point,
                hitNormal = contact.normal
            };
            damageable.TakeDamage(damageInfo);
            CreateBulletImpactEffect(objectHit);
            Destroy(gameObject);
            return;
        }

        // Fall back to your existing system (Health)
        if (hitObject.CompareTag("Enemy"))
        {
            Debug.Log("Hit " + hitObject.name + " for " + damage + " damage!");
            if (hitObject.TryGetComponent(out Health health))
            {
                health.TakeDamage(damage);
            }
            CreateBulletImpactEffect(objectHit);
            Destroy(gameObject);
        }
        else if (hitObject.CompareTag("Wall"))
        {
            CreateBulletImpactEffect(objectHit);
            Destroy(gameObject);
        }
    }

    private bool IsWeapon(GameObject obj)
    {
        // Check for weapon components
        if (obj.GetComponent<ModularWeapon>() != null) return true;
        if (obj.GetComponent<Weapon>() != null) return true;

        // Check for weapon tags
        if (obj.CompareTag("MachineGun")) return true;
        if (obj.CompareTag("ShotGun")) return true;
        if (obj.CompareTag("Sniper")) return true;
        if (obj.CompareTag("HandGun")) return true;
        if (obj.CompareTag("SMG")) return true;
        if (obj.CompareTag("BurstRifle")) return true;

        // Check name patterns
        if (obj.name.Contains("Weapon") ||
            obj.name.Contains("Gun") ||
            obj.name.Contains("Rifle") ||
            obj.name.Contains("Test")) return true;

        return false;
    }

    void CreateBulletImpactEffect(Collision objectHit)
    {
        ContactPoint contact = objectHit.contacts[0];

        // Handle typo in your GlobalRefrences (should be GlobalReferences)
        if (GlobalReferences.Instance?.bulletImpactEffectPrefab != null)
        {
            GameObject hole = Instantiate(
                GlobalReferences.Instance.bulletImpactEffectPrefab,
                contact.point,
                Quaternion.LookRotation(contact.normal)
            );
            hole.transform.SetParent(objectHit.gameObject.transform);
        }
        else if (GlobalReferences.Instance?.bulletImpactEffectPrefab != null)
        {
            GameObject hole = Instantiate(
                GlobalReferences.Instance.bulletImpactEffectPrefab,
                contact.point,
                Quaternion.LookRotation(contact.normal)
            );
            hole.transform.SetParent(objectHit.gameObject.transform);
        }
    }
}