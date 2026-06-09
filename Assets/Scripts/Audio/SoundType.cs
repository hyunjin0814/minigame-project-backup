/// <summary>
/// 게임 내 모든 효과음(SFX) 종류.
/// AudioManager.PlaySFX(SoundType.X) 로 재생.
/// </summary>
public enum SoundType
{
    // ── 플레이어 이동 ──────────────────────────────────────────────
    PlayerJump,
    PlayerLand,
    PlayerLandHeavy,     // 포고 다운어택 착지

    // ── 플레이어 전투 ──────────────────────────────────────────────
    PlayerDash,
    PlayerAttackSwing,
    PlayerAttackHitLight,    // 잡몹 타격 (HitStop Light)
    PlayerAttackHitHeavy,    // 보스 타격 (HitStop Heavy)
    PlayerAttackHitCritical, // 백스탭·마무리 (HitStop Critical)
    PlayerHurt,
    PlayerDeath,

    // ── 변신 ───────────────────────────────────────────────────────
    TransformHuman,
    TransformDog,
    TransformCat,

    // ── 강아지 스킬 ────────────────────────────────────────────────
    DogScan,
    DogDashCharge,
    DogDashImpact,

    // ── 고양이 스킬 ────────────────────────────────────────────────
    CatStealthOn,
    CatStealthOff,

    // ── 적 ─────────────────────────────────────────────────────────
    EnemyAlert,              // 플레이어 감지 (느낌표)
    EnemyAttackSwing,
    EliteShieldBlock,        // 방패 막기 차단
    EnemyProjectileLaunch,
    EnemyProjectileImpact,
    EnemyWeaknessExpose,     // 약점 노출
    EnemyWeaknessClear,      // 약점 해제
    EnemyHurt,
    EnemyDeath,

    // ── 보스 공통 ─────────────────────────────────────────────────
    BossIntro,
    BossDashCharge,
    BossWallHit,             // 대시 후 벽 충돌 → 그로기
    BossProjectileLaunch,
    BossProjectileImpact,
    BossGroggyEnter,
    BossGroggyExit,
    BossWeaknessExpose,
    BossWeaknessClear,
    BossHurt,
    BossPhaseTransition,     // 페이즈 전환
    BossDeath,

    // ── 2보스 패턴별 ───────────────────────────────────────────────
    BossCleaveCharge,        // 도끼 들어올림 (텔레그래프 시작)
    BossCleaveImpact,        // 도끼 땅 박힘 (AnimHitboxOn "cleave")
    BossSmashReady,          // 점프 준비 (텔레그래프 시작)
    BossSmashJump,           // 도약 (AnimSmashJump 이벤트)
    BossSmashLand,           // 착지 충격 (AnimSmashLand 이벤트)
    BossFireBreathCharge,    // 화염 차징 (텔레그래프 시작)
    BossFireBreathShoot,     // 화염 발사 (히트박스 ON 직후)
    BossCastCharge,          // 구체 차징 (텔레그래프 시작)
    BossCastLaunch,          // 구체 발사 (투사체 생성마다)

    // ── UI ─────────────────────────────────────────────────────────
    UIPause,
    UIUnpause,
    UIButtonClick,
    UIGameOver,
    UIStageClear,
    InteractFail,        // 상호작용 조건 미충족 (열쇠 없음 등)

    // ── 월드 / 맵 ──────────────────────────────────────────────────
    Checkpoint,
    ItemPickup,
    ItemHeal,
    AbilityUnlock,
    MaxHpUp,
    DoorUnlock,
    SecretFound,
    BossRoomEnter,
    LeverAttach,         // 레버 소켓에 부착 (1차 상호작용)
    LeverPull,           // 레버 당김 → 비밀방 오픈 (2차 상호작용)
}

/// <summary>
/// 배경음악(BGM) 종류.
/// AudioManager.PlayBGM(BGMType.X) 로 전환.
/// </summary>
public enum BGMType
{
    None,
    Title,
    Field,
    Boss,
    GameOver,
    StageClear,
}
