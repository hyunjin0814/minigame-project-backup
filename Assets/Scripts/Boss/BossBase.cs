using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(Health))]
public abstract class BossBase : MonoBehaviour, IWeaknessTarget
{
    [Header("Drop")]
    [SerializeField]
    private GameObject dropItemPrefab;

    [SerializeField]
    private GameObject stageClearZone;

    [Header("Damage")]
    [Tooltip("약점 노출 중 받는 데미지 배율 (EnemyBase와 동일 — 강아지 스캔 보상)")]
    [SerializeField]
    private float weaknessDamageMultiplier = 2f;

    public Rigidbody2D Rb { get; private set; }
    public SpriteRenderer Sprite { get; private set; }
    public Health Health { get; private set; }
    public Transform PlayerTarget { get; private set; }
    public bool HitWall { get; set; }
    public bool IsDashing { get; set; }

    protected BossStateMachine Fsm { get; private set; }
    protected abstract BossStateBase DeathState { get; }
    protected abstract BossStateBase InitialState { get; }

    public void Activate() => TransitionTo(InitialState);

    // 피격 무적 (짧은 색 플래시 + 데미지 차단)
    private bool hitInvincible;

    // 페이즈 전환 무적 (연출 없이 순수 데미지 차단)
    private bool phaseInvincible;

    // ── 색상 의도 시스템 ─────────────────────────────────────
    // Sprite.color는 여러 시스템이 공유(텔레그래프/그로기/Hit 플래시).
    // 각 시스템은 SetIntentColor로 "원하는 색"을 등록. Hit 플래시는 짧게 빨강을 덮은 뒤
    // intentColor로 복원 — 텔레그래프/그로기 틴트가 사라지지 않게.
    private Color _intentColor = Color.white;
    private bool _isHitFlashing;

    public void SetIntentColor(Color c)
    {
        _intentColor = c;
        if (!_isHitFlashing) Sprite.color = c;
    }

    // ── 약점 시스템 (IWeaknessTarget) ─────────────────────────
    public bool IsWeaknessExposed { get; private set; }
    public event Action<bool> OnWeaknessChanged;
    Transform IWeaknessTarget.Transform => transform;
    public bool CanBeSensedExternally => IsGroggy;
    private float _weaknessTimer;

    // ── 그로기 시스템 ────────────────────────────────────────
    public bool IsGroggy { get; private set; }
    public event Action OnGroggyStarted;
    public event Action OnGroggyEnded;
    private float _groggyTimer;

    // ── 시각 표현용 이벤트 (BossAnimator가 구독) ─────────────
    public event Action Hurt;
    public event Action Died;
    public event Action AttackPerformed;
    public event Action TelegraphPerformed;
    public void RaiseAttackPerformed() => AttackPerformed?.Invoke();
    public void RaiseTelegraphPerformed() => TelegraphPerformed?.Invoke();

    protected virtual void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        Sprite = GetComponent<SpriteRenderer>();
        Health = GetComponent<Health>();
        Health.DamageModifier = ComputeFinalDamage;
        Fsm = new BossStateMachine();
    }

    // 약점 노출 중에는 데미지 ×weaknessDamageMultiplier (EnemyBase 일관성)
    private int ComputeFinalDamage(int baseDamage, Vector2 source)
    {
        if (IsWeaknessExposed)
            return Mathf.RoundToInt(baseDamage * weaknessDamageMultiplier);
        return baseDamage;
    }

    protected virtual void Start()
    {
        var player = GameObject.FindWithTag("Player");
        if (player != null)
            PlayerTarget = player.transform;

        Health.OnHit += OnHit;
        Health.OnDeath += OnDeath;

        InitStates();
    }

    protected virtual void Update()
    {
        TickGroggy();
        TickWeakness();
        Fsm.Update();
    }

    public void TransitionTo(BossStateBase next) => Fsm.ChangeState(next);

    // 플레이어 방향으로 한 번 flip — 각 패턴 시작 시점에 호출
    public void FacePlayer()
    {
        if (PlayerTarget == null) return;
        float dx = PlayerTarget.position.x - transform.position.x;
        if (Mathf.Abs(dx) < 0.01f) return;
        Vector3 s = transform.localScale;
        s.x = dx > 0f ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
        transform.localScale = s;
    }

    private void OnCollisionEnter2D(Collision2D collision) => HitWall = true;

    // 각 보스가 상태 초기화 및 첫 상태 진입을 담당
    protected abstract void InitStates();

    // 페이즈 전환 무적 (연출 없음)
    public void SetPhaseInvincible(bool value)
    {
        phaseInvincible = value;
        RefreshInvincible();
    }

    private void RefreshInvincible()
    {
        Health.IsInvincible = hitInvincible || phaseInvincible;
    }

    // ── 약점/그로기 (IWeaknessTarget) ────────────────────────
    public void ExposeWeakness(float duration)
    {
        _weaknessTimer = duration;
        if (IsWeaknessExposed) return;
        IsWeaknessExposed = true;
        OnWeaknessChanged?.Invoke(true);
        WeaknessRegistry.NotifyExposed(this);
        Debug.Log($"[Boss] 약점 노출 ({duration:F1}s) — 데미지 x{weaknessDamageMultiplier}");
    }

    public void ClearWeakness()
    {
        if (!IsWeaknessExposed) return;
        IsWeaknessExposed = false;
        _weaknessTimer = 0f;
        OnWeaknessChanged?.Invoke(false);
        WeaknessRegistry.NotifyCleared(this);
        Debug.Log("[Boss] 약점 노출 종료");
    }

    public void EnterGroggy(float duration)
    {
        if (IsGroggy) { _groggyTimer = Mathf.Max(_groggyTimer, duration); return; }
        IsGroggy = true;
        _groggyTimer = duration;
        Rb.linearVelocity = Vector2.zero;
        SetIntentColor(new Color(0.6f, 0.7f, 1f)); // 임시 파란 틴트 + 강아지 아이콘(BossGroggyIndicator) 병행
        Debug.Log($"[Boss] Groggy 진입 ({duration:F1}s)");
        OnGroggyStarted?.Invoke();
    }

    public void ExitGroggy()
    {
        if (!IsGroggy) return;
        IsGroggy = false;
        SetIntentColor(Color.white);
        if (IsWeaknessExposed) ClearWeakness(); // Q5 A: 그로기 잔여 시간 = 약점 윈도우 상한
        Debug.Log("[Boss] Groggy 종료");
        OnGroggyEnded?.Invoke();
    }

    private void TickWeakness()
    {
        if (!IsWeaknessExposed) return;
        _weaknessTimer -= Time.deltaTime;
        if (_weaknessTimer <= 0f) ClearWeakness();
    }

    private void TickGroggy()
    {
        if (!IsGroggy) return;
        _groggyTimer -= Time.deltaTime;
        if (_groggyTimer <= 0f) ExitGroggy();
    }

    private void OnHit(int amount, Vector2 _)
    {
        StartCoroutine(HitFlashRoutine());
        Hurt?.Invoke();
    }

    // 피격 무적: 색 플래시 0.1s + 무적 0.4s
    // 플래시 후 intentColor로 복원 — 텔레그래프/그로기 틴트가 사라지지 않게.
    private IEnumerator HitFlashRoutine()
    {
        hitInvincible = true;
        RefreshInvincible();

        _isHitFlashing = true;
        Sprite.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        _isHitFlashing = false;
        Sprite.color = _intentColor;
        yield return new WaitForSeconds(0.3f);

        hitInvincible = false;
        RefreshInvincible();
    }

    private void OnDeath()
    {
        Health.OnHit -= OnHit;
        Health.OnDeath -= OnDeath;
        if (IsWeaknessExposed) ClearWeakness();
        if (IsGroggy) ExitGroggy();
        Died?.Invoke();
        Fsm.ChangeState(DeathState);
    }

    public void DropItem()
    {
        if (dropItemPrefab != null)
            Instantiate(dropItemPrefab, transform.position, Quaternion.identity);
        if (stageClearZone != null)
            stageClearZone.SetActive(true);
    }

    protected virtual void OnDestroy()
    {
        Health.OnHit -= OnHit;
        Health.OnDeath -= OnDeath;
    }
}
