// ModularCompatibleBullet.cs
using UnityEngine;

// This bullet works with both your existing system and the modular system
public class ModularCompatibleBullet : MonoBehaviour
{
    public float damage = 10f; // This will be set by Weapon.cs or ModularWeapon
    public ProjectileType projectileType = ProjectileType.Physical;

    private void OnCollisionEnter(Collision objectHit)
    {
        GameObject hitObject = objectHit.gameObject;
        ContactPoint contact = objectHit.contacts[0];

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

    void CreateBulletImpactEffect(Collision objectHit)
    {
        ContactPoint contact = objectHit.contacts[0];

        // Handle typo in your GlobalRefrences (should be GlobalReferences)
        if (GlobalRefrences.Instance?.bulletImpactEffectPrefab != null)
        {
            GameObject hole = Instantiate(
                GlobalRefrences.Instance.bulletImpactEffectPrefab,
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