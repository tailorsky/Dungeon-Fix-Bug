using System.Collections;
using UnityEngine;

// =====================================================
// СКРИПТ: PlayerMovement.cs
// ОПИСАНИЕ: Управляет движением игрока по клеткам (grid).
// Игрок двигается строго на 1 клетку за нажатие,
// поворачивается на 90 градусов влево/вправо.
// 
// КАК ПОДКЛЮЧИТЬ:
// 1. Повесь этот скрипт на объект "Player"
// 2. Убедись что на Player стоит CharacterController
// 3. Размер клетки задаётся в поле CellSize (по умолчанию 3)
// =====================================================

public class PlayerMovement : MonoBehaviour
{
    [Header("Настройки движения")]
    [SerializeField] private float cellSize = 3f;
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float rotateSpeed = 180f;

    [Header("Проверка стен")]
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float rayDistance = 1.5f;

    private bool isMoving = false;
    private CharacterController characterController;

    private void Start()
    {
        //тут мы начинаем свой скрипт, выравнивая игрока по клетке
        // SnapToGrip() как раз-таки выравнивает, как думаете, что тут нужно сделать? :D
        characterController = GetComponent<CharacterController>();

        // SnapToGrid();
    }

    private void Update()
    {
        if (isMoving) return;
        HandleInput();
    }

    private void HandleInput()
    {
        // тут игрок ходит (на w вперед(?))
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            TryMove(-transform.forward);
        }

        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            TryMove(-transform.forward);
        }

        // тут игрок поворачивает (если стоит -90f он поворачивает налево), подумайте, как сделать правильно?)
        // получается у вас инвертирован поворот
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            StartCoroutine(RotatePlayer(-90f));
        }

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            StartCoroutine(RotatePlayer(90f));
        }
    }

    private void TryMove(Vector3 direction)
    {
        if (Physics.Raycast(transform.position, direction, rayDistance, wallLayer))
        {
            Debug.Log("Путь заблокирован стеной!");
            return;
        }

        Vector3 targetPosition = transform.position + direction * cellSize;
        Collider[] hits = Physics.OverlapSphere(targetPosition, cellSize * 0.4f);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Enemy") || hit.CompareTag("Chest"))
            {
                Debug.Log("Путь заблокирован врагом или сундуком!");
                return;
            }
        }

        StartCoroutine(MoveToPosition(targetPosition));
    }

    private IEnumerator MoveToPosition(Vector3 targetPosition)
    {
        isMoving = true;

        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );
            yield return null;
        }

        transform.position = targetPosition;

        // хм.. 
        isMoving = false;
    }

    private IEnumerator RotatePlayer(float angle)
    {
        isMoving = true;

        float targetY = transform.eulerAngles.y + angle;
        Quaternion targetRotation = Quaternion.Euler(0f, targetY, 0f);

        while (Quaternion.Angle(transform.rotation, targetRotation) > 0.5f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotateSpeed * Time.deltaTime
            );
            yield return null;
        }

        transform.rotation = targetRotation;
        isMoving = false;
    }

    private void SnapToGrid()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Round(pos.x / cellSize) * cellSize;
        pos.z = Mathf.Round(pos.z / cellSize) * cellSize;
        transform.position = pos;
    }
}