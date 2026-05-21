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
        var proj = Instantiate(prefab, transform);
        proj.gameObject.SetActive(false);
        return proj;
    }
}
