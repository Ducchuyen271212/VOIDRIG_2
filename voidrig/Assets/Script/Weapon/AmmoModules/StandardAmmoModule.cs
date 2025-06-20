// StandardAmmoModule.cs
using System.Collections;
using UnityEngine;

public class StandardAmmoModule : MonoBehaviour, IAmmoModule
{
    [Header("Ammo Settings")]
    public int currentMagazine = 30;
    public int maxMagazine = 30;
    public int totalAmmo = 120;
    public float reloadTime = 2f;

    private ModularWeapon weapon;
    private bool isReloading = false;

    public void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;
        Debug.Log("StandardAmmoModule initialized");
    }

    public void OnWeaponActivated()
    {
        StopAllCoroutines();
        isReloading = false;
    }

    public void OnWeaponDeactivated()
    {
        StopAllCoroutines();
        isReloading = false;
    }

    public void OnUpdate() { }

    public int GetCurrentAmmo() => currentMagazine;
    public int GetTotalAmmo() => totalAmmo;
    public bool IsReloading() => isReloading;

    public bool ConsumeAmmo(int amount = 1)
    {
        if (isReloading)
        {
            Debug.Log("Cannot consume ammo - weapon is reloading");
            return false;
        }

        if (currentMagazine >= amount)
        {
            currentMagazine -= amount;
            return true;
        }

        Debug.Log("Out of ammo!");
        return false;
    }

    public bool CanReload()
    {
        return !isReloading && currentMagazine < maxMagazine && totalAmmo > 0;
    }

    public IEnumerator Reload()
    {
        if (!CanReload()) yield break;

        isReloading = true;
        Debug.Log($"Reloading for {reloadTime} seconds...");

        yield return new WaitForSeconds(reloadTime);

        // Calculate how much ammo to reload
        int ammoNeeded = maxMagazine - currentMagazine;
        int ammoToReload = Mathf.Min(ammoNeeded, totalAmmo);

        currentMagazine += ammoToReload;
        totalAmmo -= ammoToReload;

        Debug.Log($"Reload complete! Current: {currentMagazine}, Total: {totalAmmo}");
        isReloading = false;
    }

    public void AddAmmo(int amount)
    {
        totalAmmo += amount;
        Debug.Log($"Added {amount} ammo. Total ammo: {totalAmmo}");
    }
}
// end