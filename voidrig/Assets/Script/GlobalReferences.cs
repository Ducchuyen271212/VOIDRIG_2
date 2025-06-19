//GlobalReferences.cs
using UnityEngine;

public class GlobalRefrences : MonoBehaviour
{
    public static GlobalRefrences Instance { get; private set; }

    public GameObject bulletImpactEffectPrefab; // Prefab for bullet impact effect

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
}
