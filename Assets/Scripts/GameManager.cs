using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Settings")]
    public float gameOverDelay = 2f;
    public bool autoRestart = true;

    // Game state
    private bool gameOver = false;
    private bool gameActive = true;

    // Events
    public event Action<string> OnGameOver;     // (winnerText)
    public event Action OnGameRestart;          // for UI reset

    void Awake()
    {
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

    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            SetGameActive(!gameActive);
        }
    }

    public void EndGame(string winnerText)
    {
        gameOver = true;
        gameActive = false;

        Debug.Log($"Game Over: {winnerText}");

        OnGameOver?.Invoke(winnerText);

        if (autoRestart)
        {
            Invoke(nameof(RestartGame), gameOverDelay);
        }
    }

    public void RestartGame()
    {
        CancelInvoke(nameof(RestartGame));

        gameOver = false;
        gameActive = true;

        Debug.Log("Game restarted!");

        OnGameRestart?.Invoke();
    }

    public void SetGameActive(bool active)
    {
        gameActive = active;
        Debug.Log($"Game {(active ? "resumed" : "paused")}");
    }

    public bool IsGameOver() => gameOver;
    public bool IsGameActive() => gameActive;

    void OnDestroy()
    {
        CancelInvoke();
    }
}
