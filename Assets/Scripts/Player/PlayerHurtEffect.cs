using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class PlayerHurtEffect : MonoBehaviour
{
    [SerializeField] private float flickerInterval = 0.1f;
    [SerializeField] private float flickerDuration = 1f;

    private SpriteRenderer spriteRenderer;
    private Health health;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        health = GetComponent<Health>();
    }

    private void OnEnable() => health.OnHit += HandleHit;
    private void OnDisable() => health.OnHit -= HandleHit;

    private void HandleHit(int amount, Vector2 source)
    {
        StopAllCoroutines();
        StartCoroutine(FlickerRoutine());
    }

    private IEnumerator FlickerRoutine()
    {
        float elapsed = 0f;
        while (elapsed < flickerDuration)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(flickerInterval);
            elapsed += flickerInterval;
        }
        spriteRenderer.enabled = true;
    }
}
