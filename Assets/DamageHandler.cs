using UnityEngine;

public class DamageHandler : MonoBehaviour, IDamageable
{
    [Header("Settings")]
    [SerializeField] private float damageMultiplier = 1.0f;
    [SerializeField] private bool isHeadshotHitbox = false; // <-- Nueva variable

    [SerializeField] private HealthSystem healthSystem;

    public void TakeDamage(DamageData data)
    {
        if (healthSystem == null) return;

        float finalDamage = data.amount * damageMultiplier;

        // Enviamos el daño al sistema central
        healthSystem.ReduceHealth(finalDamage, data.hitPoint);

        // Feedback de sonido diferenciado
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayHitSound(data.hitPoint, isHeadshotHitbox);
        }

        // Opcional: Log para depurar
        Debug.Log($"Impacto en {(isHeadshotHitbox ? "CABEZA" : "CUERPO")}. Daño: {finalDamage}");
    }
}