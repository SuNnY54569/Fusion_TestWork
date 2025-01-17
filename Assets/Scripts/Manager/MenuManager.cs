using System.Collections;
using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private NetworkRunner networkRunnerPrefab;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button joinRoomButton;
    [SerializeField] private TMP_InputField setRoomNameInput;
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private string _gameSceneName = "Gameplay";
    
    private NetworkRunner runnerInstance;

    void Start()
    {
        createRoomButton.onClick.AddListener(OnCreateRoomClicked);
        joinRoomButton.onClick.AddListener(OnJoinRoomClicked);

        if (PlayerPrefs.HasKey("PlayerName"))
        {
            playerNameInput.text = PlayerPrefs.GetString("PlayerName");
        }
    }

    // Function to handle creating a room
    private void OnCreateRoomClicked()
    {
        string roomName = setRoomNameInput.text.Trim();
        string playerName = playerNameInput.text.Trim();
        
        if (string.IsNullOrEmpty(playerName))
        {
            statusText.text = "Please enter a player name.";
            return;
        }
        
        if (string.IsNullOrEmpty(roomName))
        {
            statusText.text = "Please enter a valid room name.";
            return;
        }
        
        PlayerPrefs.SetString("PlayerName", playerName);
        
        statusText.text = "Creating Room...";
        StartGame(GameMode.AutoHostOrClient, setRoomNameInput.text, _gameSceneName);
    }

    // Function to handle joining a room
    private void OnJoinRoomClicked()
    {
        string roomCode = roomNameInput.text.Trim();
        string playerName = playerNameInput.text.Trim();
        
        if (string.IsNullOrEmpty(playerName))
        {
            statusText.text = "Please enter a player name.";
            return;
        }
        
        if (string.IsNullOrEmpty(roomCode))
        {
            statusText.text = "Please enter a valid room code.";
            return;
        }
        
        PlayerPrefs.SetString("PlayerName", playerName);
        
        statusText.text = "Joining Room...";
        StartGame(GameMode.Client, roomNameInput.text, _gameSceneName);
    }
    
    private async void StartGame(GameMode mode, string roomName, string sceneName)
    {
        createRoomButton.interactable = false;
        joinRoomButton.interactable = false;
        
        runnerInstance = FindObjectOfType<NetworkRunner>();
        if (runnerInstance == null)
        {
            runnerInstance = Instantiate(networkRunnerPrefab);
        }
        
        runnerInstance.ProvideInput = true;

        var startGameArgs = new StartGameArgs()
        {
            GameMode = mode,
            SessionName = roomName,
        };

        // GameMode.Host = Start a session with a specific name
        // GameMode.Client = Join a session with a specific name
        var result = await runnerInstance.StartGame(startGameArgs);

        if (result.Ok)
        {
            if (runnerInstance.IsServer)
            {
                runnerInstance.LoadScene(sceneName);
            }
            statusText.text = mode == GameMode.Client ? "Joined Room!" : "Room Created!";
        }
        else
        {
            statusText.text = $"Failed to start: {result.ShutdownReason}";
        }
        
        createRoomButton.interactable = true;
        joinRoomButton.interactable = true;
    }
}
