using UnityEngine;

public class Bullet: MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("Hit " +  collision.gameObject.name + " !");
            Destroy(gameObject);
        }
    }
}
