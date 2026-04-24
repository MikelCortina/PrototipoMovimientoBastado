using Fusion;
using UnityEngine;

public class FusionPlayerWeapons : NetworkBehaviour
{
    [Header("Armas configuradas")]
    public WeaponData basicWeapon;
    public WeaponData normalWeapon;
    public WeaponData specialWeapon;
    public WeaponData epicWeapon;

    [Networked] public int currentWeaponIndex { get; set; }

    private FusionWeaponR301 weaponShooter;
    private int _lastAppliedWeaponIndex = -1;

    public override void Spawned()
    {
        weaponShooter = GetComponent<FusionWeaponR301>();

        if (HasStateAuthority)
            currentWeaponIndex = 0;

        ApplyCurrentWeapon(force: true);
    }

    public override void Render()
    {
        ApplyCurrentWeapon(force: false);
    }

    public WeaponData GetCurrentWeaponData()
    {
        switch (currentWeaponIndex)
        {
            case 0: return basicWeapon;
            case 1: return normalWeapon;
            case 2: return specialWeapon;
            case 3: return epicWeapon;
            default: return basicWeapon;
        }
    }

    public void EquipWeaponByIndex(int weaponIndex)
    {
        if (!HasStateAuthority)
            return;

        currentWeaponIndex = Mathf.Clamp(weaponIndex, 0, 3);
        ApplyCurrentWeapon(force: true);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_EquipWeaponByIndex(int weaponIndex)
    {
        if (!HasStateAuthority)
            return;

        EquipWeaponByIndex(weaponIndex);
    }

    private void ApplyCurrentWeapon(bool force)
    {
        if (!force && _lastAppliedWeaponIndex == currentWeaponIndex)
            return;

        if (weaponShooter == null)
            weaponShooter = GetComponent<FusionWeaponR301>();

        if (weaponShooter == null)
            return;

        WeaponData data = GetCurrentWeaponData();
        if (data == null)
            return;

        weaponShooter.damage = data.damage;
        weaponShooter.fireRate = data.fireRate;
        weaponShooter.bulletSpeed = data.bulletSpeed;
        weaponShooter.bulletGravity = data.bulletGravity;

        weaponShooter.isUsingShotgunModeAsMainWeapon = data.isShotgun;
        weaponShooter.mainWeaponPellets = Mathf.Max(1, data.pellets);
        weaponShooter.mainWeaponSpreadDegrees = data.spreadDegrees;
        weaponShooter.mainWeaponDamageMultiplier = data.damageMultiplier <= 0f ? 1f : data.damageMultiplier;

        _lastAppliedWeaponIndex = currentWeaponIndex;
    }
}