using UnityEngine;

public class AggroCheck: MonoBehaviour
{
    public GameObject player { get; set; }

    private Enemy Enemy;

    private SphereCollider sphereCollider;

    private void Awake()
    {
        Debug.Log("Enter!!");
        player = GameObject.FindGameObjectWithTag("Player");
        Enemy = GetComponentInParent<Enemy>();
        sphereCollider = GetComponent<SphereCollider>();
        if (Enemy != null)
        {
            Debug.Log("Enemy component found in parent.");
        }
        else
        {
            Debug.LogWarning("Enemy component NOT found in parent!");
        }

        if (sphereCollider != null)
        {
            Debug.Log("sphere component found in parent.");
        }
        else
        {
            Debug.LogWarning("sphere component NOT found in parent!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        
        //Debug.Log("Collided with: " + other.gameObject.name);
        if (other.gameObject == player)
        {
            Debug.Log("IsAggroed default is : " + Enemy.IsAggro);
            Enemy.setAggroStatus(true);
            //Enemy.changeState(Enemy.ChaseState);
            Debug.Log("Aggroed the player!");
            Debug.Log("IsAggroed set to: " + Enemy.IsAggro);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Enemy.setAggroStatus(false);
    }
}
