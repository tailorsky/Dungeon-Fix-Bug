using UnityEngine;

public enum WeaponType
{
    None,
    Dagger,  // быстрый, мало урона
    Sword,   // баланс
    Axe,     // медленный, много урона
    Staff    // магический
}

[System.Serializable]
public class Weapon
{
    public string    weaponName;
    public WeaponType type;
    public int       damageMin;
    public int       damageMax;
    public float     attackSpeed;   // weapon_speed: секунд между ударами
    public string    description;
}

public class WeaponSystem : MonoBehaviour
{
    [Header("Стартовое оружие (настраивается в Inspector)")]
    [SerializeField] private Weapon defaultWeapon = new Weapon
    {
        weaponName  = "Кулаки",
        type        = WeaponType.None,
        damageMin   = 3,
        damageMax   = 6,
        attackSpeed = 1.5f,
        description = "Ничего нет — придётся драться руками"
    };

    private Weapon currentWeapon;

    [Header("Связи")]
    [SerializeField] private UIManager uiManager;

    private void Start()
    {
        // Копируем чтобы не мутировать SerializeField-объект
        currentWeapon = defaultWeapon;
    }

    public void EquipWeapon(Weapon newWeapon)
    {
        currentWeapon = newWeapon;
        Debug.Log($"Экипировано: {newWeapon.weaponName} " +
                  $"({newWeapon.damageMin}-{newWeapon.damageMax} урона, " +
                  $"скорость {newWeapon.attackSpeed}с)");

        uiManager?.ShowMessage($"Подобрано: {newWeapon.weaponName}!");
    }

    // Геттеры для CombatSystem
    public int   GetDamageMin()   => currentWeapon?.damageMin   ?? defaultWeapon.damageMin;
    public int   GetDamageMax()   => currentWeapon?.damageMax   ?? defaultWeapon.damageMax;
    public float GetAttackSpeed() => currentWeapon?.attackSpeed ?? defaultWeapon.attackSpeed;

    public Weapon GetCurrentWeapon() => currentWeapon;
}