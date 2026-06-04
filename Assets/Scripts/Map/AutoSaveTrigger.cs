using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class AutoSaveTrigger : MonoBehaviour
{
    [SerializeField] private string checkpointID = "checkpoint_map1_hub";

    private bool _triggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || _triggered) return;
        _triggered = true;

        var gs = GameState.Instance;
        if (gs == null) return;

        gs.SaveCheckpoint(checkpointID, SceneManager.GetActiveScene().name, transform.position);

        if (gs.currentSaveSlot >= 0)
            SaveManager.Save(gs.currentSaveSlot);
    }
}
