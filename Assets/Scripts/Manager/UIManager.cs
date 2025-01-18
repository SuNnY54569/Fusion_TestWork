using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    #region Singleton

    public static UIManager Instance;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        inputManager = FindObjectOfType<InputManager>();
    }

    #endregion
    
    #region Variables
    
    [Header("UI Elements")]
    [Tooltip("The leaderboard UI object.")]
    [SerializeField] private GameObject leaderBoard;

    [Tooltip("Text UI element to display the current game state.")]
    [SerializeField] private TMP_Text gameStateText;

    [Tooltip("Text UI element to display instructions to the player.")]
    [SerializeField] private TMP_Text instructionText;

    [Tooltip("Text UI element to display the player's score.")]
    [SerializeField] private TMP_Text playerScoreText;

    [Tooltip("Text UI element to display the countdown timer.")]
    [SerializeField] private TMP_Text timerText;

    [Tooltip("Text UI element to display the countdown timer.")]
    [SerializeField] private TMP_Text countdownText;

    [Tooltip("Text UI element to display the player's health.")]
    [SerializeField] private TMP_Text playerHealthText;

    [Tooltip("Reference to the GameManager.")]
    [SerializeField] private GameManager gameManager;

    [Tooltip("The menu button that appears during the waiting state.")]
    [SerializeField] private GameObject menuButton;

    [Tooltip("Crosshair that will guide player to shoot the bullet")] 
    [SerializeField] private GameObject crosshair;

    [Tooltip("Array of leaderboard item UI elements.")]
    [SerializeField] private LeaderboardItem[] leaderboardItems;

    private InputManager inputManager;
    
    #endregion
    
    #region UI Updates
    
    //Update the player's score in the UI.
    public void UpdatePlayerScore(int newScore)
    {
        if (playerScoreText != null)
        {
            playerScoreText.text = $"Score: {newScore}";
        }
        else
        {
            Debug.LogError("playerScoreText is null!");
        }
    }
    
    // Update the player's health in the UI.
    public void UpdatePlayerHealth(int health)
    {
        if (playerHealthText != null)
        {
            playerHealthText.text = $"Health: {health}";
        }
        else
        {
            Debug.LogError("playerHealthText is null!");
        }
    }
    
    public void UpdateCountdown(float countdownTimer)
    {
        countdownText.text = Mathf.CeilToInt(countdownTimer).ToString();
    }
    
    public void UpdateTimer(float timeLeft)
    {
        string formattedTime = $"{Mathf.FloorToInt(timeLeft / 60):00}:{Mathf.FloorToInt(timeLeft % 60):00}";
        timerText.text = formattedTime;
    }

    // Update the UI when there are not enough players to start the game.
    public void NotEnoughPlayer(int minPlayersRequired)
    {
        gameStateText.text = "Waiting for more players";
        instructionText.text = $"at least {minPlayersRequired} players to start the game.";
    }

    // Update the UI when a player sets themselves as ready.
    public void DidSetReady()
    {
        instructionText.text = "Waiting for other players to be ready";
    }

    //Update the UI based on the current game state and winner.
    public void SetUI(GameState newState, Player winner)
    {
        menuButton.SetActive(newState == GameState.Waiting);
        leaderBoard.SetActive(newState == GameState.Playing);
        crosshair.SetActive(newState == GameState.Playing);
        playerHealthText.enabled = newState == GameState.Playing;
        playerScoreText.enabled = newState == GameState.Playing;
        timerText.enabled = newState == GameState.Playing;
        gameStateText.enabled = newState == GameState.Waiting;
        instructionText.enabled = newState == GameState.Waiting;
        countdownText.enabled = newState == GameState.Countdown;
        
        if (newState == GameState.Waiting)
        {
            if (winner == null)
            {
                gameStateText.text = "Waiting to Start";
                instructionText.text = $"Press R when you are ready";
            }
            else
            {
                gameStateText.text = $"{winner.Name} Wins";
                instructionText.text = $"Press R when you're Ready to play again";
            }
        }
    }

    //Update the leaderboard with the current players' scores.
    public void UpdateLeaderBoard(KeyValuePair<Fusion.PlayerRef, Player>[] players)
    {
        for (int i = 0; i < leaderboardItems.Length; i++)
        {
            if (i < players.Length)
            {
                leaderboardItems[i].leaderboardItem.SetActive(true);
                leaderboardItems[i].nameText.text = players[i].Value.Name ?? "Unknown Player";
                leaderboardItems[i].scoreText.text = players[i].Value.Score.ToString();
                
                if (players[i].Value == inputManager.LocalPlayer)
                {
                    leaderboardItems[i].nameText.color = Color.yellow;
                    leaderboardItems[i].scoreText.color = Color.yellow; // Highlight local player's score
                }
                else
                {
                    leaderboardItems[i].nameText.color = Color.white;
                    leaderboardItems[i].scoreText.color = Color.white;
                }
            }
            else
            {
                leaderboardItems[i].leaderboardItem.SetActive(false);
                leaderboardItems[i].nameText.text = "";
                leaderboardItems[i].scoreText.text = "";
            }
        }
    }
    
    #endregion
    
    #region Button Events
    
    public void OnExitButtonClick()
    {
        gameManager.ExitRoom();
    }
    
    #endregion
    
    #region Structs
    
    [Serializable]
    private struct LeaderboardItem
    {
        public GameObject leaderboardItem;
        public TMP_Text nameText;
        public TMP_Text scoreText;
    }
    
    #endregion
}
