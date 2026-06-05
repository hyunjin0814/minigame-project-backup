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

    // ── 보스 ───────────────────────────────────────────────────────
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

    // ── UI ─────────────────────────────────────────────────────────
    UIPause,
    UIUnpause,
    UIButtonClick,
    UIGameOver,
    UIStageClear,

    // ── 월드 / 맵 ──────────────────────────────────────────────────
    Checkpoint,
    ItemPickup,
    ItemHeal,
    AbilityUnlock,
    MaxHpUp,
    DoorUnlock,
    SecretFound,
    BossRoomEnter,
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
