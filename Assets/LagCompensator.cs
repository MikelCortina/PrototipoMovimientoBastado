using System.Collections.Generic;
using UnityEngine;

public static class LagCompensator
{
    // Diccionario para guardar dónde estaban antes del rewind y poder devolverlos
    private static Dictionary<HitboxHistory, (Vector3, Quaternion)> _revertData = new Dictionary<HitboxHistory, (Vector3, Quaternion)>();

    public static void RewindAll(float strikeTime)
    {
        _revertData.Clear();
        HitboxHistory[] allTargets = GameObject.FindObjectsByType<HitboxHistory>(FindObjectsSortMode.None);

        foreach (var target in allTargets)
        {
            // Guardamos posición actual (Presente)
            _revertData.Add(target, (target.transform.position, target.transform.rotation));
            // Movemos al pasado
            target.RewindTo(strikeTime);
        }

        // Forzamos a la física de Unity a entender que los objetos se han movido
        Physics.SyncTransforms();
    }

    public static void RestoreAll()
    {
        foreach (var item in _revertData)
        {
            item.Key.transform.position = item.Value.Item1;
            item.Key.transform.rotation = item.Value.Item2;
        }
        Physics.SyncTransforms();
        _revertData.Clear();
    }
}