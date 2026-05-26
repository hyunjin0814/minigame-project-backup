using System.Collections;
using UnityEngine;

public class BossPhase1State : BossStateBase
{
    private readonly FirstBossController firstBoss;

    [System.Serializable]
    public struct Settings
    {
        public float patternCooldown;    // 패턴 간격 (초)
        public float firstPatternDelay;  // 진입 후 첫 패턴까지 대기
        public float dashSpeed;
        public float dashMaxDuration;
        public float telegraphDuration;
        public int shotCount;            // 발사체 발사 횟수
        public float warningDuration;    // 궤적 예고선 표시 시간 (초)
        public float warningLineLength;  // 예고선 길이
        public float groggyDurationOnWallHit; // 돌진 벽 충돌 후 그로기 지속 (권장 2.5~3f)
    }

    private readonly Settings settings;
    private float patternTimer;
    private bool isExecutingPattern;
    private Coroutine currentPattern;

    public BossPhase1State(FirstBossController boss, Settings settings) : base(boss)
    {
        firstBoss = boss;
        this.settings = settings;
    }

    public override void Enter()
    {
        patternTimer = settings.firstPatternDelay;
        isExecutingPattern = false;
        currentPattern = null;
        Boss.OnGroggyEnded += HandleGroggyEnded;
        Debug.Log("[Boss] Phase1State Enter");
    }

    public override void Exit()
    {
        Boss.OnGroggyEnded -= HandleGroggyEnded;

        // 페이즈 전환 시 그로기/약점 상태 강제 정리 (어색한 상태 잔류 방지)
        if (Boss.IsGroggy) Boss.ExitGroggy();

        if (currentPattern != null)
            Boss.StopCoroutine(currentPattern);
        Boss.IsDashing = false;
        Boss.Rb.linearVelocity = Vector2.zero;
        HideWarningLine();
    }

    private void HandleGroggyEnded()
    {
        // 그로기 종료 후 짧은 호흡 (플레이어 폼 복귀 시간)
        patternTimer = 0.3f;
    }

    public override void Update()
    {
        if (Boss.IsGroggy) return; // 그로기 중 패턴 차단
        if (isExecutingPattern) return;

        patternTimer -= Time.deltaTime;
        if (patternTimer <= 0f)
        {
            int choice = Random.Range(0, 2);
            isExecutingPattern = true;
            currentPattern = Boss.StartCoroutine(choice == 0 ? DashRoutine() : ShootRoutine());
            patternTimer = settings.patternCooldown;
        }
    }

    // 패턴 1: 직선 돌진
    private IEnumerator DashRoutine()
    {
        if (Boss.PlayerTarget == null) { isExecutingPattern = false; yield break; }

        // 텔레그래프: 방향 락온 (수평만) + 색 변경
        float xSign = Mathf.Sign(Boss.PlayerTarget.position.x - Boss.transform.position.x);
        if (xSign == 0f) xSign = 1f; // 정확히 같은 x — fallback
        Vector2 dir = new Vector2(xSign, 0f);
        Boss.Sprite.color = new Color(1f, 0.6f, 0f); // 주황색 = 돌진 예고
        yield return new WaitForSeconds(settings.telegraphDuration);
        Boss.Sprite.color = Color.white;

        // 돌진 — 수평만, 중력 일시 제거 (포물선 방지)
        Boss.HitWall = false;
        Boss.IsDashing = true;
        float originalGravity = Boss.Rb.gravityScale;
        Boss.Rb.gravityScale = 0f;
        Boss.Rb.linearVelocity = new Vector2(dir.x * settings.dashSpeed, 0f);
        firstBoss.DashHitbox?.Activate();

        float elapsed = 0f;
        Vector2 lastPos = Boss.transform.position;
        float stuckTimer = 0f;

        while (elapsed < settings.dashMaxDuration && !Boss.HitWall)
        {
            elapsed += Time.deltaTime;

            // 실제 이동 거리 체크 — Tilemap/Composite(Outlines) 등에서 OnCollisionEnter2D 누락 시 백업
            float moved = Vector2.Distance(Boss.transform.position, lastPos);
            if (moved < 0.02f) stuckTimer += Time.deltaTime;
            else stuckTimer = 0f;
            lastPos = Boss.transform.position;

            // 0.1s 가속 grace 후, 0.1s 이상 거의 안 움직였으면 벽에 막힌 것으로 판정
            if (elapsed > 0.1f && stuckTimer > 0.1f)
            {
                Boss.HitWall = true;
                break;
            }

            yield return null;
        }

        firstBoss.DashHitbox?.Deactivate();
        Boss.Rb.gravityScale = originalGravity; // 중력 복원
        Boss.Rb.linearVelocity = Vector2.zero;
        Boss.IsDashing = false;

        // 벽 충돌 시에만 그로기 진입 (시간 만료는 그로기 없음)
        if (Boss.HitWall)
        {
            Boss.EnterGroggy(settings.groggyDurationOnWallHit);
        }

        isExecutingPattern = false;
    }

    // 패턴 2: 랜덤 스폰 포인트에서 순차 발사 (궤적 예고 포함)
    private IEnumerator ShootRoutine()
    {
        if (Boss.PlayerTarget == null || firstBoss.ProjectileSpawnPoints.Length == 0)
        {
            isExecutingPattern = false;
            yield break;
        }

        for (int i = 0; i < settings.shotCount; i++)
        {
            // 랜덤 스폰 포인트 선택
            int idx = Random.Range(0, firstBoss.ProjectileSpawnPoints.Length);
            Vector2 from = firstBoss.ProjectileSpawnPoints[idx].position;
            Vector2 dir = ((Vector2)Boss.PlayerTarget.position - from).normalized;

            // 궤적 예고선 표시
            ShowWarningLine(from, from + dir * settings.warningLineLength);
            yield return new WaitForSeconds(settings.warningDuration);

            // 발사 + 예고선 제거
            HideWarningLine();
            firstBoss.Pool.Spawn(from, dir);
        }

        isExecutingPattern = false;
    }

    private void ShowWarningLine(Vector2 start, Vector2 end)
    {
        var lr = firstBoss.WarningLine;
        if (lr == null) return;
        lr.gameObject.SetActive(true);
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
    }

    private void HideWarningLine()
    {
        if (firstBoss.WarningLine != null)
            firstBoss.WarningLine.gameObject.SetActive(false);
    }
}
