using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance;
    public GameObject bulletPrefab;
    public int poolSize = 50;
    private List<GameObject> _pool = new List<GameObject>();

    void Awake() => Instance = this;

    void Start()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(bulletPrefab);
            obj.SetActive(false);
            _pool.Add(obj);
        }
    }

    public GameObject GetBullet()
    {
        for (int i = 0; i < _pool.Count; i++)
        {
            if (!_pool[i].activeInHierarchy)
                return _pool[i];
        }

        Debug.LogError("POOL DE BALAS AGOTADO");
        return null;
    }

}