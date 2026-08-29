using UnityEngine;

public enum WeaponReloadMode
{
    OneByOne,
    FullMagazine
}

[CreateAssetMenu(
    fileName = "RangedWeaponDefinition",
    menuName = "EXVS/武装/射撃武装設定"
)]
public class RangedWeaponDefinition : ScriptableObject
{
    [Header("基本情報")]
    [SerializeField] private string weaponName = "New Weapon";
    [SerializeField] private HomingProjectile projectilePrefab;

    [Header("弾数・リロード")]
    [SerializeField] private int maxAmmo = 1;
    [SerializeField] private float reloadTime = 3f;
    [SerializeField] private WeaponReloadMode reloadMode = WeaponReloadMode.OneByOne;

    [Header("発射")]
    [SerializeField] private float cooldown = 0.25f;
    [SerializeField] private float actionLockDuration = 0.35f;
    [SerializeField] private float startupTime;
    [SerializeField] private float recoilSpeed;

    [Header("連続発射")]
    [SerializeField] private int projectileCount = 1;
    [SerializeField] private float projectileInterval;
    [SerializeField] private float spreadAngle;

    [Header("チャージ")]
    [SerializeField] private float chargeTime = 2f;

    public string WeaponName => weaponName;
    public HomingProjectile ProjectilePrefab => projectilePrefab;
    public int MaxAmmo => maxAmmo;
    public float ReloadTime => reloadTime;
    public WeaponReloadMode ReloadMode => reloadMode;
    public float Cooldown => cooldown;
    public float ActionLockDuration => actionLockDuration;
    public float StartupTime => startupTime;
    public float RecoilSpeed => recoilSpeed;
    public int ProjectileCount => projectileCount;
    public float ProjectileInterval => projectileInterval;
    public float SpreadAngle => spreadAngle;
    public float ChargeTime => chargeTime;

    public int GetReloadedAmmo(int currentAmmo)
    {
        return reloadMode == WeaponReloadMode.FullMagazine
            ? maxAmmo
            : Mathf.Min(currentAmmo + 1, maxAmmo);
    }

    public bool ShouldStartReload(int currentAmmo)
    {
        if (currentAmmo >= maxAmmo)
        {
            return false;
        }

        // 撃ち切りリロードは残弾が0になってからリロードを開始する。
        return reloadMode == WeaponReloadMode.OneByOne || currentAmmo <= 0;
    }

    private void OnValidate()
    {
        maxAmmo = Mathf.Max(1, maxAmmo);
        reloadTime = Mathf.Max(0.01f, reloadTime);
        cooldown = Mathf.Max(0f, cooldown);
        actionLockDuration = Mathf.Max(0f, actionLockDuration);
        startupTime = Mathf.Max(0f, startupTime);
        recoilSpeed = Mathf.Max(0f, recoilSpeed);
        projectileCount = Mathf.Max(1, projectileCount);
        projectileInterval = Mathf.Max(0f, projectileInterval);
        spreadAngle = Mathf.Clamp(spreadAngle, 0f, 180f);
        chargeTime = Mathf.Max(0.01f, chargeTime);
    }
}
