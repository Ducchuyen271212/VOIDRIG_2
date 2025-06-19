//InfiniteAmmoModule.cs
using UnityEngine;
using System.Collections;

public class InfiniteAmmoModule : MonoBehaviour, IAmmoModule
{
    private ModularWeapon weapon;

    public void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;
    }

    public void OnWeaponActivated() { }
    public void OnWeaponDeactivated() { }
    public void OnUpdate() { }

    public int GetCurrentAmmo() => 999;
    public int GetTotalAmmo() => 999;

    public bool ConsumeAmmo(int amount = 1) => true; // Never runs out

    public bool CanReload() => false;

    public IEnumerator Reload()
    {
        yield break;
    }

    public void AddAmmo(int amount) { } // Already infinite
}
//end
