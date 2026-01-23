using Photon.Pun;
using UnityEngine;

public class DamageHandler : MonoBehaviour, IDamageable
{
    [Header("Settings")]
    [SerializeField] private float damageMultiplier = 1.0f;
    [SerializeField] private bool isHeadshotHitbox = false; // <-- Nueva variable

    [SerializeField] private HealthSystem healthSystem;

    public void TakeDamage(DamageData data)
    {
        // SEGURIDAD: Si no se asignó en el inspector, lo buscamos en el padre
        if (healthSystem == null)
        {
            healthSystem = GetComponentInParent<HealthSystem>();
        }

        // Si después de buscar sigue siendo nulo, lanzamos un error claro
        if (healthSystem == null)
        {
            Debug.LogError($"No se encontró HealthSystem en el objeto {gameObject.name} ni en sus padres.");
            return;
        }

        float finalDamage = data.amount * damageMultiplier;

        // Error original corregido: Usamos GetComponent<PhotonView>() por seguridad
        PhotonView pv = healthSystem.GetComponent<PhotonView>();
        if (pv != null)
        {
            pv.RPC("ReduceHealth", RpcTarget.All, finalDamage, data.hitPoint);
        }
        else
        {
            Debug.LogError("El HealthSystem no tiene un PhotonView adjunto.");
        }

        // Feedback de sonido
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayHitSound(data.hitPoint, isHeadshotHitbox);
        }
    }

}