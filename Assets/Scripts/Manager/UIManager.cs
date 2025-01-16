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

    [SerializeField] private TMP_Text gameStateText;
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private LeaderboardItem[] leaderboardItems;
    
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

    public void DidSetReady()
    {
        instructionText.text = "Waiting for other players to be ready";
    }

    public void SetWaitUI(GameState newState, Player winner)
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

        gameStateText.enabled = newState == GameState.Waiting;
        instructionText.enabled = newState == GameState.Waiting;
    }

    public void UpdateLeaderBoard(KeyValuePair<Fusion.PlayerRef, Player>[] players)
    {
        for (int i = 0; i < leaderboardItems.Length; i++)
        {
            if (i < players.Length)
            {
                leaderboardItems[i].nameText.text = players[i].Value.Name ?? "Unknown Player";
                leaderboardItems[i].scoreText.text = players[i].Value.Score.ToString();
            }
            else
            {
                leaderboardItems[i].nameText.text = "";
                leaderboardItems[i].scoreText.text = "";
            }
        }
    }
    
    [Serializable]
    private struct LeaderboardItem
    {
        public TMP_Text nameText;
        public TMP_Text scoreText;
    }
}
