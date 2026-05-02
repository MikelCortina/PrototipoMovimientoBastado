using UnityEngine;
using Fusion;

// Cualquier objeto que tenga esta interfaz, podrá recibir daño
public interface IFusionDamageable
{
    void TakeDamage(FusionDamageData data);
}

// Tipos de daño por si luego quieres hacer que el fuego queme o el gas ahogue
public enum FusionDamageType
{
    Bullet,
    Grenade,
    AirStrike,
    Turret,
    Fall,
    Gas
}

// El "paquete" de datos que la bala le entrega al jugador
public struct FusionDamageData
{
    public float amount;
    public Vector3 hitPoint;
    public Vector3 hitNormal;
    public FusionDamageType type;
    public NetworkObject instigator; // Quién disparó la bala
}