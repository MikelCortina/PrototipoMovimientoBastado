using UnityEngine;
using System;

public class HealthSystem : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 100f;
    public float currentHealth;

    // Evento para que otros sistemas (UI, Sonido) escuchen cuando recibimos daño
    // Pasa: (cantidad de daño, punto de impacto, si es crítico)
    public event Action<float, Vector3, bool> OnDamageReceived;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void ReduceHealth(float amount, Vector3 hitPoint)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // Disparamos el evento para que la UI lo detecte
        OnDamageReceived?.Invoke(amount, hitPoint, false);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log(gameObject.name + " ha sido eliminado.");
        // Aquí iría la lógica de muerte (animación, loot box, etc.)
    }
}