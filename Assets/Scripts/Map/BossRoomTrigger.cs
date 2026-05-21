using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BossRoomTrigger : MonoBehaviour
{
    [SerializeField] private Collider2D door;
    [SerializeField] private BossBase boss;

    private Collider2D entryTrigger;

    private void Awake() => entryTrigger = GetComponent<Collider2D>();

    private void Start()
    {
        if (boss != null)
            boss.Health.OnDeath += OpenDoor;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        door.enabled = true;
        entryTrigger.enabled = false;
        boss?.Activate();
    }

    private void OpenDoor() => door.enabled = false;

    private void OnDestroy()
    {
        if (boss != null)
            boss.Health.OnDeath -= OpenDoor;
    }
}
