using System.Collections;
using UnityEngine;

// =====================================================
// СКРИПТ: EnemyStats.cs
// =====================================================

public class EnemyStats : MonoBehaviour
{
    [Header("Статы скелета")]
    [SerializeField] private int maxHealth        = 0;
    [SerializeField] private int damageMin        = 0;
    [SerializeField] private int damageMax        = 0;
    [SerializeField] private float attackSpeed    = 0.0f;  // skeleton_speed: секунд между ударами
    [SerializeField] private float evasion        = 0.0f;  // skeleton_evasion: шанс уклонения [0..1]
    [SerializeField] private int killExp          = 0;    // skeleton_kill_exp

    private int currentHealth;
    private bool isDead = false;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"Скелет получил {damage} урона! HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
            StartCoroutine(DieSequence());
    }

    private IEnumerator DieSequence()
    {
        isDead = true;
        Debug.Log("Скелет умирает...");

        EnemyPatrol enemyAI = GetComponent<EnemyPatrol>();
        if (enemyAI != null)
        {
            enemyAI.PlayDeath();
            Animator anim = enemyAI.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                yield return null;
                yield return new WaitForSeconds(anim.GetCurrentAnimatorStateInfo(0).length);
            }
        }

        Destroy(gameObject);
    }

    // Бросает урон по равномерному распределению [damageMin, damageMax]
    public int RollDamage()
    {
        return Random.Range(damageMin, damageMax + 1);
    }

    public bool IsDead()       => isDead;
    public float AttackSpeed   => attackSpeed;
    public float Evasion       => evasion;
    public int KillExp         => killExp;
    public int CurrentHealth   => currentHealth;
    public int MaxHealth       => maxHealth;
}