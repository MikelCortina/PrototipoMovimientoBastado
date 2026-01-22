using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class HitboxHistory : NetworkBehaviour
{
    private struct PositionSnapshot
    {
        public float time;
        public Vector3 position;
        public Quaternion rotation;
    }

    // Guardamos los últimos 500ms de posiciones
    private List<PositionSnapshot> _snapshots = new List<PositionSnapshot>();
    private const float MaxHistoryTime = 0.5f;

    void FixedUpdate()
    {
        // 1. Verificación de seguridad: Si no hay red o no somos el servidor, no hacer nada
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        // 2. Verificación adicional: Asegurarse de que el servidor esté realmente escuchando
        if (!NetworkManager.Singleton.IsListening)
            return;

        float serverTime = NetworkManager.Singleton.ServerTime.TimeAsFloat;

        // El resto de tu lógica de Snapshots...
        if (_snapshots.Count == 0 ||
            Vector3.Distance(_snapshots[0].position, transform.position) > 0.01f ||
            Quaternion.Angle(_snapshots[0].rotation, transform.rotation) > 0.1f)
        {
            _snapshots.Insert(0, new PositionSnapshot
            {
                time = serverTime,
                position = transform.position,
                rotation = transform.rotation
            });
        }

        while (_snapshots.Count > 2 && _snapshots[_snapshots.Count - 1].time < serverTime - MaxHistoryTime)
        {
            _snapshots.RemoveAt(_snapshots.Count - 1);
        }
    }

    // Esta función permite al servidor mover el objeto al tiempo exacto que el cliente vio
    public void RewindTo(float targetTime)
    {
        if (_snapshots.Count < 2) return;

        // Buscamos los dos puntos en el tiempo entre los que se encuentra targetTime
        for (int i = 0; i < _snapshots.Count - 1; i++)
        {
            if (targetTime <= _snapshots[i].time && targetTime >= _snapshots[i + 1].time)
            {
                float t = (targetTime - _snapshots[i + 1].time) / (_snapshots[i].time - _snapshots[i + 1].time);
                transform.position = Vector3.Lerp(_snapshots[i + 1].position, _snapshots[i].position, t);
                transform.rotation = Quaternion.Slerp(_snapshots[i + 1].rotation, _snapshots[i].rotation, t);
                return;
            }
        }
    }
}