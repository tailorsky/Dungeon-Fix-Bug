using System.Collections;
using UnityEngine;

// =====================================================
// СКРИПТ: DoorController.cs
// ОПИСАНИЕ: Дверь которая открывается при наличии ключа.
// При открытии — плавно уходит вниз под пол.
//
// КАК ПОДКЛЮЧИТЬ:
// 1. Повесь скрипт на объект "Door" в сцене
// 2. Убедись что у объекта есть Collider (не триггер!) для блокировки
// 3. Тег объекта должен быть "Door" (проверь в Inspector вверху)
// 4. На объекте с выходом поставь тег "Exit"
// =====================================================

public class DoorController : MonoBehaviour
{
    [Header("Настройки анимации двери")]
    [SerializeField] private float openSpeed = 2f;          // Скорость опускания двери
    [SerializeField] private float openDepth = 3f;          // На сколько единиц опускается

    [Header("Связи")]
    [SerializeField] private GameManager gameManager;       // Для вызова победы

    private bool isOpen = false;
    private Vector3 closedPosition;
    private Vector3 openPosition;

    private void Start()
    {
        closedPosition = transform.position;
        openPosition = transform.position - new Vector3(0, openDepth, 0);
    }

    // Вызывается из PlayerInteract когда игрок нажимает E с ключом
    public void Open()
    {
        if (isOpen) return;

        isOpen = true;
        Debug.Log("Дверь открывается!");
        StartCoroutine(OpenAnimation());
    }

    private IEnumerator OpenAnimation()
    {
        // Убираем коллайдер чтобы можно было пройти
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Плавно опускаем дверь
        while (Vector3.Distance(transform.position, openPosition) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                openPosition,
                openSpeed * Time.deltaTime
            );
            yield return null;
        }

        transform.position = openPosition;
        Debug.Log("Дверь открыта! Путь свободен.");
    }

    // Вызывается когда игрок входит в зону выхода
    private void OnTriggerEnter(Collider other)
    {
        // ❌ БАГ #6: Проверка тега неправильная — "Exit" написан с маленькой буквы.
        // Победа никогда не засчитывается.
        // Теги чувствительны к регистру! Найди ошибку.

        if (other.CompareTag("Player") && gameObject.CompareTag("exit"))  // ← ошибка здесь
        {
            if (gameManager != null)
                gameManager.OnPlayerWin();
        }
    }
}
