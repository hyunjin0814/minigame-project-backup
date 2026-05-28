using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class AttackHitbox : MonoBehaviour
{
    [SerializeField] private int damage = 1;

    /// <summary>디버그/치트용 공격력 외부 조정.</summary>
    public int Damage { get => damage; set => damage = value; }

    [Header("Debug Visual")]
    [SerializeField] private bool showDebugVisual = false;
    [SerializeField] private Color debugColor = new Color(1f, 0.1f, 0.1f, 0.4f);

    public event Action<IDamageable, Collider2D> OnHit;

    private Collider2D col;
    private SpriteRenderer debugRenderer;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;

        if (showDebugVisual)
            SetupDebugVisual();

        Deactivate();
    }

    public void Activate()
    {
        col.enabled = true;
        if (debugRenderer != null) debugRenderer.enabled = true;
    }

    public void Deactivate()
    {
        col.enabled = false;
        if (debugRenderer != null) debugRenderer.enabled = false;
    }

    private void SetupDebugVisual()
    {
        var go = new GameObject("HitboxVisual");
        go.transform.SetParent(transform, false);

        debugRenderer = go.AddComponent<SpriteRenderer>();
        debugRenderer.color = debugColor;
        debugRenderer.sortingOrder = 999;

        if (col is BoxCollider2D box)
        {
            debugRenderer.sprite = CreateBoxSprite();
            go.transform.localPosition = box.offset;
            go.transform.localScale = new Vector3(box.size.x, box.size.y, 1f);
        }
        else if (col is CircleCollider2D circle)
        {
            debugRenderer.sprite = CreateCircleSprite();
            go.transform.localPosition = circle.offset;
            float diameter = circle.radius * 2f;
            go.transform.localScale = new Vector3(diameter, diameter, 1f);
        }
    }

    private static Sprite CreateBoxSprite()
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    private static Sprite CreateCircleSprite(int size = 64)
    {
        var tex = new Texture2D(size, size);
        float center = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));
                tex.SetPixel(x, y, dist <= center ? Color.white : Color.clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    public void SetBox(Vector2 offset, Vector2 size)
    {
        if (col is BoxCollider2D box)
        {
            box.offset = offset;
            box.size = size;

            if (debugRenderer != null)
            {
                debugRenderer.transform.localPosition = offset;
                debugRenderer.transform.localScale = new Vector3(size.x, size.y, 1f);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<IDamageable>(out var target))
        {
            target.TakeDamage(damage, transform.position);
            OnHit?.Invoke(target, other);
        }
    }
}
