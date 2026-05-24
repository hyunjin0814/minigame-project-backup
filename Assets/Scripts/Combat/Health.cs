using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField]
    private int maxHp = 3;
    public int CurrentHp { get; private set; }
    public int MaxHp => maxHp;

    public bool IsInvincible { get; set; }

    // EnemyBase가 디버프·백스탭 배율 삽입에 사용. null이면 원본 데미지 그대로.
    public Func<int, Vector2, int> DamageModifier { get; set; }

    public event Action<int, Vector2> OnHit;
    public event Action OnDeath;
    public event Action<int> OnHeal;

    private void Awake() => CurrentHp = maxHp;

    public void TakeDamage(int amount, Vector2 source = default)
    {
        if (CurrentHp <= 0 || IsInvincible)
            return;
        int finalAmount = DamageModifier != null ? DamageModifier(amount, source) : amount;
        CurrentHp = Mathf.Max(0, CurrentHp - finalAmount);
        Debug.Log($"[Health] 데미지 -{finalAmount} → {CurrentHp}/{maxHp}");
        OnHit?.Invoke(finalAmount, source);
        if (CurrentHp == 0)
            OnDeath?.Invoke();
    }

    public void Heal(int amount)
    {
        CurrentHp = Mathf.Min(maxHp, CurrentHp + amount);
        Debug.Log($"[Health] 회복 +{amount} → {CurrentHp}/{maxHp}");
        OnHeal?.Invoke(amount);
    }
}
