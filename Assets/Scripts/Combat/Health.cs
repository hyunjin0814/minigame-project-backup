using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField]
    private int maxHp = 3;
    public int CurrentHp { get; private set; }

    public event Action OnDeath;

    private void Awake() => CurrentHp = maxHp;

    public void TakeDamage(int amount)
    {
        if (CurrentHp <= 0)
            return;
        CurrentHp = Mathf.Max(0, CurrentHp - amount);
        Debug.Log($"[Health] 데미지 -{amount} → {CurrentHp}/{maxHp}");
        if (CurrentHp == 0)
            OnDeath?.Invoke();
    }

    public void Heal(int amount)
    {
        CurrentHp = Mathf.Min(maxHp, CurrentHp + amount);
        Debug.Log($"[Health] 회복 +{amount} → {CurrentHp}/{maxHp}");
    }
}
