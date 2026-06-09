using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Phase1/2가 공유하는 전투 상태. 활성 패턴 집합만 다르게 받는다.
public abstract class SecondBossCombatState : BossStateBase
{
    public enum Pattern { Cleave, Smash, FireBreath, CastSpell }

    [System.Serializable]
    public struct Settings
    {
        public float patternCooldown;
        public float firstPatternDelay;

        [Header("Cleave (근접 베기)")]
        public float cleaveRange;       // 이 거리 이내일 때 후보
        public float cleaveTelegraph;
        public float cleaveActiveTime;

        [Header("Smash (점프 착지)")]
        public float smashMinRange;     // 이 거리보다 멀 때 후보
        public float smashTelegraph;
        public float smashAirTime;      // 포물선 모양용 nominal(≈클립 25→100). 착지 시점은 이벤트가 확정
        public float smashJumpHeight;
        public int   smashJumpCount;      // 연속 점프 횟수(예: 3). 0/미설정이면 1회로 폴백
        public float smashInterJumpDelay; // 점프 사이 텀(초)
        public float smashGroggyDuration; // 마지막 착지 후 그로기(강아지 윈도우)

        [Header("Fire Breath (Phase2)")]
        public float fireBreathRange;     // 정면 도달 거리(이 이내일 때 후보)
        public float fireBreathTelegraph;
        // 히트박스 위치·ON/OFF는 fire_breath 클립이 담당(키프레임 + AnimHitboxOn/Off "fire_breath")

        [Header("Cast Spell (Phase2, 투사체 부채꼴)")]
        public float castMinRange;      // 이 거리보다 멀 때 후보
        public float castTelegraph;
        public int   castProjectileCount;
        public float castSpreadAngle;   // 위/아래 분산 각도(도)
        public float castInterval;
    }

    protected readonly SecondBossController boss;
    protected readonly Settings s;
    private readonly Pattern[] enabled;

    private float patternTimer;
    private bool busy;
    private Coroutine current;

    // smash 점프 중 중력 복원 안전망 (사망/페이즈 전환 중단 대비)
    private float savedGravity;
    private bool gravityOverridden;

    protected SecondBossCombatState(SecondBossController b, Settings settings, Pattern[] enabledPatterns)
        : base(b)
    {
        boss = b;
        s = settings;
        enabled = enabledPatterns;
    }

    public override void Enter()
    {
        patternTimer = s.firstPatternDelay;
        busy = false;
        current = null;
        Boss.OnGroggyEnded += HandleGroggyEnded;
    }

    public override void Exit()
    {
        Boss.OnGroggyEnded -= HandleGroggyEnded;
        if (Boss.IsGroggy) Boss.ExitGroggy();
        if (current != null) Boss.StopCoroutine(current);
        Boss.IsDashing = false;
        // smash 점프 도중 중단 시 중력 0인 채 떠버리지 않게 복원
        if (gravityOverridden) { Boss.Rb.gravityScale = savedGravity; gravityOverridden = false; }
        Boss.Rb.linearVelocity = Vector2.zero;
        DeactivateAllHitboxes();
    }

    private void HandleGroggyEnded() => patternTimer = 0.3f;

    // 패턴 시간값을 페이즈 속도로 스케일 (Phase2에서 PatternSpeedMultiplier=1.5 → 코드도 1.5배 빠름)
    private float Scaled(float seconds) => seconds / boss.PatternSpeedMultiplier;

    public override void Update()
    {
        if (Boss.IsGroggy) return;
        if (busy) return;

        patternTimer -= Time.deltaTime;
        if (patternTimer <= 0f)
        {
            Pattern p = SelectPattern();
            Debug.Log($"[SecondBoss] 패턴 선택 → {p}");
            busy = true;
            current = Boss.StartCoroutine(RunPattern(p));
            patternTimer = Scaled(s.patternCooldown);
        }
    }

    private IEnumerator RunPattern(Pattern p)
    {
        // 안전망: 이전 패턴 히트박스(이벤트 OFF 누락 등)가 남아있지 않게 초기화
        DeactivateAllHitboxes();

        switch (p)
        {
            case Pattern.Cleave:     yield return CleaveRoutine();     break;
            case Pattern.Smash:      yield return SmashRoutine();      break;
            case Pattern.FireBreath: yield return FireBreathRoutine(); break;
            case Pattern.CastSpell:  yield return CastSpellRoutine();  break;
        }
        busy = false;
    }

    // ── 위치 조건부 패턴 선택 ─────────────────────────────────
    protected Pattern SelectPattern()
    {
        if (Boss.PlayerTarget == null)
        {
            Debug.LogWarning("[SecondBoss] PlayerTarget == null → Cleave fallback (플레이어 Tag가 'Player'인지 확인)");
            return Pattern.Cleave;
        }

        float absDx = Mathf.Abs(Boss.PlayerTarget.position.x - Boss.transform.position.x);

        var weights = new List<(Pattern p, int w)>();
        foreach (var pat in enabled)
        {
            switch (pat)
            {
                case Pattern.Cleave:
                    if (absDx <= s.cleaveRange) weights.Add((pat, 3)); // 가까이 앞
                    break;
                case Pattern.Smash:
                    if (absDx > s.smashMinRange) weights.Add((pat, 2)); // 수평으로 떨어짐
                    break;
                case Pattern.FireBreath:
                    if (absDx <= s.fireBreathRange) weights.Add((pat, 2)); // 정면 도달거리
                    break;
                case Pattern.CastSpell:
                    if (absDx > s.castMinRange) weights.Add((pat, 2)); // 멀리
                    break;
            }
        }

        Debug.Log($"[SecondBoss] SelectPattern absDx={absDx:F2}, 후보 {weights.Count}개");

        // 후보 없음(애매한 중간거리 등) → 거리로 fallback
        if (weights.Count == 0)
        {
            if (HasPattern(Pattern.Smash) && absDx > s.cleaveRange) return Pattern.Smash;
            return Pattern.Cleave;
        }

        int total = 0;
        foreach (var w in weights) total += w.w;
        int r = Random.Range(0, total);
        foreach (var w in weights)
        {
            if (r < w.w) return w.p;
            r -= w.w;
        }
        return weights[0].p;
    }

    private bool HasPattern(Pattern p)
    {
        foreach (var e in enabled) if (e == p) return true;
        return false;
    }

    // ── 패턴 1: Cleave (정면 근접 베기) ───────────────────────
    private IEnumerator CleaveRoutine()
    {
        Debug.Log("[SecondBoss] Cleave 시작");
        Boss.FacePlayer();
        Boss.SetIntentColor(new Color(1f, 0.6f, 0.2f)); // 주황 텔레그래프
        AudioManager.Instance?.PlaySFX(SoundType.BossCleaveCharge); // 도끼 들어올림
        yield return new WaitForSeconds(Scaled(s.cleaveTelegraph));
        Boss.SetIntentColor(Color.white);
        // BossCleaveImpact → AnimHitboxOn("cleave") 에서 재생

        Boss.RaiseAttackAnim("cleave");
        if (boss.CleaveHitbox == null) Debug.LogWarning("[SecondBoss] CleaveHitbox 미할당!");
        // 히트박스 ON/OFF는 Cleave 클립의 Animation Event(AnimHitboxOn/Off "cleave")가 담당.
        yield return new WaitForSeconds(Scaled(s.cleaveActiveTime)); // 애니 재생 동안 상태 점유(다음 패턴 차단)
        Debug.Log("[SecondBoss] Cleave 종료");
    }

    // ── 패턴 2: Smash (N연속 점프 → 마지막 착지 후 그로기) ─────
    // 점프마다 smash 클립을 재트리거 → frame 25/100 이벤트(AnimSmashJump/Land)가 매번 발동.
    // 체공시간(smashAirTime)은 포물선 모양용 nominal, 착지 '시점'은 AnimSmashLand가 확정.
    private IEnumerator SmashRoutine()
    {
        if (Boss.PlayerTarget == null) yield break;

        Boss.FacePlayer();
        Boss.SetIntentColor(new Color(0.7f, 0.3f, 1f)); // 보라 텔레그래프
        AudioManager.Instance?.PlaySFX(SoundType.BossSmashReady); // 점프 준비
        yield return new WaitForSeconds(Scaled(s.smashTelegraph));
        Boss.SetIntentColor(Color.white);
        // BossSmashJump → AnimSmashJump() 에서 재생
        // BossSmashLand → AnimSmashLand() 에서 재생

        int count = Mathf.Max(1, s.smashJumpCount); // 미설정(0)이면 1회로 안전 폴백
        for (int jump = 0; jump < count; jump++)
        {
            boss.ResetSmashSignals();
            Boss.RaiseAttackAnim("smash"); // 점프마다 클립 재생(재시작)
            Debug.Log($"[SecondBoss] Smash {jump + 1}/{count}: 도약 신호 대기...");

            // 도약 프레임(frame 25) 이벤트 대기 — 누락 대비 1.5s 안전 타임아웃
            float w = 0f;
            while (!boss.SmashJumpSignaled && w < 1.5f) { w += Time.deltaTime; yield return null; }

            Boss.FacePlayer(); // 매 점프 직전 방향 재조정
            float startX  = Boss.transform.position.x;
            float startY  = Boss.transform.position.y;
            float targetX = Boss.PlayerTarget.position.x; // 매 점프마다 현재 플레이어 위치로 락온

            savedGravity = Boss.Rb.gravityScale;
            gravityOverridden = true;
            Boss.Rb.gravityScale = 0f;
            Boss.Rb.linearVelocity = Vector2.zero;

            // 착지 이벤트가 올 때까지 포물선 이동 (nominal로 진행도 정규화)
            float t = 0f;
            float nominal = Mathf.Max(0.01f, Scaled(s.smashAirTime));
            while (!boss.SmashLandSignaled && t < nominal * 2f) // *2 = 이벤트 누락 안전망
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / nominal);
                float x = Mathf.Lerp(startX, targetX, k);
                float y = startY + s.smashJumpHeight * 4f * k * (1f - k); // k=0.5 최고점, 양끝 startY
                Boss.Rb.MovePosition(new Vector2(x, y));
                yield return null;
            }

            Debug.Log($"[SecondBoss] Smash {jump + 1}/{count}: 착지 (신호={boss.SmashLandSignaled})"
                + (boss.SmashLandSignaled ? "" : " ⚠ 타임아웃 — AnimSmashLand 미발생"));

            // 착지 확정
            Boss.Rb.MovePosition(new Vector2(targetX, startY));
            Boss.Rb.gravityScale = savedGravity;
            gravityOverridden = false;
            Boss.Rb.linearVelocity = Vector2.zero;
            // 충격 히트박스 ON=AnimSmashLand, OFF=AnimHitboxOff "smash"

            // 점프 사이 텀 (마지막 점프 제외)
            if (jump < count - 1 && s.smashInterJumpDelay > 0f)
                yield return new WaitForSeconds(Scaled(s.smashInterJumpDelay));
        }

        if (boss.SmashHitbox == null) Debug.LogWarning("[SecondBoss] SmashHitbox 미할당!");

        // 모든 점프 후 그로기 — 강아지 스캔+대시 펀치 윈도우
        Boss.EnterGroggy(s.smashGroggyDuration);
    }

    // ── 패턴 3: Fire Breath (Phase2) ─────────────────────────
    // 히트박스의 위치(클립 키프레임)와 ON/OFF(AnimHitboxOn/Off "fire_breath" 이벤트)를
    // 모두 fire_breath 클립이 담당. 코드는 텔레그래프 후 클립을 재생하고 분사가 끝날 때까지 점유만 한다.
    private IEnumerator FireBreathRoutine()
    {
        Boss.FacePlayer();
        Boss.SetIntentColor(new Color(1f, 0.3f, 0.1f)); // 붉은 텔레그래프
        AudioManager.Instance?.PlaySFX(SoundType.BossFireBreathCharge); // 화염 차징
        yield return new WaitForSeconds(Scaled(s.fireBreathTelegraph));
        Boss.SetIntentColor(Color.white);

        if (boss.FireBreathHitbox == null)
            Debug.LogWarning("[SecondBoss] FireBreathHitbox 미할당!");

        boss.FireBreathOn = false;
        Boss.RaiseAttackAnim("fire_breath");
        Debug.Log("[SecondBoss] FireBreath 시작 — 클립이 위치/ON·OFF 담당");

        // 분사 ON 이벤트 대기 (0.6s 내 안 오면 클립에 이벤트가 없는 것)
        float w = 0f;
        while (!boss.FireBreathOn && w < 0.6f) { w += Time.deltaTime; yield return null; }
        if (boss.FireBreathOn)
            AudioManager.Instance?.PlaySFX(SoundType.BossFireBreathShoot); // 화염 발사 시작
        else
            Debug.LogWarning("[SecondBoss] FireBreath ON 이벤트 미발생 — 클립에 AnimHitboxOn \"fire_breath\" 확인");

        // OFF 이벤트까지 상태 점유 (안전 타임아웃 3s)
        float t = 0f;
        while (boss.FireBreathOn && t < 3f) { t += Time.deltaTime; yield return null; }

        // OFF 이벤트 누락 안전망 — 강제로 끄기
        if (boss.FireBreathOn)
        {
            Debug.LogWarning("[SecondBoss] FireBreath OFF 이벤트 미발생 → 강제 종료");
            boss.FireBreathHitbox?.Deactivate();
            boss.FireBreathOn = false;
        }
    }

    // ── 패턴 4: Cast Spell (정면 위/아래 분산 투사체) ──────────
    private IEnumerator CastSpellRoutine()
    {
        if (Boss.PlayerTarget == null) yield break;

        Boss.FacePlayer();
        Boss.SetIntentColor(new Color(0.3f, 0.6f, 1f)); // 파랑 텔레그래프
        AudioManager.Instance?.PlaySFX(SoundType.BossCastCharge); // 구체 차징
        yield return new WaitForSeconds(Scaled(s.castTelegraph));
        Boss.SetIntentColor(Color.white);

        Boss.RaiseAttackAnim("cast_spell");
        Debug.Log($"[SecondBoss] CastSpell 시작 ({s.castProjectileCount}발)");
        if (boss.Pool == null) Debug.LogWarning("[SecondBoss] ProjectilePool 없음! (cast 투사체 안 나감)");

        float facing = Mathf.Sign(Boss.transform.localScale.x);
        if (facing == 0f) facing = 1f;
        Vector2 origin = boss.ProjectileOrigin != null
            ? (Vector2)boss.ProjectileOrigin.position
            : (Vector2)Boss.transform.position;

        for (int i = 0; i < s.castProjectileCount; i++)
        {
            float angle = Random.Range(-s.castSpreadAngle, s.castSpreadAngle); // 위/아래 랜덤
            Vector2 dir = new Vector2(
                facing * Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            ).normalized;

            boss.Pool.Spawn(origin, dir);
            AudioManager.Instance?.PlaySFX(SoundType.BossCastLaunch); // 구체 발사마다
            if (s.castInterval > 0f) yield return new WaitForSeconds(Scaled(s.castInterval));
        }
    }

    // ── 헬퍼 ─────────────────────────────────────────────────
    private void DeactivateAllHitboxes()
    {
        boss.CleaveHitbox?.Deactivate();
        boss.SmashHitbox?.Deactivate();
        boss.FireBreathHitbox?.Deactivate();
    }
}
