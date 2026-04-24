using UnityEngine;

public enum WeaponRarity
{
    Basic = 0,
    Normal = 1,
    Special = 2,
    Epic = 3
}

[System.Serializable]
public class WeaponData
{
    public string weaponName;
    public WeaponRarity rarity;

    [Header("Stats")]
    public float damage;
    public float fireRate;
    public float bulletSpeed;
    public float bulletGravity;

    [Header("Main Weapon Mode")]
    public bool isShotgun;
    public int pellets;
    public float spreadDegrees;
    public float damageMultiplier;

    [Header("Visual")]
    public Color rarityColor = Color.white;
}