//BaseAbilityModule.cs
using UnityEngine;

public abstract class BaseAbilityModule : MonoBehaviour, IAbilityModule
{
    [Header("Ability Settings")]
    public AbilityData abilityData;

    protected ModularWeapon weapon;
    protected float lastActivationTime = -999f;
    protected bool isActive = false;

    public virtual void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;
    }

    public virtual void OnWeaponActivated() { }
    public virtual void OnWeaponDeactivated()
    {
        if (isActive)
        {
            DeactivateAbility();
        }
    }

    public virtual void OnUpdate()
    {
        if (isActive && abilityData.duration > 0)
        {
            if (Time.time >= lastActivationTime + abilityData.duration)
            {
                DeactivateAbility();
            }
        }
    }

    public virtual bool CanActivate()
    {
        return Time.time >= lastActivationTime + abilityData.cooldown && !isActive;
    }

    public virtual void ActivateAbility()
    {
        if (!CanActivate()) return;

        lastActivationTime = Time.time;
        isActive = true;
        OnAbilityActivated();
    }

    protected virtual void DeactivateAbility()
    {
        isActive = false;
        OnAbilityDeactivated();
    }

    protected abstract void OnAbilityActivated();
    protected virtual void OnAbilityDeactivated() { }

    public float GetCooldownRemaining()
    {
        float timeRemaining = (lastActivationTime + abilityData.cooldown) - Time.time;
        return Mathf.Max(0f, timeRemaining);
    }

    public string GetAbilityName() => abilityData.abilityName;
}
//end
