using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Weapon : MonoBehaviour
{
    [Header("References")]
    public GameObject bulletPrefab;
    public Transform bulletSpawn;
    public GameObject muzzleEffect;

    [Header("Data")]
    [SerializeField] private GunData gunData;
    [SerializeField] private SoundData soundData;

    private GunData.Attribute activeGun;
    private SoundData.WeaponSound activeSound;

    private int currentAmmo;
    private int magazineCapacity;
    private int totalAmmo;
    private float reloadTime;
    private float fireRate;
    private float bulletVelocity;
    private float bulletLifeTime;
    private float spreadIntensity;
    private int bulletsPerBurst;
    private float burstFireInterval;

    private GunData.ShootingMode[] availableModes;
    private int currentModeIndex;
    private GunData.ShootingMode currentShootingMode;
    private int burstBulletsLeft;

    private bool isShooting = false;
    private bool readyToShoot = true;
    private bool allowReset = true;
    private bool isReloading = false;

    private PlayerInput playerInput;
    private InputAction attackAction;
    private InputAction reloadAction;
    private InputAction switchModeAction;

    private Animator animator;
    private AudioSource audioSource;


    private void Awake()
    {
        readyToShoot = true;
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
    }

    private void Start()
    {
        switch (gameObject.tag)
        {
            case "MachineGun": SetActiveGun(gunData.machineGun, soundData.machineGun); break;
            case "ShotGun": SetActiveGun(gunData.shotGun, soundData.shotGun); break;
            case "Sniper": SetActiveGun(gunData.sniper, soundData.sniper); break;
            case "HandGun": SetActiveGun(gunData.handGun, soundData.handGun); break;
            case "SMG": SetActiveGun(gunData.smg, soundData.smg); break;
            case "BurstRifle": SetActiveGun(gunData.burstRifle, soundData.burstRifle); break;
            default:
                Debug.LogWarning("Unknown weapon tag: " + gameObject.tag + ". Defaulting to MachineGun.");
                SetActiveGun(gunData.machineGun, soundData.machineGun);
                break;
        }
    }

    private void OnEnable()
    {
        playerInput = GetComponentInParent<PlayerInput>();
        if (playerInput == null || playerInput.actions == null)
        {
            Debug.LogError("PlayerInput or its actions are not initialized!");
            return;
        }

        attackAction = playerInput.actions["Attack"];
        reloadAction = playerInput.actions["Reload"];
        switchModeAction = playerInput.actions["SwitchMode"];

        attackAction.Enable();
        reloadAction.Enable();
        switchModeAction.Enable();

        /*
        reloadAction.performed += ctx => TryReload();
        switchModeAction.performed += ctx => SwitchMode();
        */

    }

    private void OnDisable()
    {
        /*
        if (reloadAction != null) reloadAction.performed -= ctx => TryReload();
        if (switchModeAction != null) switchModeAction.performed -= ctx => SwitchMode();
        */
    }

    private void Update()
    {
        isShooting = currentShootingMode == GunData.ShootingMode.Auto
            ? attackAction.IsPressed()
            : attackAction.WasPressedThisFrame();

        if (readyToShoot && isShooting)
        {
            if (currentAmmo > 0)
            {
                if (animator.GetCurrentAnimatorClipInfo(0)[0].clip.name != "Recoil")
                {
                    SetAnimTrigger("Recoil");
                }
                PlaySound(soundData.GetShootClip(activeSound));
                burstBulletsLeft = bulletsPerBurst;
                //SetAnimTrigger("RecoilHigh");
                StartCoroutine(ShootRepeatedly());
            }
            else
            {
                SetAnimTrigger("RecoilRecover");
                PlaySound(soundData.GetEmptyClip(activeSound));
                animator.Play("RecoilRecover");
            }
        }

        if (reloadAction.WasPressedThisFrame())
        {
            TryReload();
        }

        if (AmmoManager.Instance.ammoDisplay != null)
        {
            AmmoManager.Instance.ammoDisplay.text = $"{currentAmmo} / {totalAmmo}";
        }
    }

    private IEnumerator ShootRepeatedly()
    {
        if (activeGun.scatter)
        {
            if (currentAmmo > 0 && !isReloading)
            {
                for (int i = 0; i < bulletsPerBurst; i++)
                {
                    FireBullet(ignoreAmmo: true); // fire 40 pellets
                }

                currentAmmo--; // use 1 shell
            }

            SetAnimTrigger("RecoilHigh");
            PlaySound(soundData.GetShootClip(activeSound));
        }
        else
        {
            // Normal guns
            while (burstBulletsLeft > 0 && currentAmmo > 0 && !isReloading)
            {
                FireBullet();
                SetAnimTrigger("RecoilHigh");
                PlaySound(soundData.GetShootClip(activeSound));
                burstBulletsLeft--;

                if (currentShootingMode == GunData.ShootingMode.Single)
                    break;

                yield return new WaitForSeconds(burstFireInterval);
            }
        }

        if (currentShootingMode == GunData.ShootingMode.Auto && attackAction.WasReleasedThisFrame())
        {
            ResetShot();
        }

        if (currentShootingMode != GunData.ShootingMode.Auto)
        {
            ResetShot();
        }

    }


    private void FireBullet(bool ignoreAmmo = false)
    {
        if (!ignoreAmmo && currentAmmo <= 0)
        {
            Debug.Log(totalAmmo <= 0 ? "Completely out of ammo!" : "Magazine empty! Press reload.");
            return;
        }

        muzzleEffect?.GetComponent<ParticleSystem>()?.Play();

        Vector3 shootingDirection = CalculateDirectionAndSpread().normalized;

        GameObject bullet = Instantiate(bulletPrefab, bulletSpawn.position, Quaternion.identity);

        if (bullet.TryGetComponent(out Bullet bulletScript))
        {
            bulletScript.damage = activeGun.damage;
        }

        bullet.transform.forward = shootingDirection;
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        rb.AddForce(shootingDirection * bulletVelocity, ForceMode.Impulse);
        Destroy(bullet, bulletLifeTime);

        if (!ignoreAmmo)
            currentAmmo--;

        Debug.Log("Bullet fired! Remaining ammo: " + currentAmmo + "/" + totalAmmo);
    }


    private void TryReload()
    {
        if (!isReloading)
        {
            isReloading = true;
            Debug.Log("Reloading...");
            SetAnimTrigger("Reload");
            PlaySound(soundData.GetReloadClip(activeSound));
            StartCoroutine(ReloadWeapon(reloadTime));
        }
    }

    private IEnumerator ReloadWeapon(float reloadTime)
    {
        yield return new WaitForSeconds(reloadTime);

        int missingAmmo = magazineCapacity - currentAmmo;

        if (totalAmmo <= 0)
        {
            Debug.Log("No ammo in reserve to reload!");
        }
        else if (totalAmmo < missingAmmo)
        {
            currentAmmo += totalAmmo;
            totalAmmo = 0;
        }
        else
        {
            currentAmmo = magazineCapacity;
            totalAmmo -= missingAmmo;
        }

        Debug.Log("Reloaded. Ammo: " + currentAmmo + "/" + totalAmmo);
        SetAnimTrigger("ReloadRecover");

        isReloading = false;
    }

    private void ResetShot()
    {
        readyToShoot = true;
        allowReset = true;
        SetAnimTrigger("RecoilRecover");
        animator.Play("Idle");
    }

    private void SetActiveGun(GunData.Attribute gun, SoundData.WeaponSound sound)
    {
        activeGun = gun;
        activeSound = sound;

        magazineCapacity = gun.magazineCapacity;
        currentAmmo = magazineCapacity;
        totalAmmo = gun.totalAmmo;
        reloadTime = gun.reloadTime;
        fireRate = gun.fireRate;
        bulletVelocity = gun.bulletVelocity;
        bulletLifeTime = gun.bulletLifeTime;
        spreadIntensity = gun.spreadIntensity;
        bulletsPerBurst = Mathf.Max(1, gun.bulletsPerBurst);
        burstFireInterval = gun.burstFireInterval;

        availableModes = gun.availableModes != null && gun.availableModes.Length > 0
            ? gun.availableModes
            : new GunData.ShootingMode[] { gun.shootingMode };

        currentModeIndex = 0;
        currentShootingMode = availableModes[currentModeIndex];
        burstBulletsLeft = bulletsPerBurst;

        Debug.Log($"Equipped weapon: {gun} | Mode: {currentShootingMode} | Ammo: {currentAmmo}/{totalAmmo}");
    }

    public void SwitchMode()
    {
        if (availableModes.Length > 1)
        {
            currentModeIndex = (currentModeIndex + 1) % availableModes.Length;
            currentShootingMode = availableModes[currentModeIndex];
            Debug.Log("Switched shooting mode to: " + currentShootingMode);
        }
    }

    private Vector3 CalculateDirectionAndSpread()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 targetPoint = Physics.Raycast(ray, out RaycastHit hit) ? hit.point : ray.GetPoint(100);
        Vector3 direction = (targetPoint - bulletSpawn.position).normalized;

        // Spread is now angular — better for realism
        float spreadAngle = spreadIntensity;
        Quaternion spreadRotation = Quaternion.Euler(
            Random.Range(-spreadAngle, spreadAngle),
            Random.Range(-spreadAngle, spreadAngle),
            0
        );

        return spreadRotation * direction;
    }


    private void SetAnimTrigger(string triggerName)
    {
        if (animator == null)
        {
            Debug.LogError("Animator not assigned.");
            return;
        }

        if (!animator.HasParameterOfType(triggerName, AnimatorControllerParameterType.Trigger))
        {
            Debug.LogWarning($"Missing trigger in Animator: {triggerName}");
            return;
        }

        Debug.Log($"Setting animation trigger: {triggerName}");
        animator.SetTrigger(triggerName);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null) audioSource.PlayOneShot(clip);
    }
}

public static class AnimatorExtensions
{
    public static bool HasParameterOfType(this Animator animator, string name, AnimatorControllerParameterType type)
    {
        foreach (var param in animator.parameters)
            if (param.name == name && param.type == type)
                return true;
        return false;
    }
}
