// AmmoManager.cs
using UnityEngine;
using TMPro;

public class AmmoManager : MonoBehaviour
{
    public static AmmoManager Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI ammoDisplay;

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

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}