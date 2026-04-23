using UnityEngine;

public enum LootType
{
    Weapon,
    Heal
}

[System.Serializable]
public class LootEntry
{
    public LootType type;

    // Оружие (если type = Weapon)
    public Weapon weapon;

    // Хилка (если type = Heal)
    public int healAmount;

    [Range(0f, 100f)]
    public float dropChance;
}

public class ChestLoot : MonoBehaviour
{
    [Header("Таблица лута")]
    [SerializeField] private LootEntry[] lootTable;

    [Header("Настройки сундука")]
    // БАГ #13: сундук помечен как уже открытый с самого начала —
    // игрок никогда не сможет его открыть, сразу получит "Сундук уже открыт!"
    // Подсказка: каким должен быть сундук в начале игры — открытым или закрытым?
    [SerializeField] private bool isOpened = true;
    [SerializeField] private GameObject openedChestModel;
    [SerializeField] private GameObject closedChestModel;

    // Открытие сундука (вызывается игроком)
    public void Open(WeaponSystem weaponSystem, PlayerStats playerStats)
    {
        if (isOpened)
        {
            Debug.Log("Сундук уже открыт!");
            return;
        }
        //булевая переменная, которая даёт понять, открыт сундук или нет, все ли модели на месте, если и на месте, то как они работают?
        isOpened = true;

        if (closedChestModel != null) closedChestModel.SetActive(true);
        if (openedChestModel != null) openedChestModel.SetActive(false);

        LootEntry loot = RollLoot();

        if (loot == null)
        {
            Debug.Log("Сундук пуст...");
            return;
        }
        //тут мы выбираем тип лута, как думаешь всё на месте?
        switch (loot.type)
        {
            case LootType.Weapon:
                HandleHealLoot(loot, playerStats);
                break;

            case LootType.Heal:
                HandleWeaponLoot(loot, weaponSystem);
                break;
        }
    }

    private LootEntry RollLoot()
    {
        if (lootTable == null || lootTable.Length == 0)
        {
            Debug.LogWarning("LootTable пуст!");
            return null;
        }

        float roll = Random.Range(0f, 100f);
        float cumulative = 0f;

        foreach (var entry in lootTable)
        {
            cumulative += entry.dropChance;

            // Подсказка: мы хотим выдать лут когда бросок "попал" в диапазон — он должен быть
            // меньше накопленного значения или больше?
            if (roll >= cumulative)
                return entry;
        }

        return null;
    }

    private void HandleWeaponLoot(LootEntry loot, WeaponSystem weaponSystem)
    {
        Weapon w = loot.weapon;

        if (w == null)
        {
            Debug.LogWarning("В луте нет оружия!");
            return;
        }

        Debug.Log($"Выпало оружие: {FormatWeapon(w)}");

        if (weaponSystem != null)
            weaponSystem.EquipWeapon(CloneWeapon(w));
    }

    private void HandleHealLoot(LootEntry loot, PlayerStats playerStats)
    {
        if (playerStats == null)
        {
            Debug.LogWarning("PlayerStats не передан!");
            return;
        }

        Debug.Log($"Выпала хилка: +{loot.healAmount} HP");
        playerStats.Heal(loot.healAmount);
    }

    private string FormatWeapon(Weapon w)
    {
        return $"{w.weaponName} [{w.damageMin}-{w.damageMax} урона, {w.attackSpeed:0.00}s]";
    }

    private Weapon CloneWeapon(Weapon original)
    {
        if (original == null) return null;

        return new Weapon
        {
            weaponName  = original.weaponName,
            type        = original.type,
            damageMin   = original.damageMin,
            damageMax   = original.damageMax,
            attackSpeed = original.attackSpeed,
            description = original.description
        };
    }

    [ContextMenu("Показать таблицу лута")]
    private void PrintLootTable()
    {
        if (lootTable == null || lootTable.Length == 0)
        {
            Debug.Log("Таблица пустая!");
            return;
        }

        Debug.Log("=== ТАБЛИЦА ЛУТА ===");

        float total = 0f;

        foreach (var entry in lootTable)
        {
            total += entry.dropChance;

            if (entry.type == LootType.Weapon && entry.weapon != null)
            {
                Debug.Log(
                    $"[Weapon] {entry.weapon.weaponName} | " +
                    $"шанс: {entry.dropChance}% | " +
                    $"урон: {entry.weapon.damageMin}-{entry.weapon.damageMax}"
                );
            }
            else if (entry.type == LootType.Heal)
            {
                Debug.Log(
                    $"[Heal] +{entry.healAmount} HP | шанс: {entry.dropChance}%"
                );
            }
        }

        Debug.Log($"Сумма шансов: {total}%");
    }
}