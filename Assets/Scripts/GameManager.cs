using UnityEngine;
using UnityEngine.SceneManagement;

// =====================================================
// СКРИПТ: GameManager.cs
// ОПИСАНИЕ: Главный менеджер игры. Отвечает за:
// - Победу и поражение
// - Перезапуск уровня
// - Переход между сценами
//
// КАК ПОДКЛЮЧИТЬ:
// 1. Создай пустой объект "GameManager" в сцене
// 2. Повесь этот скрипт на него
// 3. Укажи имена сцен в полях ниже
//
// ВАЖНО: Сцены должны быть добавлены в Build Settings!
// File → Build Settings → перетащи сцены в список
// =====================================================

public class GameManager : MonoBehaviour
{
    [Header("Названия сцен")]
    [SerializeField] private string gameSceneName = "GameScene";    // Текущая сцена игры
    [SerializeField] private string menuSceneName = "MainMenu";     // Сцена главного меню
    [SerializeField] private string winSceneName  = "WinScreen";    // Экран победы

    [Header("UI панели (назначь в Inspector)")]
    [SerializeField] private GameObject gameOverPanel;   // Панель поражения
    [SerializeField] private GameObject winPanel;        // Панель победы

    private bool isGameActive = true;

    private void Start()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (winPanel != null)      winPanel.SetActive(false);

        Time.timeScale = 1f;
    }

    // Вызывается из PlayerStats когда HP = 0
    public void OnPlayerDeath()
    {
        if (!isGameActive) return;
        isGameActive = false;

        Debug.Log("GAME OVER");
        Time.timeScale = 0f;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    // Вызывается из DoorController когда игрок добрался до выхода
    public void OnPlayerWin()
    {
        if (!isGameActive) return;
        isGameActive = false;

        Debug.Log("ПОБЕДА!");
        Time.timeScale = 0f;

        if (winPanel != null)
            winPanel.SetActive(true);
    }

    // Кнопка "Играть снова" — перезапускает текущий уровень
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    // Кнопка "В главное меню"
    public void GoToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }

    // Кнопка "Следующий уровень" / экран победы
    public void GoToWinScreen()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(winSceneName);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && isGameActive)
        {
            // TODO: добавить меню паузы
            Debug.Log("Пауза не реализована");
        }
    }
}