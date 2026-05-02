using Fusion;
using UnityEngine;

public class FusionWeaponPickup : NetworkBehaviour
{
    [Header("0=Basic, 1=Normal, 2=Special, 3=Epic")]
    public int weaponIndex = 1;

    [Header("Visual")]
    public Renderer pickupRenderer;
    public Color rarityColor = Color.white;

    private bool _collected = false;
    private FusionPickupSpawner _spawner;

    public override void Spawned()
    {
        _collected = false;
        ApplyVisuals();
    }

    public void SetSpawner(FusionPickupSpawner spawner)
    {
        _spawner = spawner;
    }

    public void ConfigurePickup(int newWeaponIndex, Color newRarityColor, FusionPickupSpawner spawner)
    {
        weaponIndex = newWeaponIndex;
        rarityColor = newRarityColor;
        _spawner = spawner;

        ApplyVisuals();
    }

    private void ApplyVisuals()
    {
        if (pickupRenderer != null && pickupRenderer.material != null)
        {
            pickupRenderer.material.color = rarityColor;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_collected)
            return;

        FusionPlayerWeapons playerWeapons = other.GetComponentInParent<FusionPlayerWeapons>();
        if (playerWeapons == null)
            return;

        if (Object == null || !Object.IsValid)
            return;

        _collected = true;

        if (playerWeapons.HasStateAuthority)
            playerWeapons.EquipWeaponByIndex(weaponIndex);
        else
            playerWeapons.RPC_EquipWeaponByIndex(weaponIndex);

        if (Object.HasStateAuthority)
        {
            if (_spawner != null)
                _spawner.NotifyPickupCollected(Object);

            Runner.Despawn(Object);
        }
    }
}