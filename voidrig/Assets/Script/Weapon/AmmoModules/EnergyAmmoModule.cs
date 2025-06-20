//EnergyAmmoModule.cs
using System.Collections;
using UnityEngine;

public class EnergyAmmoModule : MonoBehaviour, IAmmoModule
{
    [Header("Energy Settings")]
    public int maxEnergyCapacity = 100;
    public float regenRate = 5f; // Energy per second
    public float overheatThreshold = 80f;
    public float overheatCooldown = 3f;

    private ModularWeapon weapon;
    private float currentEnergy;
    private bool isOverheated = false;
    private float overheatTimer = 0f;

    public void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;
        currentEnergy = maxEnergyCapacity;
        Debug.Log("EnergyAmmoModule initialized");
    }

    public void OnWeaponActivated() { }
    public void OnWeaponDeactivated() { }

    public void OnUpdate()
    {
        if (isOverheated)
        {
            overheatTimer -= Time.deltaTime;
            if (overheatTimer <= 0f)
            {
                isOverheated = false;
                currentEnergy = maxEnergyCapacity * 0.3f; // Start with 30% energy
                Debug.Log("Weapon cooled down");
            }
        }
        else if (currentEnergy < maxEnergyCapacity)
        {
            currentEnergy += regenRate * Time.deltaTime;
            currentEnergy = Mathf.Min(currentEnergy, maxEnergyCapacity);
        }
    }

    public int GetCurrentAmmo() => Mathf.RoundToInt(currentEnergy);
    public int GetTotalAmmo() => maxEnergyCapacity;

    public bool ConsumeAmmo(int amount = 1)
    {
        if (isOverheated) return false;

        if (currentEnergy >= amount)
        {
            currentEnergy -= amount;

            // Check for overheat
            if (currentEnergy <= (maxEnergyCapacity - overheatThreshold))
            {
                isOverheated = true;
                overheatTimer = overheatCooldown;
                Debug.Log("Weapon overheated!");
            }
            return true;
        }
        return false;
    }

    public bool CanReload() => false; // Energy weapons don't reload
    public IEnumerator Reload() { yield break; }
    public void AddAmmo(int amount)
    {
        currentEnergy += amount;
        currentEnergy = Mathf.Min(currentEnergy, maxEnergyCapacity);
    }
}

//end