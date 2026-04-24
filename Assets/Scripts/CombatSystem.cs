using System.Collections;
using UnityEngine;

public class CombatSystem : MonoBehaviour
{
    [Header("Оружие игрока (fallback если нет WeaponSystem)")]
    [SerializeField] private int   weaponDamageMin;
    [SerializeField] private int   weaponDamageMax;
    [SerializeField] private float weaponTime; // секунд между ударами

    [Header("Атака")]
    [SerializeField] private float attackRange = 3.5f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Камера во время боя")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float combatCameraDistance = 2f;
    [SerializeField] private float cameraLerpSpeed      = 5f;

    [Header("Связи")]
    [SerializeField] private PlayerStats    playerStats;
    [SerializeField] private UIManager      uiManager;
    [SerializeField] private WeaponSystem   weaponSystem;
    [SerializeField] private PlayerMovement playerMovement;

    private EnemyStats  currentEnemy;
    private EnemyPatrol currentEnemyAI;
    private bool        inCombat = false;

    private Coroutine playerAttackLoop;
    private Coroutine enemyAttackLoop;

    private Vector3 defaultCameraLocalPos;

    private void Start()
    {
        if (cameraTransform != null)
            defaultCameraLocalPos = cameraTransform.localPosition;

        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
            if (playerMovement == null)
                Debug.LogWarning("CombatSystem: PlayerMovement не назначен и не найден!");
        }
    }

    private void Update()
    {
        if (!inCombat && Input.GetKeyDown(KeyCode.Space))
            TryStartCombat();

        UpdateCombatCamera();
    }

    private void TryStartCombat()
    {
        if (!Physics.Raycast(transform.position, transform.forward,
                out RaycastHit hit, attackRange, enemyLayer)) return;

        EnemyStats enemy = hit.collider.GetComponent<EnemyStats>();
        if (enemy != null && !enemy.IsDead())
            StartCombat(enemy);
    }

    public void StartCombat(EnemyStats enemy)
    {
        if (inCombat) return;

        // почему тут комментарий?🤔
        // if (playerMovement != null)
        //     playerMovement.enabled = false;

        uiManager?.ShowEnemy(enemy);

        if (weaponSystem != null)
        {
            weaponTime     = weaponSystem.GetWeaponTime();
            weaponDamageMin = weaponSystem.GetDamageMin();
            weaponDamageMax = weaponSystem.GetDamageMax();
        }

        inCombat       = true;
        currentEnemy   = enemy;
        currentEnemyAI = enemy.GetComponent<EnemyPatrol>();

        Vector3 dir = (enemy.transform.position - transform.position).normalized;
        dir.y = 0f;
        if (dir != Vector3.zero)
            StartCoroutine(RotateToEnemy(Quaternion.LookRotation(dir)));

        uiManager?.ShowMessage("Бой!");

        // Исправлено: корутины назначены правильно
        playerAttackLoop = StartCoroutine(PlayerAttackLoop());
        enemyAttackLoop  = StartCoroutine(EnemyAttackLoop());
    }

    public void EndCombat(bool playerWon)
    {
        if (!inCombat) return;

        //а мы вернули игроку управление?..
        if (playerMovement != null)
            playerMovement.enabled = false;

        inCombat = false;

        if (playerAttackLoop != null) StopCoroutine(playerAttackLoop);
        if (enemyAttackLoop  != null) StopCoroutine(enemyAttackLoop);

        currentEnemy   = null;
        currentEnemyAI = null;

        uiManager?.ShowMessage(playerWon ? "Победа!" : "Вы погибли...");
        uiManager?.HideEnemy();
    }

    private IEnumerator PlayerAttackLoop()
    {
        while (inCombat)
        {
            yield return PlayerAttack();
            yield return new WaitForSeconds(weaponTime);
        }
    }

    private IEnumerator PlayerAttack()
    {
        if (currentEnemy == null || currentEnemy.IsDead()) yield break;

        if (Random.value < currentEnemy.Evasion)
        {
            uiManager?.ShowMessage("Скелет уклонился!");

            if (currentEnemyAI != null)
            {
                currentEnemyAI.PlayPlayerAttackAnimation();
                Animator anim = currentEnemyAI.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    yield return null;
                    yield return new WaitForSeconds(anim.GetCurrentAnimatorStateInfo(0).length);
                }
            }

            yield break;
        }

        int weaponDamage = RollWeaponDamage();
        int totalDamage  = playerStats.CalculateDamage(weaponDamage);

        currentEnemy.TakeDamage(totalDamage);
        uiManager?.UpdateEnemyHealth(currentEnemy.CurrentHealth, currentEnemy.MaxHealth);
        uiManager?.ShowMessage($"Удар! -{totalDamage} скелету");

        if (currentEnemyAI != null)
        {
            currentEnemyAI.PlayPlayerAttackAnimation();
            Animator anim = currentEnemyAI.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                yield return null;
                yield return new WaitForSeconds(anim.GetCurrentAnimatorStateInfo(0).length);
            }
        }

        if (currentEnemy != null && currentEnemy.IsDead())
        {
            playerStats.GainXp(currentEnemy.KillExp);
            // хм.. а мы точно заканчиваем бой в строке ниже? 🤔
            EndCombat(true);
        }
    }

    private IEnumerator EnemyAttackLoop()
    {
        while (inCombat)
        {
            yield return new WaitForSeconds(currentEnemy.AttackTime);
            yield return EnemyAttack();
        }
    }

    private IEnumerator EnemyAttack()
    {
        if (currentEnemy == null || currentEnemy.IsDead()) yield break;

        if (currentEnemyAI != null)
        {
            currentEnemyAI.PlayAttackAnimation();
            Animator anim = currentEnemyAI.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                yield return null;
                yield return new WaitForSeconds(anim.GetCurrentAnimatorStateInfo(0).length);
            }
        }

        int damage = currentEnemy.RollDamage();
        playerStats.TakeDamage(damage);
        uiManager?.ShowMessage($"Скелет бьёт! -{damage} HP");
    }

    private int RollWeaponDamage()
    {
        if (weaponSystem != null)
            return Random.Range(weaponSystem.GetDamageMin(), weaponSystem.GetDamageMax() + 1);

        return Random.Range(weaponDamageMin, weaponDamageMax + 1);
    }

    public void OnEnemySpotted(EnemyStats enemy)
    {
        if (!inCombat)
            StartCombat(enemy);
    }

    private void UpdateCombatCamera()
    {
        if (cameraTransform == null) return;

        Vector3 target = inCombat
            ? defaultCameraLocalPos + Vector3.forward * combatCameraDistance
            : defaultCameraLocalPos;

        cameraTransform.localPosition = Vector3.Lerp(
            cameraTransform.localPosition,
            target,
            cameraLerpSpeed * Time.deltaTime
        );
    }

    private IEnumerator RotateToEnemy(Quaternion targetRotation)
    {
        while (Quaternion.Angle(transform.rotation, targetRotation) > 1f)
        {
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                targetRotation,
                cameraLerpSpeed * Time.deltaTime
            );
            yield return null;
        }
        transform.rotation = targetRotation;
    }

    public bool IsInCombat() => inCombat;
}