using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [SerializeField] private Health             playerHealth;
    [SerializeField] private PlayerInputHandler playerInput;
    [SerializeField] private Animator           playerAnimator;
    [SerializeField] private float              deathAnimDuration = 2f;

    private static readonly int DiedHash = Animator.StringToHash("Died");

    private void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnDeath -= HandleDeath;
    }

    private void HandleDeath()
    {
        if (playerInput != null)
            playerInput.enabled = false;

        if (playerAnimator != null)
            playerAnimator.SetTrigger(DiedHash);

        StartCoroutine(RespawnAfterDeath());
    }

    private IEnumerator RespawnAfterDeath()
    {
        yield return new WaitForSeconds(deathAnimDuration);

        var gs = GameState.Instance;
        string targetScene;

        if (gs != null && !string.IsNullOrEmpty(gs.lastCheckpointID))
        {
            // 체크포인트 있음 → 해당 씬으로 이동, 풀HP 부활
            gs.savedHP   = -1;
            gs.savedForm = PlayerForm.Human;
            if (playerHealth != null)
                gs.savedMaxHP = playerHealth.MaxHp;
            gs.MarkCheckpointRespawn();
            targetScene = gs.lastCheckpointScene;
        }
        else
        {
            // 체크포인트 없음(튜토리얼) → 현재 씬 재시작
            // Health.Awake()가 MaxHp로 자동 초기화하므로 별도 HP 처리 불필요
            targetScene = SceneManager.GetActiveScene().name;
        }

        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.TransitionTo(targetScene);
        else
            SceneManager.LoadScene(targetScene);
    }
}
