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
        public float warningArrowHeadSize; // 화살촉 크기 (월드 유닛, 권장 1.0~1.5)
        public float warningBlinkInterval; // 깜빡임 한 사이클 시간 (s, 권장 0.2~0.3)
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

    // 패턴 2: 사각 영역 내 랜덤 위치에서 순차 발사 (화살표 예고 + 깜빡임)
    private IEnumerator ShootRoutine()
    {
        if (Boss.PlayerTarget == null)
        {
            isExecutingPattern = false;
            yield break;
        }

        for (int i = 0; i < settings.shotCount; i++)
        {
            // 영역 내 랜덤 좌표 (월드 좌표)
            Vector2 areaMin = firstBoss.ProjectileSpawnAreaMin;
            Vector2 areaMax = firstBoss.ProjectileSpawnAreaMax;
            Vector2 from = new Vector2(
                Random.Range(areaMin.x, areaMax.x),
                Random.Range(areaMin.y, areaMax.y)
            );
            Vector2 dir = ((Vector2)Boss.PlayerTarget.position - from).normalized;

            // 깜빡이는 화살표 예고
            yield return ShowWarningArrow(
                from,
                from + dir * settings.warningLineLength,
                settings.warningDuration
            );

            // 발사 + 예고선 제거
            HideWarningLine();
            firstBoss.Pool.Spawn(from, dir);
        }

        isExecutingPattern = false;
    }

    // 화살표 모양 LineRenderer 표시 + 알파 PingPong 깜빡임
    private IEnumerator ShowWarningArrow(Vector2 start, Vector2 end, float duration)
    {
        var lr = firstBoss.WarningLine;
        if (lr == null)
        {
            yield return new WaitForSeconds(duration);
            yield break;
        }

        // 화살표 5점 구성: start → tip → wing1 → tip → wing2
        // (tip 경유 overdraw로 V자 화살촉을 단일 LineRenderer로 표현)
        Vector2 dir = end - start;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        dir.Normalize();
        Vector2 perp = new Vector2(-dir.y, dir.x);
        float h = settings.warningArrowHeadSize;
        Vector2 wing1 = end - dir * h + perp * h * 0.6f;
        Vector2 wing2 = end - dir * h - perp * h * 0.6f;

        lr.positionCount = 5;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.SetPosition(2, wing1);
        lr.SetPosition(3, end);
        lr.SetPosition(4, wing2);
        lr.gameObject.SetActive(true);

        // 깜빡임: 알파를 PingPong으로 0.25~1.0 사이 진동
        Color baseStart = lr.startColor;
        Color baseEnd = lr.endColor;
        float elapsed = 0f;
        float blinkInterval = Mathf.Max(0.01f, settings.warningBlinkInterval);
        while (elapsed < duration)
        {
            float t = Mathf.PingPong(elapsed / blinkInterval, 1f);
            float alpha = Mathf.Lerp(0.25f, 1f, t);
            Color cs = baseStart; cs.a = alpha;
            Color ce = baseEnd;   ce.a = alpha;
            lr.startColor = cs;
            lr.endColor   = ce;
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 알파 복원 (다음 사용에 영향 없도록)
        baseStart.a = 1f; baseEnd.a = 1f;
        lr.startColor = baseStart;
        lr.endColor   = baseEnd;
    }

    private void HideWarningLine()
    {
        if (firstBoss.WarningLine != null)
            firstBoss.WarningLine.gameObject.SetActive(false);
    }
}
