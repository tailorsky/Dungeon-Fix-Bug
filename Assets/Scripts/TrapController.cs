using UnityEngine;

// =====================================================
// СКРИПТ: TrapController.cs
// ОПИСАНИЕ: Ловушка на полу — наносит урон когда игрок наступает.
// Может быть постоянной (каждый шаг) или одноразовой.
//
// КАК ПОДКЛЮЧИТЬ:
// 1. Повесь скрипт на объект ловушки (например "SpikeTrap")
// 2. ВАЖНО: на объекте должен стоять Collider
//    и Is Trigger должно быть ВКЛЮЧЕНО (галочка стоит)
// 3. Тег объекта: можно любой, главное что скрипт висит
// =====================================================

public class TrapController : MonoBehaviour
{
    [Header("Настройки ловушки")]
    [SerializeField] private int damageAmount = 25;     // Урон при срабатывании
    [SerializeField] private bool isOneTime = false;    // Одноразовая ловушка?

    private bool hasTriggered = false;  // Уже срабатывала? (для одноразовых)

    // ❌ БАГ #7: использован OnCollisionEnter вместо OnTriggerEnter!
    // Ловушка никогда не срабатывает потому что Collider стоит как Trigger.
    // Нужно использовать правильный метод.
    // Подсказка: если Is Trigger = true, нужен OnTrigger___, а не OnCollision___

    private void OnCollisionEnter(Collision collision)  // ← неправильный метод!
    {
        // Проверяем что это игрок
        if (!collision.gameObject.CompareTag("Player")) return;

        // Для одноразовых ловушек — проверяем что ещё не срабатывали
        if (isOneTime && hasTriggered) return;

        hasTriggered = true;

        // Наносим урон
        PlayerStats player = collision.gameObject.GetComponent<PlayerStats>();
        if (player != null)
        {
            player.TakeDamage(damageAmount);
            Debug.Log($"Ловушка сработала! Урон: {damageAmount}");
        }

        // Если одноразовая — отключаем
        if (isOneTime)
        {
            gameObject.SetActive(false);
        }
    }
}
