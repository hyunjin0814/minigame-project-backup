using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [SerializeField] private Health playerHealth;
    [SerializeField] private SpriteRenderer playerSprite;
    [SerializeField] private PlayerInputHandler playerInput;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private float restartDelay = 2f;

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
        if (playerSprite != null)
            playerSprite.enabled = false;
        if (playerInput != null)
            playerInput.enabled = false;
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        StartCoroutine(RestartAfterDelay());
    }

    private IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(restartDelay);

        // 체크포인트가 저장된 경우 → 해당 씬으로 이동
        if (GameState.Instance != null
            && !string.IsNullOrEmpty(GameState.Instance.lastCheckpointScene))
        {
            // 게임 오버 후 체크포인트 복귀: 풀HP로 부활하도록 savedHP를 -1로 초기화
            // (PlayerSpawner가 최대 HP까지 회복시킨다).
            GameState.Instance.savedHP = -1;

            // 리스폰 시 인간 형태로 리셋
            GameState.Instance.savedForm = PlayerForm.Human;

            // 최대 HP 업그레이드는 사망 후에도 유지
            if (playerHealth != null)
                GameState.Instance.savedMaxHP = playerHealth.MaxHp;

            // 진입점 ID 대신 체크포인트 직접 좌표(spawnPosition)로 복원하도록 예약
            GameState.Instance.MarkCheckpointRespawn();

            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.TransitionTo(GameState.Instance.lastCheckpointScene);
            else
                SceneManager.LoadScene(GameState.Instance.lastCheckpointScene);
        }
        else
        {
            // 체크포인트 미저장 → 현재 씬 재시작 (기존 동작 유지)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
