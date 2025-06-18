using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Game Over UI")]
    public Text gameOverText;
    public GameObject gameOverPanel; // Optional: if you have a panel that contains game over UI

    [Header("Other UI Elements")]
    public GameObject pauseMenu; // Optional: for future pause functionality
    public Text countdownText; // Optional: for round countdown

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }


    public void ShowGameOver(string winnerText)
    {
        if (gameOverText != null)
            gameOverText.text = winnerText;

        SetGameOverUIActive(true);
    }

    public void HideGameOver()
    {
        SetGameOverUIActive(false);
    }

    private void SetGameOverUIActive(bool active)
    {
        if (gameOverText != null)
            gameOverText.gameObject.SetActive(active);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(active);
    }
}
