using System.Collections;
using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    #region Variables
    
    [Header("Network Runner Settings")]
    [Tooltip("Prefab of the NetworkRunner instance used for connecting to rooms.")]
    [SerializeField] private NetworkRunner networkRunnerPrefab;

    [Header("UI Elements")]
    [Tooltip("Button to create a new room.")]
    [SerializeField] private Button createRoomButton;
    [Tooltip("Button to join an existing room.")]
    [SerializeField] private Button joinRoomButton;

    [Header("Input Fields")]
    [Tooltip("Input field for setting a new room name.")]
    [SerializeField] private TMP_InputField setRoomNameInput;
    [Tooltip("Input field for entering the room code when joining.")]
    [SerializeField] private TMP_InputField roomNameInput;
    [Tooltip("Input field for entering player name.")]
    [SerializeField] private TMP_InputField playerNameInput;

    [Header("Status Text")]
    [Tooltip("Text element to display status messages.")]
    [SerializeField] private TMP_Text statusText;

    [Header("Scene Settings")]
    [Tooltip("Name of the gameplay scene to load.")]
    [SerializeField] private string _gameSceneName = "Gameplay";
    
    private NetworkRunner runnerInstance;
    
    #endregion

    void Start()
    {
        createRoomButton.onClick.AddListener(OnCreateRoomClicked);
        joinRoomButton.onClick.AddListener(OnJoinRoomClicked);

        if (PlayerPrefs.HasKey("PlayerName"))
        {
            playerNameInput.text = PlayerPrefs.GetString("PlayerName");
        }
    }

    #region Button Handlers
    
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
    
    #endregion
    
    #region Game Start Logic
    
    // Function to start the game by either creating or joining a room
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
    
    #endregion
}
