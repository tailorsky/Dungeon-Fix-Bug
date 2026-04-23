using System.Collections;
using UnityEngine;

// =====================================================
// СКРИПТ: CombatSystem.cs
// ОПИСАНИЕ: Реалтаймовая боёвка.
// Игрок бьёт каждые weapon_speed секунд,
// скелет бьёт каждые skeleton_speed секунд,
// оба могут уклоняться.
//
// КАК ПОДКЛЮЧИТЬ:
// 1. Повесь на объект "Player"
// 2. Назначь playerStats, uiManager, cameraTransform в Inspector
// 3. WeaponSystem — опционально
// =====================================================

public class CombatSystem : MonoBehaviour
{
    [Header("Оружие игрока (fallback если нет WeaponSystem)")]
    [SerializeField] private int weaponDamageMin;
    [SerializeField] private int weaponDamageMax;
    [SerializeField] private float weaponSpeed;

    [Header("Атака")]
    [SerializeField] private float attackRange = 3.5f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Камера во время боя")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float combatCameraDistance = 2f;
    [SerializeField] private float cameraLerpSpeed      = 5f;

    [Header("Связи")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private WeaponSystem weaponSystem;
    [SerializeField] private PlayerMovement playerMovement;

    private EnemyStats   currentEnemy;
    private EnemyPatrol  currentEnemyAI;
    private bool         inCombat = false;

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
        //тут мы начинаем свой бой, можем ли мы убегать или нет?
        if (inCombat) return;

        if (playerMovement != null)
            playerMovement.enabled = true;
        

        //а что это делает?
        uiManager?.ShowEnemy(enemy);

        if (weaponSystem != null)
        {
            weaponSpeed = weaponSystem.GetAttackSpeed();
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


        //хм всё ли тут на месте
        playerAttackLoop = StartCoroutine(EnemyAttackLoop());
        enemyAttackLoop  = StartCoroutine(PlayerAttackLoop());
    }

    public void EndCombat(bool playerWon)
    {
        //здесь мы заканчиваем бой
        if (!inCombat) return;

        // if (playerMovement != null) 
        //     playerMovement.enabled = true;

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
            yield return new WaitForSeconds(weaponSpeed);
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

            //тут мы заканчиваем бой, ведь так?
            EndCombat(false);
        }
    }

    private IEnumerator EnemyAttackLoop()
    {
        while (inCombat)
        {
            yield return new WaitForSeconds(currentEnemy.AttackSpeed);
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

        if (Random.value < playerStats.GetDodgeChance())
        {
            uiManager?.ShowMessage("Ты уклонился!");
            yield break;
        }

        int damage = currentEnemy.RollDamage();
        playerStats.TakeDamage(damage);
        uiManager?.ShowMessage($"Скелет бьёт! -{damage} HP");
    }

    private int RollWeaponDamage()
    {
        int min = weaponDamageMin;
        int max = weaponDamageMax;

        if (weaponSystem != null)
        {
            min = weaponSystem.GetDamageMin();
            max = weaponSystem.GetDamageMax();
        }

        return Random.Range(min, max + 1);
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