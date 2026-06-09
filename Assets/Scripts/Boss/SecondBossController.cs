using UnityEngine;

[RequireComponent(typeof(ProjectilePool))]
public class SecondBossController : BossBase
{
    [Header("Phase Settings")]
    [SerializeField, Tooltip("HP가 이 값 이하일 때 Phase2 전환. 0이면 비활성(Phase1에서 처치 가능)")]
    private int phase2HpThreshold = 0;

    [SerializeField, Tooltip("Phase2 진입 시 애니메이션/패턴 속도 배율. 0/미설정이면 1로 폴백")]
    private float phase2SpeedMultiplier = 1.5f;

    [Header("Combat Settings (Phase1/2 공용)")]
    [SerializeField]
    private SecondBossCombatState.Settings combatSettings = new SecondBossCombatState.Settings
    {
        patternCooldown = 2f,
        firstPatternDelay = 1f,

        cleaveRange = 2.5f,
        cleaveTelegraph = 0.4f,
        cleaveActiveTime = 0.2f,

        smashMinRange = 3f,
        smashTelegraph = 0.5f,
        smashAirTime = 75f / 60f,     // 포물선 모양용 nominal(≈클립 25→100=1.25s). 착지 시점은 이벤트가 확정
        smashJumpHeight = 4f,
        smashJumpCount = 3,           // 3연속 점프
        smashInterJumpDelay = 0.25f,  // 점프 사이 텀
        smashGroggyDuration = 2.5f,

        fireBreathRange = 6f,
        fireBreathTelegraph = 0.6f,

        castMinRange = 5f,
        castTelegraph = 0.5f,
        castProjectileCount = 5,
        castSpreadAngle = 35f,
        castInterval = 0.12f,
    };

    [Header("Hitboxes")]
    [SerializeField] private AttackHitbox cleaveHitbox;
    [SerializeField] private AttackHitbox smashHitbox;
    [SerializeField] private AttackHitbox fireBreathHitbox;

    [Header("Cast Spell")]
    [Tooltip("투사체 발사 기준점(없으면 보스 위치). 보통 입 위치의 자식 Transform")]
    [SerializeField] private Transform projectileOrigin;

    public AttackHitbox CleaveHitbox => cleaveHitbox;
    public AttackHitbox SmashHitbox => smashHitbox;
    public AttackHitbox FireBreathHitbox => fireBreathHitbox;
    public Transform ProjectileOrigin => projectileOrigin;
    public ProjectilePool Pool { get; private set; }

    // 패턴/애니 속도 배율 (Phase1 = 1, Phase2 = phase2SpeedMultiplier). 코루틴이 시간값 스케일에 사용.
    public float PatternSpeedMultiplier { get; private set; } = 1f;
    private Animator _animator;

    // Phase2 진입 시 호출 — 애니메이션 속도 + 코드 패턴 속도를 동시에 가속.
    // animator.speed는 클립 재생과 Animation Event 발동 시점을 모두 비례로 당겨줌(수동 키프레임 이동 불필요).
    public void ApplyPhase2Speed()
    {
        float m = Mathf.Max(1f, phase2SpeedMultiplier); // 0/미설정 → 1 폴백
        PatternSpeedMultiplier = m;
        if (_animator != null) _animator.speed = m;
        Debug.Log($"[SecondBoss] Phase2 속도 x{m} 적용 (애니/이벤트/패턴 동시)");
    }

    // ── Animation Event 핸들러 ───────────────────────────────
    // Animator와 '같은 GameObject'의 컴포넌트만 이벤트가 닿음 → Animator를 보스 루트에 둘 것.
    // 클립에서 Function=AnimHitboxOn, String="smash" 식으로 호출.
    public void AnimHitboxOn(string which)
    {
        Debug.Log($"[SecondBoss] ▶ AnimHitboxOn(\"{which}\") 이벤트 수신");
        switch (which)
        {
            case "cleave":
                cleaveHitbox?.Activate();
                AudioManager.Instance?.PlaySFX(SoundType.BossCleaveImpact); // 도끼 땅 박힘
                break;
            case "smash":
                smashHitbox?.Activate();
                break;
            case "fire_breath":
                fireBreathHitbox?.Activate();
                FireBreathOn = true;
                break;
        }
    }

    public void AnimHitboxOff(string which)
    {
        Debug.Log($"[SecondBoss] ▶ AnimHitboxOff(\"{which}\") 이벤트 수신");
        switch (which)
        {
            case "cleave":      cleaveHitbox?.Deactivate();     break;
            case "smash":       smashHitbox?.Deactivate();      break;
            case "fire_breath": fireBreathHitbox?.Deactivate(); FireBreathOn = false; break;
        }
    }

    // ── Smash 전용 이벤트 신호 (직렬화 안 함 → Map6 0-기본값 함정 회피) ──
    [System.NonSerialized] public bool SmashJumpSignaled;
    [System.NonSerialized] public bool SmashLandSignaled;
    public void ResetSmashSignals() { SmashJumpSignaled = false; SmashLandSignaled = false; }

    // FireBreath 히트박스 ON 상태 (AnimHitboxOn/Off "fire_breath"가 토글). 스윕 종료 판정에 사용.
    [System.NonSerialized] public bool FireBreathOn;

    // Smash 클립 frame 25 이벤트 (도약 시작)
    public void AnimSmashJump()
    {
        Debug.Log("[SecondBoss] ▶ AnimSmashJump 이벤트 수신 (도약)");
        SmashJumpSignaled = true;
        AudioManager.Instance?.PlaySFX(SoundType.BossSmashJump); // 도약 사운드
    }

    // Smash 클립 frame 100 이벤트 (착지 확정 + 충격 히트박스 ON)
    public void AnimSmashLand()
    {
        Debug.Log("[SecondBoss] ▶ AnimSmashLand 이벤트 수신 (착지+히트박스 ON)");
        SmashLandSignaled = true;
        smashHitbox?.Activate();
        AudioManager.Instance?.PlaySFX(SoundType.BossSmashLand); // 착지 충격 사운드
    }

    private BossIntroState introState;
    private SecondBossPhase1State phase1State;
    private SecondBossPhase2State phase2State;
    private BossDeathState deathState;

    protected override BossStateBase DeathState => deathState;
    protected override BossStateBase InitialState => introState;

    protected override void Awake()
    {
        base.Awake();
        Pool = GetComponent<ProjectilePool>();
        _animator = GetComponent<Animator>();
    }

    protected override void InitStates()
    {
        deathState  = new BossDeathState(this, triggerClear: triggerClearOnDeath);
        phase2State = new SecondBossPhase2State(this, combatSettings);
        phase1State = new SecondBossPhase1State(this, combatSettings);
        introState  = new BossIntroState(this, phase1State);
    }

    protected override void Update()
    {
        base.Update();
        CheckPhaseTransition();
    }

    private void CheckPhaseTransition()
    {
        if (Fsm.Current != phase1State)
            return;
        if (phase2HpThreshold > 0 && Health.CurrentHp <= phase2HpThreshold)
            TransitionTo(phase2State);
    }
}
