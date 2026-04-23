using UnityEngine;

// =====================================================
// СКРИПТ: EnemyAttack.cs
// ОПИСАНИЕ: Враг атакует игрока при контакте.
//
// КАК ПОДКЛЮЧИТЬ:
// 1. Повесь на тот же объект "Enemy" что и EnemyPatrol
// 2. Убедись что на Enemy стоит Collider с Is Trigger = true
// 3. Задай значение урона в поле damageAmount (по умолчанию 20)
// =====================================================

public class EnemyAttack : MonoBehaviour
{
    [Header("Настройки атаки")]
    [SerializeField] private int damageAmount = 20;         // Урон за одну атаку
    [SerializeField] private float attackCooldown = 1.5f;   // Задержка между атаками (сек)

    private float lastAttackTime = 0f;

    // Вызывается из EnemyPatrol когда враг касается игрока
    public void AttackPlayer(PlayerStats player)
    {
        if (player == null) return;

        // Проверяем cooldown — не атакуем слишком часто
        if (Time.time - lastAttackTime < attackCooldown) return;

        lastAttackTime = Time.time;
        player.TakeDamage(damageAmount);
        Debug.Log($"Враг атаковал игрока на {damageAmount} урона!");
    }
}
