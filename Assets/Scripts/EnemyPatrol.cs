using System.Collections;
using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    [Header("Точки патруля")]
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;

    [Header("Настройки движения")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float waitTime = 1f;
    [SerializeField] private float cellSize = 3f;

    [Header("Обнаружение игрока")]
    [SerializeField] private float detectionRange = 4f;
    [SerializeField] private float rotateToPlayerSpeed = 5f;
    [SerializeField] private LayerMask obstacleLayer;

    private Transform currentTarget;
    private Transform playerTransform;
    private CombatSystem playerCombat;
    private EnemyStats enemyStats;
    [SerializeField] private Animator animator;

    private bool inCombat = false;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        transform.position = GetCellPosition(transform.position);

        // тут ставится начальная точка
        currentTarget = pointB;

        enemyStats = GetComponent<EnemyStats>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerCombat = player.GetComponent<CombatSystem>();
        }

        StartCoroutine(PatrolRoutine());
    }

    private IEnumerator PatrolRoutine()
    {
        while (true)
        {
            if (enemyStats != null && enemyStats.IsDead())
                yield break;

            if (!inCombat && CanSeePlayer())
            {
                yield return StartCoroutine(SpotPlayerRoutine());
                continue;
            }

            yield return StartCoroutine(MoveToPoint(currentTarget.position));

            yield return new WaitForSeconds(waitTime);
            //тут работает анимация, подумай, что тут не так?
            animator.SetBool("IsWalking", true);

            currentTarget = (currentTarget == pointA) ? pointB : pointA;
        }
    }

    private bool CanSeePlayer()
    {
        if (playerTransform == null) return false;
        //тут проверка клеток игрока и своих клеток
        Vector3 enemyCell = GetCellPosition(transform.position);
        Vector3 playerCell = GetCellPosition(playerTransform.position);

        Vector3[] directions = new Vector3[]
        {
            Vector3.forward,
            Vector3.back,
            Vector3.right,
            Vector3.left
        };

        foreach (Vector3 dir in directions)
        {
            Vector3 checkCell = enemyCell + dir * cellSize;

            if (Vector3.Distance(checkCell, playerCell) < 0.2f)
            {
                Vector3 origin = enemyCell + Vector3.up * 0.5f;
                Vector3 target = checkCell + Vector3.up * 0.5f;

                Vector3 rayDir = (target - origin).normalized;
                float distance = Vector3.Distance(origin, target);

                if (!Physics.Raycast(origin, rayDir, distance, obstacleLayer))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private IEnumerator SpotPlayerRoutine()
    {
        animator.SetBool("InCombat", true);
        inCombat = true;
        //тут враг поворачивает на игрока
        yield return StartCoroutine(RotateTowards(transform.position - playerTransform.position));

        if (playerCombat != null && enemyStats != null)
            playerCombat.OnEnemySpotted(enemyStats);

        while (playerCombat != null && playerCombat.IsInCombat())
        {
            Vector3 dir = (playerTransform.position - transform.position).normalized;
            dir.y = 0f;

            if (dir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Lerp(
                    transform.rotation,
                    targetRot,
                    rotateToPlayerSpeed * Time.deltaTime
                );
            }

            if (enemyStats.IsDead())
            {
                //inCombat не сбрасывается при смерти врага 
                // мёртвый враг навсегда остаётся в состоянии "в бою" и
                // не возобновляет патруль (хотя он уже мёртв это особенно заметно
                // если враг потом реснётся).
                // Подсказка: нужна одна строчка перед yield break.
                yield break;
            }

            yield return null;
        }

        inCombat = false;
    }

    private IEnumerator RotateTowards(Vector3 targetPos)
    {
        Vector3 dir = (targetPos - transform.position).normalized;
        dir.y = 0f;

        if (dir == Vector3.zero) yield break;

        Quaternion targetRotation = Quaternion.LookRotation(dir);

        while (Quaternion.Angle(transform.rotation, targetRotation) > 1f)
        {
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                targetRotation,
                rotateToPlayerSpeed * Time.deltaTime
            );
            yield return null;
        }

        //поворачивает к нужной позиции
        transform.rotation = targetRotation;
    }

    private IEnumerator MoveToPoint(Vector3 target)
    {
        target = GetCellPosition(target);
        animator.SetBool("IsWalking", true);
        Vector3 direction = (target - transform.position).normalized;
        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction);

        while (Vector3.Distance(transform.position, target) > 0.1f)
        {
            if (CanSeePlayer())
            {
                transform.position = GetCellPosition(transform.position);
                animator.SetBool("IsWalking", false);
                yield break;
            }

            transform.position = Vector3.MoveTowards(
                transform.position,
                target,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }

        transform.position = target;
        animator.SetBool("IsWalking", false);
    }

    private Vector3 GetCellPosition(Vector3 worldPos)
    {
        return new Vector3(
            Mathf.Round(worldPos.x / cellSize) * cellSize,
            worldPos.y,
            Mathf.Round(worldPos.z / cellSize) * cellSize
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            EnemyAttack attack = GetComponent<EnemyAttack>();
            if (attack != null)
                attack.AttackPlayer(other.GetComponent<PlayerStats>());
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.gray;
        int gridSize = 3;
        for (int x = -gridSize; x <= gridSize; x++)
        {
            for (int z = -gridSize; z <= gridSize; z++)
            {
                Vector3 cellPos = GetCellPosition(transform.position) +
                                new Vector3(x * cellSize, -1, z * cellSize);
                Gizmos.DrawWireCube(cellPos + Vector3.up * 0.05f, new Vector3(cellSize, 0.1f, cellSize));
            }
        }

        Gizmos.color = Color.red;
        Vector3 enemyCell = GetCellPosition(transform.position);
        Vector3[] directions = new Vector3[] { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };

        foreach (Vector3 dir in directions)
        {
            Vector3 checkCell = enemyCell + dir * cellSize;
            Gizmos.DrawCube(checkCell + Vector3.up * -1f, new Vector3(cellSize * 1f, -0.1f, cellSize * 1f));
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(enemyCell + Vector3.up * 0.1f, new Vector3(cellSize, -1f, cellSize));

        Gizmos.color = Color.yellow;
        foreach (Vector3 dir in directions)
        {
            Vector3 checkCell = enemyCell + dir * cellSize;
            Vector3 origin = enemyCell + Vector3.up * 0.5f;
            Vector3 target = checkCell + Vector3.up * 0.5f;
            Gizmos.DrawLine(origin, target);
        }
    }

    public void PlayAttackAnimation()
    {
        if (animator != null) animator.SetTrigger("Attack");
    }

    public void PlayPlayerAttackAnimation()
    {
        if (animator != null) animator.SetTrigger("PlayerAttack");
    }

    public void PlayDeath()
    {
        if (animator != null) animator.SetTrigger("Dead");
    }
}