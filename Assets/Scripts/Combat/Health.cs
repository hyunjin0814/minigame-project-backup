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
        CurrentHp = Mathf.Max(0, CurrentHp - amount);
        if (CurrentHp == 0)
            OnDeath?.Invoke();
    }
}
