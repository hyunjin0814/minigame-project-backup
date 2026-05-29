using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class PlayerHurtEffect : MonoBehaviour
{
    [Header("Iframe / Flicker")]
    [SerializeField] private float iframeDuration   = 1f;
    [SerializeField] private float flickerInterval  = 0.1f;

    [Header("Knockback")]
    [SerializeField] private float knockbackForce   = 8f;
    [SerializeField] private float knockbackUpForce = 3f;
    [SerializeField] private float knockbackDuration = 0.15f;

    /// <summary>넉백 지속 중 true — PlayerHorizontalMovement가 이동 입력을 차단하는 데 사용.</summary>
    public bool IsHurt { get; private set; }

    private SpriteRenderer spriteRenderer;
    private Health health;
    private PlayerMotor motor;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        health         = GetComponent<Health>();
        motor          = GetComponent<PlayerMotor>();
    }

    private void OnEnable()  => health.OnHit += HandleHit;
    private void OnDisable() => health.OnHit -= HandleHit;

    private void HandleHit(int amount, Vector2 source)
    {
        StopAllCoroutines();
        float stopDuration = HitStopManager.Instance?.Freeze(HitStopType.Light) ?? 0f;
        StartCoroutine(HurtRoutine(source, stopDuration));
    }

    private IEnumerator HurtRoutine(Vector2 source, float stopDuration)
    {
        // ① 히트스톱 대기 (timeScale=0 구간, Realtime으로 대기)
        if (stopDuration > 0f)
            yield return new WaitForSecondsRealtime(stopDuration);

        // ② 넉백 — 피격 방향 반대로 밀어냄
        IsHurt = true;
        float dirX = transform.position.x >= source.x ? 1f : -1f;
        motor?.SetVelocityX(dirX * knockbackForce);
        motor?.SetVelocityY(knockbackUpForce);

        // ③ 무적 시작
        health.IsInvincible = true;

        // ④ 넉백 유지 시간 (이동 입력 차단)
        yield return new WaitForSecondsRealtime(knockbackDuration);
        IsHurt = false;

        // ⑤ 남은 무적 시간 동안 깜빡임
        float elapsed = knockbackDuration;
        while (elapsed < iframeDuration)
        {
            if (spriteRenderer != null)
                spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSecondsRealtime(flickerInterval);
            elapsed += flickerInterval;
        }

        if (spriteRenderer != null)
            spriteRenderer.enabled = true;
        health.IsInvincible = false;
    }
}
