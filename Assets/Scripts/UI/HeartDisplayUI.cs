using UnityEngine;

/// <summary>
/// HpDisplay 오브젝트에 부착.
/// SetHearts(count) 호출 시 heartIconPrefab을 count만큼 자식으로 생성한다.
/// </summary>
public class HeartDisplayUI : MonoBehaviour
{
    [SerializeField] private GameObject heartIconPrefab;

    public void SetHearts(int count)
    {
        // 기존 하트 전부 제거
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        // count만큼 새로 생성
        for (int i = 0; i < count; i++)
            Instantiate(heartIconPrefab, transform);
    }
}
