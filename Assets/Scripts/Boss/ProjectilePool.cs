using System.Collections.Generic;
using UnityEngine;

public class ProjectilePool : MonoBehaviour
{
    [SerializeField] private BossProjectile prefab;
    [SerializeField] private int initialSize = 10;
    [SerializeField] private float projectileSpeed = 10f;

    private Queue<BossProjectile> pool;

    private void Awake()
    {
        pool = new Queue<BossProjectile>();
        for (int i = 0; i < initialSize; i++)
            pool.Enqueue(CreateNew());
    }

    public void Spawn(Vector2 position, Vector2 dir)
    {
        var proj = pool.Count > 0 ? pool.Dequeue() : CreateNew();
        proj.transform.position = position;
        proj.gameObject.SetActive(true);
        proj.Init(dir, projectileSpeed, this);
    }

    public void Return(BossProjectile proj)
    {
        proj.gameObject.SetActive(false);
        pool.Enqueue(proj);
    }

    private BossProjectile CreateNew()
    {
        // 보스(부모)는 FacePlayer로 localScale.x를 ±1 반전시킨다.
        // 투사체를 보스 자식으로 두면 좌향(scale.x=-1) 발사 시 스프라이트가
        // 거울 반전되어 진행 방향의 반대를 바라본다(velocity는 월드라 정상 이동).
        // → flip 영향을 받지 않도록 월드 루트에 생성한다.
        var proj = Instantiate(prefab);
        proj.gameObject.SetActive(false);
        return proj;
    }
}
