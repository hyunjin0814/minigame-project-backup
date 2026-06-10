using UnityEngine.Pool;
using UnityEngine;

public class ProjectilePool : MonoBehaviour
{
    [SerializeField] private BossProjectile prefab;
    [SerializeField] private int defaultCapacity  = 10;
    [SerializeField] private int maxSize = 20;   
    [SerializeField] private float projectileSpeed = 10f;

    private IObjectPool<BossProjectile> objectPool;

    private void Awake()
    {
        objectPool = new ObjectPool<BossProjectile>(
            CreateProjectile,
            OnGetFromPool,
            OnReleaseToPool,
            OnDestroyPooledObject,
            collectionCheck: true,          // 에디터에서 이중 반환 시 예외 발생
            defaultCapacity: defaultCapacity,
            maxSize: maxSize
        );
    }
    

    public void Spawn(Vector2 position, Vector2 dir)
    {
        var proj = objectPool.Get();
        proj.transform.position = position;
        proj.Init(dir, projectileSpeed, objectPool);
    }

    public void Return(BossProjectile proj)
    {
        objectPool.Release(proj);
    }

     private BossProjectile CreateProjectile()
    {
        // 기존 CreateNew()와 동일: 월드 루트 생성, 비활성 상태로 반환
        var proj = Instantiate(prefab);
        proj.gameObject.SetActive(false);
        return proj;
    }

    private void OnGetFromPool(BossProjectile proj)
    {
        proj.gameObject.SetActive(true);
    }

    private void OnReleaseToPool(BossProjectile proj)
    {
        proj.gameObject.SetActive(false);
    }

    private void OnDestroyPooledObject(BossProjectile proj)
    {
        Destroy(proj.gameObject);           // maxSize 초과 시 호출
    }

    // private BossProjectile CreateNew()
    // {
    //     // 보스(부모)는 FacePlayer로 localScale.x를 ±1 반전시킨다.
    //     // 투사체를 보스 자식으로 두면 좌향(scale.x=-1) 발사 시 스프라이트가
    //     // 거울 반전되어 진행 방향의 반대를 바라본다(velocity는 월드라 정상 이동).
    //     // → flip 영향을 받지 않도록 월드 루트에 생성한다.
    //     var proj = Instantiate(prefab);
    //     proj.gameObject.SetActive(false);
    //     return proj;
    // }
}
