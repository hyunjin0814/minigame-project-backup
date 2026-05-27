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
    /// <summary>HP/MaxHp 값이 바뀔 때마다 발생. UI 갱신 전용 (연출 부작용 없음).</summary>
    public event Action OnChanged;

    private void Awake() => CurrentHp = maxHp;

    public void TakeDamage(int amount, Vector2 source = default)
    {
        if (CurrentHp <= 0 || IsInvincible)
            return;
        int finalAmount = DamageModifier != null ? DamageModifier(amount, source) : amount;
        CurrentHp = Mathf.Max(0, CurrentHp - finalAmount);
        Debug.Log($"[Health] 데미지 -{finalAmount} → {CurrentHp}/{maxHp}");
        OnHit?.Invoke(finalAmount, source);
        OnChanged?.Invoke();
        if (CurrentHp == 0)
            OnDeath?.Invoke();
    }

    public void Heal(int amount)
    {
        CurrentHp = Mathf.Min(maxHp, CurrentHp + amount);
        Debug.Log($"[Health] 회복 +{amount} → {CurrentHp}/{maxHp}");
        OnHeal?.Invoke(amount);
        OnChanged?.Invoke();
    }

    public void IncreaseMaxHp(int amount)
    {
        maxHp += amount;
        Debug.Log($"[Health] 최대 HP +{amount} → {maxHp}");
        Heal(amount);
    }

    /// <summary>
    /// 씬 전환 후 HP 복원 전용. 이벤트를 발생시키지 않고 직접 설정한다.
    /// PlayerSpawner에서만 사용할 것.
    /// </summary>
    public void ForceSetHp(int hp)
    {
        CurrentHp = Mathf.Clamp(hp, 0, maxHp);
        Debug.Log($"[Health] HP 강제 설정: {CurrentHp}/{maxHp}");
        OnChanged?.Invoke();
    }

    /// <summary>
    /// 씬 전환 후 최대 HP 복원 전용 (업그레이드 유지). 회복은 일으키지 않고 직접 설정한다.
    /// PlayerSpawner에서만 사용할 것.
    /// </summary>
    public void SetMaxHp(int value)
    {
        maxHp = Mathf.Max(1, value);
        CurrentHp = Mathf.Min(CurrentHp, maxHp);
        Debug.Log($"[Health] 최대 HP 복원: {maxHp}");
        OnChanged?.Invoke();
    }
}
