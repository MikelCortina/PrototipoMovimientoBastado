using UnityEngine;
using Unity.Netcode;

// 1. EL ENUM
public enum DamageType { Bullet, Explosive, Melee, Gas, Zone }

// 2. LA INTERFAZ
public interface IDamageable
{
    void TakeDamage(DamageData data);
}

// 3. LA ESTRUCTURA DE DATOS (Sincronizable por red)
public struct DamageData : INetworkSerializable
{
    public float amount;
    public Vector3 hitPoint;
    public Vector3 hitNormal;
    public DamageType type;
    public ulong instigatorId;

    // Requerido para enviar DamageData a través de RPCs de Netcode
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref amount);
        serializer.SerializeValue(ref hitPoint);
        serializer.SerializeValue(ref hitNormal);
        serializer.SerializeValue(ref type);
        serializer.SerializeValue(ref instigatorId);
    }
}