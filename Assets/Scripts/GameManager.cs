using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI Elements")]
    public Text player1ScoreText;
    public Text player2ScoreText;
    public Text gameOverText;

    [Header("Game Settings")]
    public int scoreToWin = 5;
    public float gameOverDelay = 2f;

    private int player1Score = 0;
    private int player2Score = 0;
    private bool gameOver = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        UpdateScoreUI();
        if (gameOverText != null)
            gameOverText.gameObject.SetActive(false);
    }

    public void PlayerHit(int playerNumber)
    {
        if (gameOver) return;

        // Award point to the other player
        if (playerNumber == 1)
        {
            player2Score++;
        }
        else
        {
            player1Score++;
        }

        UpdateScoreUI();
        CheckForWin();
    }

    void UpdateScoreUI()
    {
        if (player1ScoreText != null)
            player1ScoreText.text = "Player 1: " + player1Score;

        if (player2ScoreText != null)
            player2ScoreText.text = "Player 2: " + player2Score;
    }

    void CheckForWin()
    {
        if (player1Score >= scoreToWin)
        {
            EndGame("Player 1 Wins!");
        }
        else if (player2Score >= scoreToWin)
        {
            EndGame("Player 2 Wins!");
        }
    }

    void EndGame(string winnerText)
    {
        gameOver = true;

        if (gameOverText != null)
        {
            gameOverText.text = winnerText;
            gameOverText.gameObject.SetActive(true);
        }

        // Optionally restart after delay
        Invoke("RestartGame", gameOverDelay);
    }

    void RestartGame()
    {
        player1Score = 0;
        player2Score = 0;
        gameOver = false;

        UpdateScoreUI();

        if (gameOverText != null)
            gameOverText.gameObject.SetActive(false);
    }

    void Update()
    {
        // Quick restart with R key
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }
    }
}
