using UnityEngine;

// Managers 오브젝트에 부착. 인스펙터에서 각 프리팹을 할당한다.
//   Small           — 방패 막기 (EliteEnemy 가드 차단)
//   Medium          — 플레이어 일반 공격 히트
//   Large           — 플레이어 피격 / 적 처치
//   Transform       — 플레이어 변신 (세 가지 폼 공통)
//   MultiplierText  — 배율 타격(×2 약점, ×3 기습) 시 떠오르는 텍스트
public class EffectSpawner : MonoBehaviour
{
    public static EffectSpawner Instance { get; private set; }

    [SerializeField] private GameObject smallEffectPrefab;
    [SerializeField] private GameObject mediumEffectPrefab;
    [SerializeField] private GameObject largeEffectPrefab;
    [SerializeField] private GameObject transformEffectPrefab;

    [Header("Multiplier Text")]
    [SerializeField] private GameObject multiplierTextPrefab;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SpawnSmall(Vector2 pos)     => Spawn(smallEffectPrefab, pos);
    public void SpawnMedium(Vector2 pos)    => Spawn(mediumEffectPrefab, pos);
    public void SpawnLarge(Vector2 pos)     => Spawn(largeEffectPrefab, pos);
    public void SpawnTransform(Vector2 pos) => Spawn(transformEffectPrefab, pos);

    /// <summary>
    /// 배율 피해 발생 시 히트 위치 위에 "×2", "×3" 등의 텍스트를 띄운다.
    /// </summary>
    public void SpawnMultiplierText(Vector2 pos, string text, Color color)
    {
        if (multiplierTextPrefab == null) return;
        var go = Instantiate(multiplierTextPrefab, (Vector3)pos + Vector3.up * 0.4f, Quaternion.identity);
        go.GetComponent<FloatingMultiplierText>()?.Setup(text, color);
    }

    private void Spawn(GameObject prefab, Vector2 pos)
    {
        if (prefab == null) return;
        Instantiate(prefab, pos, Quaternion.identity);
    }
}
