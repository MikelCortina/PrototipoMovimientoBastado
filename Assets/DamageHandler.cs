using UnityEngine;
using Unity.Netcode;

public class DamageHandler : MonoBehaviour, IDamageable
{
    [SerializeField] private float damageMultiplier = 1.0f;
    [SerializeField] private bool isHeadshotHitbox = false;
    [SerializeField] private HealthSystem healthSystem;

    public void TakeDamage(DamageData data)
    {
        // Solo procesamos daño en el servidor
        if (!NetworkManager.Singleton.IsServer) return;

        if (healthSystem != null)
        {
            float finalDamage = data.amount * damageMultiplier;
            healthSystem.ReduceHealth(finalDamage, data.hitPoint, isHeadshotHitbox);

            // Sonido global para todos los jugadores cerca
            PlayImpactSoundClientRpc(data.hitPoint, isHeadshotHitbox);
        }
    }

    [ClientRpc]
    private void PlayImpactSoundClientRpc(Vector3 pos, bool isHead)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayHitFeedback(pos, isHead);
        }
    }
}