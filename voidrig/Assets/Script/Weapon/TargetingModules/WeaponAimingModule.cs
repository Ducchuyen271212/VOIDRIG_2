// WeaponAimingModule.cs - Complete per-weapon customizable aiming offsets
using UnityEngine;

public class WeaponAimingModule : MonoBehaviour, IWeaponModule
{
    [Header("Weapon-Specific Aiming")]
    [Tooltip("Custom aim position offset for this weapon")]
    public Vector3 aimPositionOffset = new Vector3(0, 0, 0.2f);

    [Tooltip("Custom aim rotation offset for this weapon")]
    public Vector3 aimRotationOffset = Vector3.zero;

    [Tooltip("Custom FOV when aiming (0 = use default)")]
    public float customAimFOV = 0f;

    [Tooltip("Custom accuracy multiplier when aiming")]
    public float customAccuracyMultiplier = 0.5f;

    [Header("Aiming Behavior")]
    [Tooltip("Should this weapon be able to aim?")]
    public bool canAim = true;

    [Tooltip("Speed of aim transition")]
    public float aimSpeed = 5f;

    private ModularWeapon weapon;

    public void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;
        Debug.Log($"WeaponAimingModule initialized - Position offset: {aimPositionOffset}");
    }

    public void OnWeaponActivated()
    {
        // Notify AimingManager about the new weapon
        if (AimingManager.Instance != null)
        {
            AimingManager.Instance.SetCurrentWeapon(weapon);
        }
    }

    public void OnWeaponDeactivated() { }
    public void OnUpdate() { }

    // Getters for AimingManager to use
    public bool CanAim() => canAim;
    public float GetAimFOV() => customAimFOV > 0 ? customAimFOV : 0;
    public float GetAccuracyMultiplier() => customAccuracyMultiplier;

    // Preset configurations
    [ContextMenu("Preset: Assault Rifle")]
    public void PresetAssaultRifle()
    {
        aimPositionOffset = new Vector3(0, 0.02f, 0.15f);
        aimRotationOffset = Vector3.zero;
        customAimFOV = 45f;
        customAccuracyMultiplier = 0.4f;
        canAim = true;
    }

    [ContextMenu("Preset: Sniper Rifle")]
    public void PresetSniperRifle()
    {
        aimPositionOffset = new Vector3(0, 0.05f, 0.25f);
        aimRotationOffset = Vector3.zero;
        customAimFOV = 20f;
        customAccuracyMultiplier = 0.1f;
        canAim = true;
    }

    [ContextMenu("Preset: Shotgun")]
    public void PresetShotgun()
    {
        aimPositionOffset = new Vector3(0, 0.01f, 0.1f);
        aimRotationOffset = Vector3.zero;
        customAimFOV = 50f;
        customAccuracyMultiplier = 0.7f;
        canAim = true;
    }

    [ContextMenu("Preset: Pistol")]
    public void PresetPistol()
    {
        aimPositionOffset = new Vector3(0, 0.08f, 0.12f);
        aimRotationOffset = Vector3.zero;
        customAimFOV = 50f;
        customAccuracyMultiplier = 0.5f;
        canAim = true;
    }
}
// end