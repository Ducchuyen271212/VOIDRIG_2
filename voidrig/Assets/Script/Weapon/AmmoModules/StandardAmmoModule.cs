// StandardAmmoModule.cs - Fixed Version
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

        Debug.Log($"StandardAmmoModule.Initialize called for {weapon?.weaponName}");

        if (weapon?.WeaponData != null)
        {
            maxMagazine = weapon.WeaponData.magazineCapacity;
            reloadTime = weapon.WeaponData.reloadTime;

            Debug.Log($"Weapon data found - Max Magazine: {maxMagazine}, Reload Time: {reloadTime}");

            // Only reset ammo if we haven't been initialized with proper values yet
            if (currentMagazine == 30 && totalAmmo == 120) // Default inspector values
            {
                Debug.Log("Setting initial ammo from weapon data");
                currentMagazine = maxMagazine;
                totalAmmo = weapon.WeaponData.totalAmmo;
            }

            Debug.Log($"Current ammo state - Magazine: {currentMagazine}/{maxMagazine}, Total: {totalAmmo}");
        }
        else
        {
            Debug.LogWarning("No weapon data available during ammo module initialization");
        }
    }

    public void OnWeaponActivated()
    {
        // Stop any reload in progress when switching weapons
        StopAllCoroutines();
        isReloading = false;
    }

    public void OnWeaponDeactivated()
    {
        // Stop reload if switching weapons
        StopAllCoroutines();
        isReloading = false;
    }

    public void OnUpdate() { }

    public int GetCurrentAmmo() => currentMagazine;

    public int GetTotalAmmo() => totalAmmo;

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

        // Play empty sound if available
        if (weapon?.WeaponSound?.emptyClip != null)
        {
            weapon.PlaySound(weapon.WeaponSound.emptyClip);
        }

        return false;
    }

    public bool IsReloading() => isReloading;

    public bool CanReload()
    {
        return !isReloading && currentMagazine < maxMagazine && totalAmmo > 0;
    }

    public IEnumerator Reload()
    {
        if (!CanReload()) yield break;

        isReloading = true;

        // Play reload animation
        weapon?.SetAnimationTrigger("Reload");

        // Play reload sound
        if (weapon?.WeaponSound?.reloadClip != null)
        {
            weapon.PlaySound(weapon.WeaponSound.reloadClip);
        }

        Debug.Log($"Reloading {weapon?.weaponName} for {reloadTime} seconds...");

        yield return new WaitForSeconds(reloadTime);

        // Calculate how much ammo to reload
        int ammoNeeded = maxMagazine - currentMagazine;
        int ammoToReload = Mathf.Min(ammoNeeded, totalAmmo);

        currentMagazine += ammoToReload;
        totalAmmo -= ammoToReload;

        Debug.Log($"Reload complete! Current: {currentMagazine}, Total: {totalAmmo}");

        // Play reload complete animation
        weapon?.SetAnimationTrigger("ReloadRecover");

        isReloading = false;
    }

    public void AddAmmo(int amount)
    {
        totalAmmo += amount;
        Debug.Log($"Added {amount} ammo. Total ammo: {totalAmmo}");
    }
}
// end