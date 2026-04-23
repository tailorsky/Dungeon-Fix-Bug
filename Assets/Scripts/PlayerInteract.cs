using UnityEngine;

// =====================================================
// СКРИПТ: PlayerInteract.cs
// ОПИСАНИЕ: Игрок может взаимодействовать с объектами впереди:
// подбирать ключи, открывать двери, читать надписи.
//
// КАК ПОДКЛЮЧИТЬ:
// 1. Повесь скрипт на объект "Player"
// 2. Нажатие E — взаимодействие с объектом впереди
// 3. interactDistance — дальность (рекомендуется = cellSize игрока)
// =====================================================

public class PlayerInteract : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private float interactDistance = 3f;  // Дальность взаимодействия
    [SerializeField] private LayerMask interactLayer;      // Слой интерактивных объектов
    [SerializeField] private LayerMask wallLayer; // Слой со стеной

    // Хранит подобранные ключи
    private bool hasKey = false;

    [Header("Связи")]
    [SerializeField] private UIManager uiManager;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        Vector3[] directions = new Vector3[]
        {
            transform.forward,
            (transform.forward + Vector3.down).normalized,
            Vector3.down
        };

        RaycastHit hit;

        foreach (Vector3 dir in directions)
        {
            // Теперь два слоя: интерактивные объекты И стены
            LayerMask combinedMask = interactLayer | wallLayer;

            if (Physics.Raycast(transform.position, dir, out hit, interactDistance, combinedMask))
            {
                // Если попали в стену — луч заблокирован, идём дальше
                if (hit.collider.CompareTag("Wall"))
                {
                    Debug.Log("Луч заблокирован стеной");
                    continue;
                }

                GameObject hitObject = hit.collider.gameObject;

                if (hitObject.CompareTag("Key")) { PickUpKey(hitObject); return; }
                else if (hitObject.CompareTag("Door")) { TryOpenDoor(hitObject); return; }
                else if (hitObject.CompareTag("Chest")) { OpenChest(hitObject); return; }
            }
        }

        Debug.Log("Впереди нет объектов.");
    }

    private void PickUpKey(GameObject keyObject)
    {
        hasKey = true;
        Debug.Log("Ключ подобран!");

        // Обновляем иконку ключа в UI
        if (uiManager != null)
            uiManager.ShowKeyIcon(true);

        // Убираем ключ со сцены
        Destroy(keyObject);
    }

    private void TryOpenDoor(GameObject doorObject)
    {
        if (hasKey)
        {
            DoorController door = doorObject.GetComponent<DoorController>();
            if (door != null)
            {
                door.Open();
                hasKey = false;

                if (uiManager != null)
                    uiManager.ShowKeyIcon(false);
            }
        }
        else
        {
            Debug.Log("Нужен ключ чтобы открыть эту дверь!");
        }
    }

    private void OpenChest(GameObject chestObject)
    {
        ChestLoot chest = chestObject.GetComponent<ChestLoot>();
        WeaponSystem weaponSystem = GetComponent<WeaponSystem>();
        PlayerStats playerStats = GetComponent<PlayerStats>();

        if (chest != null)
            chest.Open(weaponSystem, playerStats);
    }

    // Другие скрипты могут проверить наличие ключа
    public bool HasKey() => hasKey;
}