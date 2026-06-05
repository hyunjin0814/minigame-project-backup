using UnityEngine;

// Managers 오브젝트에 부착. 인스펙터에서 Small/Medium/Large 프리팹을 할당한다.
//   Small  — 방패 막기 (EliteEnemy 가드 차단)
//   Medium — 플레이어 일반 공격 히트
//   Large  — 플레이어 피격 / 적 처치
public class EffectSpawner : MonoBehaviour
{
    public static EffectSpawner Instance { get; private set; }

    [SerializeField] private GameObject smallEffectPrefab;
    [SerializeField] private GameObject mediumEffectPrefab;
    [SerializeField] private GameObject largeEffectPrefab;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SpawnSmall(Vector2 pos)  => Spawn(smallEffectPrefab, pos);
    public void SpawnMedium(Vector2 pos) => Spawn(mediumEffectPrefab, pos);
    public void SpawnLarge(Vector2 pos)  => Spawn(largeEffectPrefab, pos);

    private void Spawn(GameObject prefab, Vector2 pos)
    {
        if (prefab == null) return;
        Instantiate(prefab, pos, Quaternion.identity);
    }
}
