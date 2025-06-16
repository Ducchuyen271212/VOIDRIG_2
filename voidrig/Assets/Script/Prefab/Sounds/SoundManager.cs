using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Sound Data Asset")]
    public SoundData soundData;

    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayShoot(SoundData.WeaponSound weaponSound)
    {
        PlaySound(soundData.GetShootClip(weaponSound));
    }

    public void PlayReload(SoundData.WeaponSound weaponSound)
    {
        PlaySound(soundData.GetReloadClip(weaponSound));
    }

    public void PlayEmpty(SoundData.WeaponSound weaponSound)
    {
        PlaySound(soundData.GetEmptyClip(weaponSound));
    }

    public void PlayBurnout(SoundData.WeaponSound weaponSound)
    {
        PlaySound(soundData.GetOverheatClip(weaponSound));
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
            audioSource.PlayOneShot(clip);
    }
}
