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
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
