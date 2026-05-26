using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MaxHpReward : MonoBehaviour
{
    [SerializeField] private int hpIncrease = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var health = other.GetComponent<Health>();
        if (health == null) return;

        health.IncreaseMaxHp(hpIncrease);
        Debug.Log($"[MaxHpReward] maxHealth +{hpIncrease} → {health.MaxHp}");
        gameObject.SetActive(false);
    }
}
