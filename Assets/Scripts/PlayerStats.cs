using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("База")]
    [SerializeField] private int baseHealth = 0;

    [Header("Модификаторы скейла")]
    [SerializeField] private float strengthMod     = 0.0f;
    [SerializeField] private float enduranceMod    = 0.0f;
    [SerializeField] private float agilityMod      = 0.0f;
    [SerializeField] private float intelligenceMod = 0.0f;

    [Header("Опыт и уровни")]
    [SerializeField] private int baseXpToNextLevel = 0;
    [SerializeField] private float xpScaling       = 0f;
    [SerializeField] private int lvlUpStatPoints       = 0;


    [Header("Стартовые очки")] // НОВОЕ
    [SerializeField] private int startingStatPoints = 0; // Сколько очков дать в начале

    [Header("Характеристики (стартовые значения)")]
    [SerializeField] private int strength     = 0;
    [SerializeField] private int endurance    = 0;
    [SerializeField] private int agility      = 0;
    [SerializeField] private int intelligence = 0;

    private int currentHealth;
    private int maxHealth;

    private int currentLevel = 1;
    private int currentXp    = 0;
    private int xpToNextLevel;

    private int pendingStatPoints = 0;
    
    private bool isWaitingForStatDistribution = false;
    private bool isFirstDistribution = true; // НОВОЕ: флаг первого распределения

    public event System.Action<int, int> OnHealthChanged;
    public event System.Action<int, int> OnXpChanged;
    public event System.Action<int>      OnLevelUp;
    public event System.Action           OnStatPointReady;
    public event System.Action           OnAllStatsDistributed;

    private void Start()
    {
        pendingStatPoints = startingStatPoints;
        isWaitingForStatDistribution = true;

        RecalculateStats();
        currentHealth = maxHealth;
        xpToNextLevel = baseXpToNextLevel;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnXpChanged?.Invoke(currentXp, xpToNextLevel);

        if (pendingStatPoints > 0)
        {
            OnStatPointReady?.Invoke();
        }
    }

    private void RecalculateStats()
    {
        int oldMax = maxHealth;
        maxHealth = baseHealth + Mathf.FloorToInt(endurance * enduranceMod);

        if (oldMax > 0)
            currentHealth = Mathf.RoundToInt((float)currentHealth / oldMax * maxHealth);
        else
            currentHealth = maxHealth; // НОВОЕ: если это первый раз, ставим полное HP

        currentHealth = Mathf.Clamp(currentHealth, 1, maxHealth);
    }

    public int CalculateDamage(int weaponDamage)
    {
        return weaponDamage + Mathf.FloorToInt(strength * strengthMod);
    }

    public int GetDodgeChance()
    {
        return Mathf.Min(Mathf.FloorToInt(agility * agilityMod), 75);
    }

    public int CalculateXpGain(int rawXp)
    {
        int bonus = Mathf.FloorToInt(intelligence * intelligenceMod);
        return rawXp * (1 + bonus);
    }

    public void TakeDamage(int incomingDamage)
    {
        if (isWaitingForStatDistribution)
            return;

        if (Random.Range(0, 100) < GetDodgeChance())
        {
            Debug.Log("Игрок уклонился!");
            return;
        }

        currentHealth -= incomingDamage;
        currentHealth  = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log($"Игрок получил {incomingDamage} урона! HP: {currentHealth}/{maxHealth}");

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        Debug.Log($"Игрок восстановил {amount} HP! HP: {currentHealth}/{maxHealth}");
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void GainXp(int rawXp)
    {
        if (isWaitingForStatDistribution)
            return;

        int gained = CalculateXpGain(rawXp);
        currentXp += gained;
        Debug.Log($"Получено {gained} XP (базовые: {rawXp}). Всего: {currentXp}/{xpToNextLevel}");

        while (currentXp >= xpToNextLevel)
        {
            currentXp    -= xpToNextLevel;
            xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * xpScaling);
            LevelUp();
        }

        OnXpChanged?.Invoke(currentXp, xpToNextLevel);
    }

    private void LevelUp()
    {
        currentLevel++;
        pendingStatPoints+= lvlUpStatPoints;
        
        isWaitingForStatDistribution = true;

        Debug.Log($"Уровень повышен! Уровень: {currentLevel}. Очков для распределения: {pendingStatPoints}");

        OnLevelUp?.Invoke(currentLevel);
        OnStatPointReady?.Invoke();
    }

    public bool SpendStatPoint(StatType stat)
    {
        if (pendingStatPoints <= 0)
        {
            Debug.LogWarning("Нет очков для распределения!");
            return false;
        }

        switch (stat)
        {
            case StatType.Strength:     strength++;     break;
            case StatType.Endurance:    endurance++;    break;
            case StatType.Agility:      agility++;      break;
            case StatType.Intelligence: intelligence++; break;
        }

        pendingStatPoints--;
        RecalculateStats();

        Debug.Log($"{stat} повышена! STR:{strength} END:{endurance} AGI:{agility} INT:{intelligence}");
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (pendingStatPoints > 0)
        {
            OnStatPointReady?.Invoke();
        }
        else
        {
            // Все очки распределены
            isWaitingForStatDistribution = false;
            
            if (isFirstDistribution)
            {
                currentHealth = maxHealth;
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
                isFirstDistribution = false;
            }
            
            OnAllStatsDistributed?.Invoke();
        }

        return true;
    }

    public int CurrentHealth  => currentHealth;
    public int MaxHealth      => maxHealth;
    public int CurrentLevel   => currentLevel;
    public int CurrentXp      => currentXp;
    public int XpToNextLevel  => xpToNextLevel;
    public int PendingPoints  => pendingStatPoints;
    public int Strength       => strength;
    public int Endurance      => endurance;
    public int Agility        => agility;
    public int Intelligence   => intelligence;
    
    public bool IsWaitingForStatDistribution => isWaitingForStatDistribution;

    private void Die()
    {
        Debug.Log("Игрок погиб!");
        GameManager gameManager = FindObjectOfType<GameManager>();
        gameManager?.OnPlayerDeath();
    }
}

public enum StatType
{
    Strength,
    Endurance,
    Agility,
    Intelligence
}