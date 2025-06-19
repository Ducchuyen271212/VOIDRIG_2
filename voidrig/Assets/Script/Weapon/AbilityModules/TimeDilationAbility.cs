//TimeDilationAbility.cs
using UnityEngine;

public class TimeDilationAbility : BaseAbilityModule
{
    [Header("Time Dilation Settings")]
    public float timeScale = 0.3f;

    protected override void OnAbilityActivated()
    {
        Time.timeScale = timeScale;
        Time.fixedDeltaTime = 0.02f * timeScale;

        weapon.SetAnimationTrigger("TimeDilation");

        // Create time distortion effect
        if (abilityData.abilityEffect != null)
        {
            GameObject effect = Instantiate(abilityData.abilityEffect);
            Destroy(effect, abilityData.duration / timeScale); // Adjust for time scale
        }
    }

    protected override void OnAbilityDeactivated()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }
}
//end
