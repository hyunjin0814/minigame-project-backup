using System.Collections;
using UnityEngine;

public class BossDeathState : BossStateBase
{
    private readonly float destroyDelay;

    public BossDeathState(BossBase boss, float destroyDelay = 1.5f) : base(boss)
    {
        this.destroyDelay = destroyDelay;
    }

    public override void Enter()
    {
        Debug.Log("[Boss] DeathState Enter");
        Boss.Rb.linearVelocity = Vector2.zero;
        Boss.Rb.bodyType = RigidbodyType2D.Static;
        Boss.DropItem();
        Boss.StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        // 사망 연출: 색 페이드
        float elapsed = 0f;
        Color original = Boss.Sprite.color;
        while (elapsed < destroyDelay)
        {
            elapsed += Time.deltaTime;
            Boss.Sprite.color = Color.Lerp(original, Color.clear, elapsed / destroyDelay);
            yield return null;
        }
        Object.Destroy(Boss.gameObject);
    }
}
