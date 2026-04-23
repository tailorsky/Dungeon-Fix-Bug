using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Связи")]
    [SerializeField] private PlayerStats playerStats;

    [Header("Здоровье")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("Опыт и уровень")]
    [SerializeField] private Slider xpBar;
    [SerializeField] private TextMeshProUGUI xpText;
    [SerializeField] private TextMeshProUGUI levelText;

    [Header("Статы")]
    [SerializeField] private TextMeshProUGUI statsText;

    [Header("Инвентарь")]
    [SerializeField] private Image keyIcon;

    [Header("Сообщения")]
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private float messageDuration = 2f;

    [Header("Панель прокачки")]
    [SerializeField] private GameObject statUpPanel;
    [SerializeField] private TextMeshProUGUI pendingPointsText;
    
    [SerializeField] private Button strengthButton;
    [SerializeField] private Button enduranceButton;
    [SerializeField] private Button agilityButton;
    [SerializeField] private Button intelligenceButton;
    
    [Header("Тексты характеристик")]
    [SerializeField] private TextMeshProUGUI strengthValueText;
    [SerializeField] private TextMeshProUGUI enduranceValueText;
    [SerializeField] private TextMeshProUGUI agilityValueText;
    [SerializeField] private TextMeshProUGUI intelligenceValueText;

    [Header("Враг")]
    [SerializeField] private GameObject enemyPanel;
    [SerializeField] private Slider enemyHealthBar;
    [SerializeField] private TextMeshProUGUI enemyHealthText;

    private Coroutine messageCoroutine;
    private bool isFirstTime = true; // НОВОЕ: флаг первого показа

    // ─── Инициализация ────────────────────────────────────────────────────────
    private void Start()
    {
        if (playerStats == null)
        {
            Debug.LogError("UIManager: PlayerStats не назначен в Inspector!");
            return;
        }

        playerStats.OnHealthChanged       += UpdateHealthBar;
        playerStats.OnXpChanged           += UpdateXpBar;
        playerStats.OnLevelUp             += UpdateLevel;
        playerStats.OnStatPointReady      += ShowStatUpPanel;
        playerStats.OnAllStatsDistributed += HideStatUpPanel;

        strengthButton?.onClick    .AddListener(() => SpendPoint(StatType.Strength));
        enduranceButton?.onClick   .AddListener(() => SpendPoint(StatType.Endurance));
        agilityButton?.onClick     .AddListener(() => SpendPoint(StatType.Agility));
        intelligenceButton?.onClick.AddListener(() => SpendPoint(StatType.Intelligence));

        ShowKeyIcon(false);
        SetMessageActive(false);

        UpdateHealthBar(playerStats.CurrentHealth, playerStats.MaxHealth);
        UpdateXpBar(playerStats.CurrentXp, playerStats.XpToNextLevel);
        UpdateLevel(playerStats.CurrentLevel);
        UpdateStatsText();
        
        // НОВОЕ: Панель может быть показана сразу в Start через OnStatPointReady
    }

    private void OnDestroy()
    {
        if (playerStats == null) return;

        playerStats.OnHealthChanged       -= UpdateHealthBar;
        playerStats.OnXpChanged           -= UpdateXpBar;
        playerStats.OnLevelUp             -= UpdateLevel;
        playerStats.OnStatPointReady      -= ShowStatUpPanel;
        playerStats.OnAllStatsDistributed -= HideStatUpPanel;
    }

    // ─── HP ──────────────────────────────────────────────────────────────────
    private void UpdateHealthBar(int current, int max)
    {
        if (healthBar != null)
        {
            healthBar.maxValue = max;
            healthBar.value    = current;
        }

        if (healthText != null)
            healthText.text = $"{current}/{max}";
    }

    // ─── Опыт и уровень ──────────────────────────────────────────────────────
    private void UpdateXpBar(int current, int toNext)
    {
        if (xpBar != null)
        {
            xpBar.maxValue = toNext;
            xpBar.value    = current;
        }

        if (xpText != null)
            xpText.text = $"{current}/{toNext} XP";
    }

    private void UpdateLevel(int level)
    {
        if (levelText != null)
            levelText.text = $"Ур. {level}";
    }

    // ─── Статы ───────────────────────────────────────────────────────────────
    private void UpdateStatsText()
    {
        if (statsText == null || playerStats == null) return;

        statsText.text =
            $"СИЛ:{playerStats.Strength}  " +
            $"ВЫН:{playerStats.Endurance}  " +
            $"ЛОВ:{playerStats.Agility}  " +
            $"ИНТ:{playerStats.Intelligence}";
    }

    private void UpdateStatValuesInPanel()
    {
        if (playerStats == null) return;

        if (strengthValueText != null)
            strengthValueText.text = $"Уровень: {playerStats.Strength}";
        
        if (enduranceValueText != null)
            enduranceValueText.text = $"Уровень: {playerStats.Endurance}";
        
        if (agilityValueText != null)
            agilityValueText.text = $"Уровень: {playerStats.Agility}";
        
        if (intelligenceValueText != null)
            intelligenceValueText.text = $"Уровень: {playerStats.Intelligence}";
        
        if (pendingPointsText != null)
            pendingPointsText.text = $"Очков: {playerStats.PendingPoints}";
    }

    // ─── Панель прокачки ─────────────────────────────────────────────────────
    private void ShowStatUpPanel()
    {
        if (statUpPanel != null)
            statUpPanel.SetActive(true);

        UpdateStatValuesInPanel();
        
        // НОВОЕ: Меняем заголовок в зависимости от ситуации
        if (isFirstTime)
        {
            ShowMessage("Распредели стартовые очки характеристик!");
            isFirstTime = false;
        }
        else
        {
            ShowMessage("Уровень повышен! Распредели очко характеристики.");
        }
        
        Time.timeScale = 0f;
    }

    private void HideStatUpPanel()
    {
        if (statUpPanel != null)
            statUpPanel.SetActive(false);
        
        Time.timeScale = 1f;
    }

    private void SpendPoint(StatType stat)
    {
        if (playerStats == null) return;

        bool success = playerStats.SpendStatPoint(stat);

        if (success)
        {
            UpdateStatsText();
            UpdateStatValuesInPanel();
        }
    }

    // ─── Инвентарь ───────────────────────────────────────────────────────────
    public void ShowKeyIcon(bool show)
    {
        if (keyIcon != null)
            keyIcon.gameObject.SetActive(show);
    }

    // ─── Сообщения ───────────────────────────────────────────────────────────
    public void ShowMessage(string text)
    {
        if (messageText == null) return;

        if (messageCoroutine != null)
            StopCoroutine(messageCoroutine);

        messageCoroutine = StartCoroutine(ShowMessageCoroutine(text));
    }

    private System.Collections.IEnumerator ShowMessageCoroutine(string text)
    {
        messageText.text = text;
        SetMessageActive(true);

        yield return new WaitForSecondsRealtime(messageDuration);

        SetMessageActive(false);
    }

    private void SetMessageActive(bool active)
    {
        if (messageText != null)
            messageText.gameObject.SetActive(active);
    }

    // ─── Враг ────────────────────────────────────────────────────────────────
    public void ShowEnemy(EnemyStats enemy)
    {
        if (enemyPanel != null)
            enemyPanel.SetActive(true);

        UpdateEnemyHealth(enemy.CurrentHealth, enemy.MaxHealth);
    }

    public void HideEnemy()
    {
        if (enemyPanel != null)
            enemyPanel.SetActive(false);
    }

    public void UpdateEnemyHealth(int current, int max)
    {
        if (enemyHealthBar != null)
        {
            enemyHealthBar.maxValue = max;
            enemyHealthBar.value = current;
        }

        if (enemyHealthText != null)
            enemyHealthText.text = $"{current}/{max}";
    }
}