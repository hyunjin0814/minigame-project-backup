using UnityEngine;

public class CatStealth : MonoBehaviour
{
    private const float HideDelay = 0.1f;
    private const float MaxStealthMoveDistance = 2.25f;
    private const float StealthCooldown = 1.5f;

    public bool IsDetectable { get; private set; } = true;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private float stillTimer;
    private float accumulatedDistance;
    private float cooldownTimer;
    private bool prevDetectable = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        IsDetectable = true;
        prevDetectable = true;
        stillTimer = 0f;
        accumulatedDistance = 0f;
        cooldownTimer = 0f;
        SetAlpha(1f);
    }

    private void OnDisable()
    {
        IsDetectable = true;
        SetAlpha(1f);
    }

    private void SetAlpha(float a)
    {
        if (sr == null)
            return;
        Color c = sr.color;
        c.a = a;
        sr.color = c;
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        Vector2 v = rb.linearVelocity;
        bool isMoving = Mathf.Abs(v.x) > 0.1f || Mathf.Abs(v.y) > 0.1f;

        if (isMoving)
        {
            stillTimer = 0f;
            if (!IsDetectable)
            {
                accumulatedDistance += v.magnitude * Time.deltaTime;
                if (accumulatedDistance >= MaxStealthMoveDistance)
                    IsDetectable = true;
            }
        }
        else
        {
            if (IsDetectable && cooldownTimer <= 0f)
            {
                stillTimer += Time.deltaTime;
                if (stillTimer >= HideDelay)
                    IsDetectable = false;
            }
        }

        if (IsDetectable != prevDetectable)
        {
            if (!IsDetectable)
            {
                accumulatedDistance = 0f;
            }
            else
            {
                cooldownTimer = StealthCooldown;
                stillTimer = 0f;
            }
            SetAlpha(IsDetectable ? 1f : 0.3f);
            prevDetectable = IsDetectable;
        }
    }
}
