using UnityEngine;

[RequireComponent(typeof(ProjectilePool))]
public class FirstBossController : BossBase
{
    [Header("Phase Settings")]
    [
        SerializeField,
        Tooltip("HP가 이 값 이하일 때 Phase2 전환. 0이면 비활성(Phase1에서 처치 가능)")
    ]
    private int phase2HpThreshold = 0;

    [Header("Phase1 Pattern Settings")]
    [SerializeField]
    private BossPhase1State.Settings phase1Settings = new BossPhase1State.Settings
    {
        patternCooldown = 2f,
        firstPatternDelay = 1f,
        dashSpeed = 15f,
        dashMaxDuration = 1.5f,
        telegraphDuration = 0.5f,
        shotCount = 3,
        warningDuration = 1f,
        warningLineLength = 20f,
    };

    [Header("Dash Hitbox")]
    [SerializeField]
    private AttackHitbox dashHitbox;

    [Header("Projectile")]
    [SerializeField]
    private Transform[] projectileSpawnPoints;

    [SerializeField]
    private LineRenderer warningLine;

    public ProjectilePool Pool { get; private set; }
    public AttackHitbox DashHitbox => dashHitbox;
    public Transform[] ProjectileSpawnPoints => projectileSpawnPoints;
    public LineRenderer WarningLine => warningLine;

    private BossIntroState introState;
    private BossPhase1State phase1State;
    private BossPhase2State phase2State;
    private BossDeathState deathState;

    protected override BossStateBase DeathState => deathState;
    protected override BossStateBase InitialState => introState;

    protected override void Awake()
    {
        base.Awake();
        Pool = GetComponent<ProjectilePool>();
    }

    protected override void InitStates()
    {
        deathState = new BossDeathState(this);
        phase2State = new BossPhase2State(this);
        phase1State = new BossPhase1State(this, phase1Settings);
        introState = new BossIntroState(this, phase1State);
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
