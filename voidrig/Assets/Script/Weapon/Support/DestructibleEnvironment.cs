using UnityEngine;

namespace Weapons.Support
{
    public class DestructibleEnvironment : MonoBehaviour
    {
        public float health = 100f;

        public void TakeDamage(float damage)
        {
            health -= damage;
            if (health <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
}
