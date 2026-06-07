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

    [Header("Hit Freeze Pose")]
    [SerializeField] private Sprite humanHurtSprite;
    [SerializeField] private Sprite catHurtSprite;
    [SerializeField] private Sprite dogHurtSprite;

    [Header("Effect")]
    [Tooltip("이펙트가 나올 몸통 중심 (로컬 오프셋). 기본 0이면 transform 위치.")]
    [SerializeField] private Vector2 _effectCenterOffset = Vector2.zero;
    [Tooltip("몸통 중심에서 맞은 방향으로 밀어낼 거리. 0이면 정중앙.")]
    [SerializeField] private float _effectEdgeDistance = 0.3f;

    /// <summary>넉백 지속 중 true — PlayerHorizontalMovement가 이동 입력을 차단하는 데 사용.</summary>
    public bool IsHurt { get; private set; }

    private SpriteRenderer spriteRenderer;
    private Health health;
    private PlayerMotor motor;
    private Animator animator;
    private PlayerTransformController _transformCtrl;

    private void Awake()
    {
        spriteRenderer  = GetComponentInChildren<SpriteRenderer>();
        health          = GetComponent<Health>();
        motor           = GetComponent<PlayerMotor>();
        animator        = GetComponentInChildren<Animator>();
        _transformCtrl  = GetComponent<PlayerTransformController>();
    }

    private void OnEnable()
    {
        health.OnHit       += HandleHit;
        Health.OnPlayerDied += HandlePlayerDied;
    }

    private void OnDisable()
    {
        health.OnHit       -= HandleHit;
        Health.OnPlayerDied -= HandlePlayerDied;
    }

    private void HandlePlayerDied()
    {
        StopAllCoroutines();
        IsHurt = false;
        if (animator != null) animator.enabled = true; // 죽을 때 Animator 복원
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        health.IsInvincible = false;
    }

    private void HandleHit(int amount, Vector2 source)
    {
        StopAllCoroutines();
        AudioManager.Instance?.PlaySFX(SoundType.PlayerHurt);
        health.IsInvincible = true;  // 히트스톱 대기 전 즉시 무적 — ContactDamage 연타 방지
        EffectSpawner.Instance?.SpawnLarge(GetEffectPoint(source));

        // 타임스톱 동안 피격 포즈 고정 (Animator가 스프라이트를 덮어쓰지 못하게 잠시 끔)
        Sprite hurtSprite = _transformCtrl?.CurrentForm switch
        {
            PlayerForm.Cat => catHurtSprite,
            PlayerForm.Dog => dogHurtSprite,
            _              => humanHurtSprite,
        };
        if (hurtSprite != null && spriteRenderer != null)
        {
            if (animator != null) animator.enabled = false;
            spriteRenderer.sprite = hurtSprite;
        }

        float stopDuration = HitStopManager.Instance?.Freeze(HitStopType.Long) ?? 0f;
        StartCoroutine(HurtRoutine(source, stopDuration));
    }

    private IEnumerator HurtRoutine(Vector2 source, float stopDuration)
    {
        // ① 히트스톱 대기 (timeScale=0 구간, Realtime으로 대기)
        if (stopDuration > 0f)
            yield return new WaitForSecondsRealtime(stopDuration);

        // 타임스톱 종료 → 평소 애니메이션 복원 (깜빡임 구간은 원래 스프라이트)
        if (animator != null) animator.enabled = true;

        // ② 넉백 — 피격 방향 반대로 밀어냄
        IsHurt = true;
        float dirX = transform.position.x >= source.x ? 1f : -1f;
        motor?.SetVelocityX(dirX * knockbackForce);
        motor?.SetVelocityY(knockbackUpForce);

        // ③ 무적은 HandleHit에서 이미 설정됨

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

    // 맞은 방향을 고려한 이펙트 생성 위치. 몸통 중심에서 공격자 쪽으로 약간 밀어낸다.
    private Vector2 GetEffectPoint(Vector2 hitSource)
    {
        Vector2 center = (Vector2)transform.position + _effectCenterOffset;
        Vector2 dir = hitSource - center;
        if (dir.sqrMagnitude < 0.0001f) return center;
        return center + dir.normalized * _effectEdgeDistance;
    }
}
