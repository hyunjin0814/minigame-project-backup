using System.Collections;
using UnityEngine;

public class BossDeathState : BossStateBase
{
    private readonly float postAnimFade;
    private readonly bool triggerClear;

    // postAnimFade: Death 애니메이션 끝난 뒤 투명 페이드 시간 (0이면 즉시 삭제)
    public BossDeathState(BossBase boss, float postAnimFade = 0.4f, bool triggerClear = false) : base(boss)
    {
        this.postAnimFade = postAnimFade;
        this.triggerClear = triggerClear;
    }

    public override void Enter()
    {
        Debug.Log("[Boss] DeathState Enter");
        Boss.Rb.linearVelocity = Vector2.zero;
        Boss.Rb.bodyType = RigidbodyType2D.Static;
        if (!triggerClear)
            Boss.DropItem();
        Boss.StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        // Death 트리거는 BossAnimator.HandleDied()가 이미 날렸음.
        // 한 프레임 대기 후 Animator가 Death 상태로 전환됐는지 확인.
        yield return null;

        var anim = Boss.BossAnim;
        if (anim != null)
        {
            // Death 상태로 진입할 때까지 대기 (최대 0.3초 타임아웃 — 전환이 즉시 일어나지 않으면 애니 없는 것으로 간주)
            float waitTimeout = 0.3f;
            float waited = 0f;
            while (waited < waitTimeout)
            {
                if (IsInDeathState(anim)) break;
                waited += Time.deltaTime;
                yield return null;
            }

            // Death 상태라면 클립이 끝날 때까지 대기
            if (IsInDeathState(anim))
            {
                while (true)
                {
                    var info = anim.GetCurrentAnimatorStateInfo(0);
                    // 루프 클립은 대기하지 않음
                    if (!info.loop && info.normalizedTime >= 1f) break;
                    // Death 상태를 벗어났으면 중단
                    if (!IsInDeathState(anim)) break;
                    yield return null;
                }
            }
        }

        // 애니메이션 후 짧은 페이드 아웃
        if (postAnimFade > 0f)
        {
            float elapsed = 0f;
            Color original = Boss.Sprite.color;
            while (elapsed < postAnimFade)
            {
                elapsed += Time.deltaTime;
                Boss.Sprite.color = Color.Lerp(original, Color.clear, elapsed / postAnimFade);
                yield return null;
            }
        }

        if (triggerClear)
            StageClearManager.Instance?.TriggerClear();

        Object.Destroy(Boss.gameObject);
    }

    // "Death" 또는 "Base Layer.Death" 등 레이어 경로 상관없이 상태명으로 체크
    private static bool IsInDeathState(Animator anim)
    {
        var info = anim.GetCurrentAnimatorStateInfo(0);
        return info.IsName("Death") || info.IsName("Base Layer.Death");
    }
}
