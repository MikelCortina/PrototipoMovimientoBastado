using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance;
    public GameObject bulletPrefab;
    public int poolSize = 50;
    public bool canGrow = true; // Permite que el pool crezca si es necesario

    private List<GameObject> _pool = new List<GameObject>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        for (int i = 0; i < poolSize; i++)
        {
            AddBulletToPool();
        }
    }

    private GameObject AddBulletToPool()
    {
        GameObject obj = Instantiate(bulletPrefab);
        obj.SetActive(false);
        _pool.Add(obj);
        return obj;
    }

    public GameObject GetBullet()
    {
        for (int i = 0; i < _pool.Count; i++)
        {
            if (!_pool[i].activeInHierarchy)
            {
                return _pool[i];
            }
        }

        if (canGrow)
        {
            return AddBulletToPool();
        }

        return null;
    }
}