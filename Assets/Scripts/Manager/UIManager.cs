using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Singleton
    {
        get => _singleton;
        set
        {
            if (value == null)
            {
                _singleton = null;
            }
            else if(_singleton == null)
            {
                _singleton = value;
            }
            else if (_singleton != value)
            {
                Destroy(value);
                Debug.LogError($"There should only ever be one instance of {nameof(UIManager)}!");
            }
        }
    }

    private static UIManager _singleton;

    [SerializeField] private GameObject leaderBoard;
    [SerializeField] private TMP_Text gameStateText;
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private LeaderboardItem[] leaderboardItems;
    [SerializeField] private TMP_Text playerScoreText;
    [SerializeField] private TMP_Text timerText;
    
    private Player localPlayer;
    
    private void Awake()
    {
        Singleton = this;
    }

    private void OnDestroy()
    {
        if (Singleton == this)
        {
            Singleton = null;
        }
    }

    public void GetLocalPlayer(Player player)
    {
        localPlayer = player;
    }
    
    public void UpdatePlayerScore(int newScore)
    {
        if (playerScoreText != null)
        {
            playerScoreText.text = $"Your Score: {newScore}";
        }
        else
        {
            Debug.LogError("playerScoreText is null!");
        }
    }

    public void DidSetReady()
    {
        instructionText.text = "Waiting for other players to be ready";
    }

    public void SetUI(GameState newState, Player winner)
    {
        if (newState == GameState.Waiting)
        {
            if (winner == null)
            {
                gameStateText.text = "Waiting to Start";
                instructionText.text = "Press R when you are ready";
            }
            else
            {
                gameStateText.text = $"{winner.Name} Wins";
                instructionText.text = "Press R when you're Ready to play again";
            }
            
        }
        
        leaderBoard.SetActive(newState == GameState.Playing);
        playerScoreText.enabled = newState == GameState.Playing;
        timerText.enabled = newState == GameState.Playing;
        gameStateText.enabled = newState == GameState.Waiting;
        instructionText.enabled = newState == GameState.Waiting;
    }

    public void UpdateLeaderBoard(KeyValuePair<Fusion.PlayerRef, Player>[] players)
    {
        for (int i = 0; i < leaderboardItems.Length; i++)
        {
            if (i < players.Length)
            {
                leaderboardItems[i].leaderboardItem.SetActive(true);
                leaderboardItems[i].nameText.text = players[i].Value.Name ?? "Unknown Player";
                leaderboardItems[i].scoreText.text = players[i].Value.Score.ToString();
                
                if (players[i].Value == localPlayer)
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
    
    public void UpdateTimer(float timeLeft)
    {
        // Display the timer in MM:SS format
        string formattedTime = $"{Mathf.FloorToInt(timeLeft / 60):00}:{Mathf.FloorToInt(timeLeft % 60):00}";
        timerText.text = formattedTime; // Assuming you have a Text or TMP_Text called timerText
    }
    
    [Serializable]
    private struct LeaderboardItem
    {
        public GameObject leaderboardItem;
        public TMP_Text nameText;
        public TMP_Text scoreText;
    }
}
