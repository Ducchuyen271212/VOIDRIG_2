// Weapon.cs
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
    [SerializeField] private SoundData soundData;
    [SerializeField] private GameObject gunDataHolder;

    [Header("Positioning")]
    public Vector3 spawnPosition; // in-hand position
    public Vector3 spawnRotation; // in-hand rotation

    [Header("Shotgun Settings")]
    [Tooltip("Number of pellets per shot")]
    public int pelletCount = 8;
    [Tooltip("Half-angle of horizontal fan in degrees (0 = no spread)")]
    public float spreadIntensity = 3f;
    [Tooltip("Tilt the entire fan around barrel forward (0 = horizontal)")]
    public float tiltAngle = 0f;

    [Header("State")]
    public bool isActiveWeapon = false;
    private bool hasBeenPickedUp = false;

    // runtime stats
    private GunData.Attribute activeGun;
    private SoundData.WeaponSound activeSound;
    private int currentAmmo, totalAmmo;
    private float fireRate, bulletVelocity, bulletLifeTime, reloadTime;

    // input
    private PlayerInput playerInput;
    private InputAction attackAction, reloadAction;

    private bool readyToShoot = true;
    private float lastShotTime = 0f;

    void Awake()
    {
        // cache bullet spawn fallback
        if (bulletSpawn == null) bulletSpawn = transform;
        playerInput = GetComponentInParent<PlayerInput>();
    }

    void Start()
    {
        if (gunDataHolder != null)
            InitializeWeaponData();
    }

    void OnEnable()
    {
        if (playerInput?.actions != null)
        {
            attackAction = playerInput.actions["Attack"];
            reloadAction = playerInput.actions["Reload"];
            attackAction.Enable();
            reloadAction.Enable();
            isActiveWeapon = true;
            readyToShoot = true;
        }
    }

    void OnDisable()
    {
        isActiveWeapon = false;
        attackAction?.Disable();
        reloadAction?.Disable();
    }

    void Update()
    {
        if (!isActiveWeapon || activeGun == null) return;

        // fire input
        bool wasPressed = attackAction.WasPressedThisFrame();
        if (wasPressed && Time.time - lastShotTime >= fireRate && readyToShoot)
        {
            if (currentAmmo > 0)
                StartCoroutine(FireShotgun());
            else
                PlayEmpty();
        }

        // reload input
        if (reloadAction.WasPressedThisFrame())
            TryReload();
    }

    private void InitializeWeaponData()
    {
        var gd = gunDataHolder.GetComponent<GunData>();
        activeGun = gd != null ? gd.shotGun : null;
        if (activeGun == null) return;

        // apply stats
        currentAmmo = activeGun.magazineCapacity;
        totalAmmo = activeGun.totalAmmo;
        fireRate = activeGun.fireRate;
        bulletVelocity = activeGun.bulletVelocity;
        bulletLifeTime = activeGun.bulletLifeTime;
        reloadTime = activeGun.reloadTime;
        activeSound = soundData?.shotGun;
    }

    private IEnumerator FireShotgun()
    {
        readyToShoot = false;
        lastShotTime = Time.time;

        // play muzzle
        muzzleEffect?.GetComponent<ParticleSystem>()?.Play();
        activeSound?.shootClip.Exec(PlayOneShot);

        // generate base direction toward crosshair:
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 target = Physics.Raycast(ray, out var hit, 1000f)
            ? hit.point
            : ray.GetPoint(100f);
        Vector3 baseDir = (target - bulletSpawn.position).normalized;

        // evenly spaced yaw offsets from –spread…+spread
        for (int i = 0; i < pelletCount; i++)
        {
            float t = pelletCount > 1 ? (float)i / (pelletCount - 1) : 0.5f;
            float yawOffset = Mathf.Lerp(-spreadIntensity, spreadIntensity, t);

            // 1) yaw around world up for perfect horizontal fan
            Quaternion yaw = Quaternion.AngleAxis(yawOffset, Vector3.up);
            Vector3 dir = yaw * baseDir;

            // 2) then tilt around the barrel forward
            dir = Quaternion.AngleAxis(tiltAngle, baseDir) * dir;

            // spawn
            var b = Instantiate(bulletPrefab, bulletSpawn.position, Quaternion.LookRotation(dir));
            if (b.TryGetComponent(out Bullet bs)) bs.damage = activeGun.damage;
            if (b.TryGetComponent(out Rigidbody rb)) rb.AddForce(dir * bulletVelocity, ForceMode.Impulse);
            Destroy(b, bulletLifeTime);
        }

        currentAmmo--;
        UpdateAmmoUI();

        // recover
        yield return new WaitForSeconds(fireRate);
        readyToShoot = true;
    }

    private void TryReload()
    {
        if (currentAmmo >= activeGun.magazineCapacity || totalAmmo <= 0) return;
        StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        activeSound?.reloadClip.Exec(PlayOneShot);
        yield return new WaitForSeconds(reloadTime);

        int needed = activeGun.magazineCapacity - currentAmmo;
        int take = Mathf.Min(needed, totalAmmo);
        currentAmmo += take;
        totalAmmo -= take;
        UpdateAmmoUI();
    }

    private void PlayEmpty()
    {
        activeSound?.emptyClip.Exec(PlayOneShot);
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip != null)
            GetComponent<AudioSource>()?.PlayOneShot(clip);
    }

    private void UpdateAmmoUI()
    {
        if (AmmoManager.Instance?.ammoDisplay != null)
            AmmoManager.Instance.ammoDisplay.text = $"{currentAmmo} / {totalAmmo}";
    }
}

// Optional extension method for safe AudioSource play
static class ClipExt
{
    public static void Exec(this AudioClip clip, System.Action<AudioClip> fn)
    {
        if (clip != null && fn != null) fn(clip);
    }
}
