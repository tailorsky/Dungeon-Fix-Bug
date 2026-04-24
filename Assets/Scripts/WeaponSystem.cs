using UnityEngine;

public enum WeaponType
{
    None,
    Dagger,
    Sword,
    Staff
}

[System.Serializable]
public class Weapon
{
    public string    weaponName;
    public WeaponType type;
    public int       damageMin;
    public int       damageMax;
    public float     weaponTime; // секунд между ударами
    public string    description;
}

public class WeaponSystem : MonoBehaviour
{
    [Header("Стартовое оружие")]
    [SerializeField] private Weapon defaultWeapon = new Weapon
    {
        weaponName = "Кулаки",
        type       = WeaponType.None,
        damageMin  = 3,
        damageMax  = 6,
        weaponTime = 1.5f,
        description = "Ничего нет — придётся драться руками"
    };

    private Weapon currentWeapon;

    [Header("Связи")]
    [SerializeField] private UIManager uiManager;

    private void Start()
    {
        currentWeapon = defaultWeapon;
    }

    public void EquipWeapon(Weapon newWeapon)
    {
        currentWeapon = newWeapon;
        Debug.Log($"Экипировано: {newWeapon.weaponName} " +
                  $"({newWeapon.damageMin}-{newWeapon.damageMax} урона, " +
                  $"время {newWeapon.weaponTime}с)");
        uiManager?.ShowMessage($"Подобрано: {newWeapon.weaponName}!");
    }

    public int   GetDamageMin()  => currentWeapon?.damageMin  ?? defaultWeapon.damageMin;
    public int   GetDamageMax()  => currentWeapon?.damageMax  ?? defaultWeapon.damageMax;
    public float GetWeaponTime() => currentWeapon?.weaponTime ?? defaultWeapon.weaponTime;

    public Weapon GetCurrentWeapon() => currentWeapon;
}