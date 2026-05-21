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
        Debug.Log("[Boss] Phase1State Enter");
    }

    public override void Exit()
    {
        if (currentPattern != null)
            Boss.StopCoroutine(currentPattern);
        Boss.IsDashing = false;
        Boss.Rb.linearVelocity = Vector2.zero;
        HideWarningLine();
    }

    public override void Update()
    {
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

        // 텔레그래프: 방향 락온 + 색 변경
        Vector2 dir = (Boss.PlayerTarget.position - Boss.transform.position).normalized;
        Boss.Sprite.color = new Color(1f, 0.6f, 0f); // 주황색 = 돌진 예고
        yield return new WaitForSeconds(settings.telegraphDuration);
        Boss.Sprite.color = Color.white;

        // 돌진
        Boss.HitWall = false;
        Boss.IsDashing = true;
        Boss.Rb.linearVelocity = dir * settings.dashSpeed;
        firstBoss.DashHitbox?.Activate();

        float elapsed = 0f;
        while (elapsed < settings.dashMaxDuration && !Boss.HitWall)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        firstBoss.DashHitbox?.Deactivate();
        Boss.Rb.linearVelocity = Vector2.zero;
        Boss.IsDashing = false;
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
