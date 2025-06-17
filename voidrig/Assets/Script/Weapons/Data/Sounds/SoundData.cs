using UnityEngine;

[CreateAssetMenu(fileName = "SoundData", menuName = "Weapon/Sound Data")]
public class SoundData : ScriptableObject
{
    [System.Serializable]
    public class WeaponSound
    {
        public AudioClip shootClip;
        public AudioClip reloadClip;
        public AudioClip emptyClip;
        public AudioClip overheatClip;
    }

    [Header("Default Fallback Sounds")]
    public AudioClip defaultShootClip;
    public AudioClip defaultReloadClip;
    public AudioClip defaultEmptyClip;
    public AudioClip defaultOverheatClip;

    [Header("Weapon-Specific Sounds")]
    public WeaponSound machineGun;
    public WeaponSound shotGun;
    public WeaponSound sniper;
    public WeaponSound handGun;
    public WeaponSound smg;
    public WeaponSound burstRifle;

    // === Utility Methods ===

    public AudioClip GetShootClip(WeaponSound sound)
    {
        return sound != null && sound.shootClip != null ? sound.shootClip : defaultShootClip;
    }

    public AudioClip GetReloadClip(WeaponSound sound)
    {
        return sound != null && sound.reloadClip != null ? sound.reloadClip : defaultReloadClip;
    }

    public AudioClip GetEmptyClip(WeaponSound sound)
    {
        return sound != null && sound.emptyClip != null ? sound.emptyClip : defaultEmptyClip;
    }

    public AudioClip GetOverheatClip(WeaponSound sound)
    {
        return sound != null && sound.overheatClip != null ? sound.overheatClip : defaultOverheatClip;
    }
}
