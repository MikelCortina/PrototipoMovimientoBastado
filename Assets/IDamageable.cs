using UnityEngine;

public interface IDamageable
{
    void TakeDamage(DamageData data);
}

// Estructura para pasar información completa del impacto
public struct DamageData
{
    public float amount;
    public Vector3 hitPoint;
    public Vector3 hitNormal;
    public DamageType type; // Ej: Bala, Explosión, Gas
    public GameObject instigator; // Quién disparó
}