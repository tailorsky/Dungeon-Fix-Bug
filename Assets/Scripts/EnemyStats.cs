using System.Collections;
using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    [Header("Статы скелета")]
    [SerializeField] private int   maxHealth  = 0;
    [SerializeField] private int   damageMin  = 0;
    [SerializeField] private int   damageMax  = 0;
    [SerializeField] private float attackTime = 0f; // секунд между ударами
    [SerializeField] private float evasion    = 0f; // шанс уклонения [0..1]
    [SerializeField] private int   killExp    = 0;

    private int  currentHealth;
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

    public int  RollDamage()    => Random.Range(damageMin, damageMax + 1);
    public bool IsDead()        => isDead;
    public float AttackTime     => attackTime;
    public float Evasion        => evasion;
    public int  KillExp         => killExp;
    public int  CurrentHealth   => currentHealth;
    public int  MaxHealth       => maxHealth;
}