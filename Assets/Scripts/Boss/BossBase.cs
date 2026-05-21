using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(Health))]
public abstract class BossBase : MonoBehaviour
{
    [Header("Drop")]
    [SerializeField] private GameObject dropItemPrefab;

    [Header("Dash")]
    [SerializeField] private int dashContactDamage = 1;

    public Rigidbody2D Rb { get; private set; }
    public SpriteRenderer Sprite { get; private set; }
    public Health Health { get; private set; }
    public Transform PlayerTarget { get; private set; }
    public bool HitWall { get; set; }
    public bool IsDashing { get; set; }

    protected BossStateMachine Fsm { get; private set; }
    protected abstract BossStateBase DeathState { get; }

    // 피격 무적 (짧은 색 플래시 + 데미지 차단)
    private bool hitInvincible;
    // 페이즈 전환 무적 (연출 없이 순수 데미지 차단)
    private bool phaseInvincible;

    protected virtual void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        Sprite = GetComponent<SpriteRenderer>();
        Health = GetComponent<Health>();
        Fsm = new BossStateMachine();
    }

    protected virtual void Start()
    {
        var player = GameObject.FindWithTag("Player");
        if (player != null) PlayerTarget = player.transform;

        Health.OnHit += OnHit;
        Health.OnDeath += OnDeath;

        InitStates();
    }

    protected virtual void Update() => Fsm.Update();

    public void TransitionTo(BossStateBase next) => Fsm.ChangeState(next);

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsDashing && collision.gameObject.CompareTag("Player"))
            if (collision.gameObject.TryGetComponent<IDamageable>(out var target))
                target.TakeDamage(dashContactDamage);
        HitWall = true;
    }

    // 각 보스가 상태 초기화 및 첫 상태 진입을 담당
    protected abstract void InitStates();

    // 페이즈 전환 무적 (연출 없음)
    public void SetPhaseInvincible(bool value)
    {
        phaseInvincible = value;
        Health.IsInvincible = hitInvincible || phaseInvincible;
    }

    private void OnHit(int amount)
    {
        StartCoroutine(HitFlashRoutine());
    }

    // 피격 무적: 색 플래시 0.1s + 무적 0.4s
    private IEnumerator HitFlashRoutine()
    {
        hitInvincible = true;
        Health.IsInvincible = true;

        Sprite.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        Sprite.color = Color.white;
        yield return new WaitForSeconds(0.3f);

        hitInvincible = false;
        Health.IsInvincible = phaseInvincible;
    }

    private void OnDeath()
    {
        Health.OnHit -= OnHit;
        Health.OnDeath -= OnDeath;
        Fsm.ChangeState(DeathState);
    }

    public void DropItem()
    {
        if (dropItemPrefab != null)
            Instantiate(dropItemPrefab, transform.position, Quaternion.identity);
    }

    protected virtual void OnDestroy()
    {
        Health.OnHit -= OnHit;
        Health.OnDeath -= OnDeath;
    }
}
