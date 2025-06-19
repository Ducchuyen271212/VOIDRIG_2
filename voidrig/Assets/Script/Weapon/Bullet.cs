//Bullet.cs
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float damage = 10f; // This will be set by Weapon.cs

    private void OnCollisionEnter(Collision objectHit)
    {
        GameObject hitObject = objectHit.gameObject;

        // Apply damage if the object has a health component
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

        GameObject hole = Instantiate(
            GlobalRefrences.Instance.bulletImpactEffectPrefab,
            contact.point,
            Quaternion.LookRotation(contact.normal)
        );

        hole.transform.SetParent(objectHit.gameObject.transform);
    }
}
