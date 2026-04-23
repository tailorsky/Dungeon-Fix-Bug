using UnityEngine;

// =====================================================
// СКРИПТ: KeyPickup.cs
// ОПИСАНИЕ: Вешается на объект ключа в сцене.
// При подборе — передаёт себя в PlayerInteract.
//
// КАК ПОДКЛЮЧИТЬ:
// 1. Повесь скрипт на объект "Key" в сцене
// 2. Тег объекта ОБЯЗАТЕЛЬНО должен быть "Key" (с заглавной K!)
// 3. На объекте должен быть Collider с Is Trigger = true
// 4. (Опционально) добавь лёгкое вращение через анимацию
//
// ВАЖНО ДЛЯ ГЕЙМДИЗАЙНЕРОВ:
// Ключ должен быть размещён в сцене вручную!
// Проверь что он не провалился под пол (Y позиция = уровень пола + 0.5)
// =====================================================

public class KeyPickup : MonoBehaviour
{
    [Header("Визуальные настройки")]
    [SerializeField] private float rotationSpeed = 90f;     // Скорость вращения ключа
    [SerializeField] private float bobHeight = 0.2f;        // Высота покачивания
    [SerializeField] private float bobSpeed = 2f;           // Скорость покачивания

    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        // Вращаем ключ вокруг своей оси (Y)
        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);

        // Покачивание вверх-вниз (синус)
        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(
            transform.position.x,
            newY,
            transform.position.z
        );
    }

    // Срабатывает когда игрок касается ключа
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Передаём подбор в PlayerInteract
            PlayerInteract interact = other.GetComponent<PlayerInteract>();
            if (interact != null)
            {
                // KeyPickup сообщает о себе — PlayerInteract сам подберёт через Raycast
                // Это резервный способ подбора если Raycast не сработал
                Debug.Log("Ключ подобран автоматически при касании!");
            }

            // Уничтожаем объект ключа
            Destroy(gameObject);
        }
    }
}
