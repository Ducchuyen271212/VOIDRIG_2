//GlobalReferences.cs - Fixed Version
using UnityEngine;

public class GlobalReferences : MonoBehaviour
{
    public static GlobalReferences Instance { get; private set; }

    [Header("Bullet Effects")]
    public GameObject bulletImpactEffectPrefab;

    [Header("Explosion Effects")]
    public GameObject explosionEffectPrefab;

    [Header("Muzzle Effects")]
    public GameObject muzzleFlashEffectPrefab;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnValidate()
    {
        // Help message in inspector
        if (bulletImpactEffectPrefab == null)
        {
            Debug.LogWarning("GlobalReferences: No bullet impact effect prefab assigned!");
        }
    }
}