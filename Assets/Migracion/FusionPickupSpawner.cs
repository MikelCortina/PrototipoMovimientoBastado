using System.Collections.Generic;
using Fusion;
using UnityEngine;

[System.Serializable]
public class PickupWeaponRotationEntry
{
    [Header("0=Basic, 1=Normal, 2=Special, 3=Epic")]
    public int weaponIndex = 1;
    public Color rarityColor = Color.white;
    public string debugName = "Normal";
}

public class FusionPickupSpawner : NetworkBehaviour
{
    [Header("Pickup settings")]
    [SerializeField] private NetworkObject pickupPrefab;
    [SerializeField] private float respawnDelay = 10f;

    [Header("Spawn points")]
    [SerializeField] private List<FusionPickupSpawnPoint> spawnPoints = new List<FusionPickupSpawnPoint>();

    [Header("Weapon rotation")]
    [SerializeField] private List<PickupWeaponRotationEntry> weaponRotation = new List<PickupWeaponRotationEntry>();

    [Networked] private TickTimer respawnTimer { get; set; }
    [Networked] private NetworkBool waitingRespawn { get; set; }
    [Networked] private int currentRotationIndex { get; set; }

    private NetworkObject currentPickup;
    private int _lastSpawnPointIndex = -1;

    public override void Spawned()
    {
        Debug.Log($"[PICKUP SPAWNER] Spawned | name: {name} | HasStateAuthority: {HasStateAuthority}");

        if (HasStateAuthority)
        {
            currentRotationIndex = 0;
            SpawnPickupAtNextPointAndWeapon();
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (currentPickup == null && waitingRespawn && respawnTimer.ExpiredOrNotRunning(Runner))
        {
            waitingRespawn = false;
            SpawnPickupAtNextPointAndWeapon();
        }
    }

    public void NotifyPickupCollected(NetworkObject collectedPickup)
    {
        if (!HasStateAuthority)
            return;

        if (collectedPickup != null && collectedPickup == currentPickup)
        {
            currentPickup = null;
            waitingRespawn = true;
            respawnTimer = TickTimer.CreateFromSeconds(Runner, respawnDelay);
        }
    }

    private void SpawnPickupAtNextPointAndWeapon()
    {
        if (pickupPrefab == null)
        {
            Debug.LogWarning("FusionPickupSpawner: pickupPrefab es NULL");
            return;
        }

        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogWarning("FusionPickupSpawner: no hay spawn points asignados");
            return;
        }

        if (weaponRotation == null || weaponRotation.Count == 0)
        {
            Debug.LogWarning("FusionPickupSpawner: no hay weaponRotation configurada");
            return;
        }

        int randomSpawnIndex = GetRandomSpawnPointIndex();
        FusionPickupSpawnPoint chosenPoint = spawnPoints[randomSpawnIndex];
        if (chosenPoint == null)
        {
            Debug.LogWarning("FusionPickupSpawner: chosenPoint es NULL");
            return;
        }

        PickupWeaponRotationEntry entry = weaponRotation[currentRotationIndex];
        if (entry == null)
        {
            Debug.LogWarning("FusionPickupSpawner: entrada de rotación NULL");
            return;
        }

        currentPickup = Runner.Spawn(
            pickupPrefab,
            chosenPoint.transform.position,
            chosenPoint.transform.rotation,
            default
        );

        FusionWeaponPickup pickup = currentPickup.GetComponent<FusionWeaponPickup>();
        if (pickup != null)
        {
            pickup.ConfigurePickup(entry.weaponIndex, entry.rarityColor, this);
        }

        Debug.Log($"Pickup spawneado en {chosenPoint.name} | Tipo: {entry.debugName} | weaponIndex: {entry.weaponIndex}");

        currentRotationIndex++;
        if (currentRotationIndex >= weaponRotation.Count)
            currentRotationIndex = 0;
    }

    private int GetRandomSpawnPointIndex()
    {
        if (spawnPoints.Count == 1)
            return 0;

        int randomIndex = Random.Range(0, spawnPoints.Count);

        if (randomIndex == _lastSpawnPointIndex)
        {
            randomIndex++;
            if (randomIndex >= spawnPoints.Count)
                randomIndex = 0;
        }

        _lastSpawnPointIndex = randomIndex;
        return randomIndex;
    }
}