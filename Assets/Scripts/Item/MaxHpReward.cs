using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MaxHpReward : MonoBehaviour
{
    [SerializeField] private string rewardID;  // 월드에서 유일한 식별자 (재획득 방지용)
    [SerializeField] private int hpIncrease = 1;

    private void Start()
    {
        // 이미 획득한 보상이면 다시 나타나지 않도록 비활성화 (사망/재방문 시 재획득 방지)
        if (!string.IsNullOrEmpty(rewardID)
            && GameState.Instance != null
            && GameState.Instance.HasCollected(rewardID))
        {
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var health = other.GetComponent<Health>();
        if (health == null) return;

        health.IncreaseMaxHp(hpIncrease);
        Debug.Log($"[MaxHpReward] maxHealth +{hpIncrease} → {health.MaxHp}");

        // 1회성 처리: 수집 상태를 GameState에 영속화
        if (string.IsNullOrEmpty(rewardID))
            Debug.LogWarning($"[MaxHpReward] '{name}'에 rewardID가 없어 재획득 방지가 적용되지 않습니다.", this);
        else if (GameState.Instance != null)
            GameState.Instance.CollectItem(rewardID);

        gameObject.SetActive(false);
    }
}
