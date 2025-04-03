using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class ChessNetworkManager : NetworkBehaviour
{
    public static ChessNetworkManager Instance { get; private set; }
    
    private NetworkVariable<bool> isGameStarted = new NetworkVariable<bool>(false);
    private NetworkVariable<int> currentPlayerTurn = new NetworkVariable<int>(0); // 0 = white, 1 = black
    
    // Track players
    private NetworkVariable<int> playersConnected = new NetworkVariable<int>(0);
    private bool isWhitePlayer = false;
    
    // Game state
    private NetworkVariable<bool> isGameOver = new NetworkVariable<bool>(false);
    private NetworkVariable<string> gameResult = new NetworkVariable<string>("");
    
    // Performance metrics
    private NetworkVariable<float> averageLatency = new NetworkVariable<float>(0);
    private float lastPingTime;
    private const float PING_INTERVAL = 5f;

    // Network settings
    [SerializeField] private string ipAddress = "127.0.0.1";
    [SerializeField] private ushort port = 7777;
    private UnityTransport transport;

    public bool IsGameStarted => isGameStarted.Value;
    public bool IsWhitePlayer => isWhitePlayer;
    public bool IsGameOver => isGameOver.Value;
    public string GameResult => gameResult.Value;
    public int CurrentPlayerTurn => currentPlayerTurn.Value;
    public float AverageLatency => averageLatency.Value;

    [SerializeField] private NetworkPrefabsList chessPieces;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Ensure we don't have NetworkManager component on this GameObject
        if (TryGetComponent<NetworkManager>(out _))
        {
            Debug.LogError("ChessNetworkManager cannot be on the same GameObject as NetworkManager");
            Destroy(this);
        }
    }

    private void Start()
    {
        transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        if (chessPieces == null)
        {
            chessPieces = Resources.Load<NetworkPrefabsList>("Teemu/ChessPiecesPrefabList");

            Debug.LogError("Failed to load NetworkPrefabsList!");
            return;
        }
        RegisterNetworkPrefabs();
        ConfigureSceneManager();
    }

    public void SetIPAddress(string ip)
    {
        ipAddress = ip;
    }

    public void StartHost()
    {
        transport.SetConnectionData(ipAddress, port);
        
        // Updated connection approval callback
        NetworkManager.Singleton.ConnectionApprovalCallback += (request, response) => 
        {
            // Only allow 2 players
            if (playersConnected.Value >= 2)
            {
                response.Approved = false;
                response.Reason = "Game is full";
                return;
            }
            
            // Approve connection
            response.Approved = true;
            response.CreatePlayerObject = false;
            
            // Assign player color
            bool isWhite = playersConnected.Value == 0;
            SetPlayerColorClientRpc(isWhite, new ClientRpcParams 
            { 
                Send = new ClientRpcSendParams { TargetClientIds = new[] { request.ClientNetworkId } } 
            });
            
            playersConnected.Value++;
            
            if (playersConnected.Value == 2)
            {
                StartGameClientRpc();
            }
        };
        
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        
        NetworkManager.Singleton.StartHost();
    }

    public void StartClient()
    {
        transport.SetConnectionData(ipAddress, port);
        
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        
        NetworkManager.Singleton.StartClient();
    }

    [ClientRpc]
    private void SetPlayerColorClientRpc(bool isWhite, ClientRpcParams rpcParams = default)
    {
        isWhitePlayer = isWhite;
        Debug.Log($"You are playing as {(isWhite ? "WHITE" : "BLACK")}");
    }

    [ClientRpc]
    private void StartGameClientRpc()
    {
        isGameStarted.Value = true;
        Debug.Log("Game started! White moves first.");
        
        if (IsHost)
        {
            currentPlayerTurn.Value = 0; // White starts
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client connected: {clientId}");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Client disconnected: {clientId}");
        if (IsServer && isGameStarted.Value && !isGameOver.Value)
        {
            // Handle player disconnect during game
            gameResult.Value = isWhitePlayer ? "Black wins by disconnect" : "White wins by disconnect";
            isGameOver.Value = true;
            EndGameClientRpc();
        }
    }

    [ClientRpc]
    private void EndGameClientRpc()
    {
        isGameOver.Value = true;
        Debug.Log($"Game over! Result: {gameResult.Value}");
    }

    private void Update()
    {
        if (IsServer && Time.time - lastPingTime > PING_INTERVAL)
        {
            UpdateLatencyClientRpc();
            lastPingTime = Time.time;
        }
    }

    [ClientRpc]
    private void UpdateLatencyClientRpc()
    {
        if (!IsServer) // Clients respond to server
        {
            RespondLatencyServerRpc(NetworkManager.Singleton.LocalTime.Time);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RespondLatencyServerRpc(double clientTime)
    {
        double roundTripTime = NetworkManager.Singleton.LocalTime.Time - clientTime;
        averageLatency.Value = (float)(roundTripTime * 1000); // Convert to milliseconds
        Debug.Log($"Average latency: {averageLatency.Value}ms");
    }

    // Method to attempt a move
    public void TryMakeMove(Vector2Int from, Vector2Int to)
    {
        if (!isGameStarted.Value || isGameOver.Value) return;
        
        // Check if it's this player's turn
        bool isMyTurn = (currentPlayerTurn.Value == 0 && isWhitePlayer) || 
                        (currentPlayerTurn.Value == 1 && !isWhitePlayer);
        
        if (!isMyTurn)
        {
            Debug.Log("Not your turn!");
            return;
        }
        
        // Validate move on server
        RequestMoveServerRpc(from.x, from.y, to.x, to.y);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestMoveServerRpc(int fromX, int fromY, int toX, int toY, ServerRpcParams rpcParams = default)
    {
        // Here you would validate the move according to chess rules
        // For now we'll assume it's valid
        
        // Update board state
        UpdateBoardStateClientRpc(fromX, fromY, toX, toY);
        
        // Switch turns
        currentPlayerTurn.Value = currentPlayerTurn.Value == 0 ? 1 : 0;
        
        // Notify players
        NotifyTurnChangeClientRpc(currentPlayerTurn.Value);
    }

    [ClientRpc]
    private void UpdateBoardStateClientRpc(int fromX, int fromY, int toX, int toY)
    {
        // Here you would update your local board representation
        // This would call your existing chess game logic
        Debug.Log($"Moving piece from ({fromX},{fromY}) to ({toX},{toY})");
        
        // In your actual implementation, you would call something like:
        // ChessGame.Instance.MovePiece(new Vector2Int(fromX, fromY), new Vector2Int(toX, toY));
    }

    [ClientRpc]
    private void NotifyTurnChangeClientRpc(int newTurn)
    {
        currentPlayerTurn.Value = newTurn;
        Debug.Log($"It's now {(newTurn == 0 ? "WHITE" : "BLACK")}'s turn");
        
        // You might want to update UI here to show whose turn it is
    }

    // Add methods for game end conditions
    public void CheckGameEndConditions()
    {
        if (!IsServer) return;
        
        // Here you would check for checkmate, stalemate, etc.
        // This would integrate with your existing chess logic
        
        // For example:
        /*
        if (ChessGame.Instance.IsCheckmate(true)) // White is checkmated
        {
            gameResult.Value = "Black wins by checkmate";
            isGameOver.Value = true;
            EndGameClientRpc();
        }
        else if (ChessGame.Instance.IsStalemate())
        {
            gameResult.Value = "Draw by stalemate";
            isGameOver.Value = true;
            EndGameClientRpc();
        }
        */
    }

    public void Resign()
    {
        if (isGameOver.Value) return;
        
        RequestResignServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestResignServerRpc(ServerRpcParams rpcParams = default)
    {
        gameResult.Value = isWhitePlayer ? "Black wins by resignation" : "White wins by resignation";
        isGameOver.Value = true;
        EndGameClientRpc();
    }

    private void ConfigureSceneManager()
    {
        // Correct way to disable validation warnings in Netcode 1.5.2
        NetworkManager.Singleton.SceneManager.DisableValidationWarnings(true);
        
        // Set client synchronization mode
        NetworkManager.Singleton.SceneManager.SetClientSynchronizationMode(LoadSceneMode.Additive);
        
        // Optional verification override
        NetworkManager.Singleton.SceneManager.VerifySceneBeforeLoading = (scene, scenesLoaded, sceneEventProgress) => true;
    }

    private void RegisterNetworkPrefabs()
    {
        // Create new NetworkPrefabs instance
        var newPrefabs = new NetworkPrefabs();
        
        // Track registered prefab names to prevent duplicates
        var registeredPrefabs = new HashSet<string>();

        foreach (var networkPrefab in chessPieces.PrefabList)
        {
            if (networkPrefab.Prefab == null)
            {
                Debug.LogWarning("Null prefab in NetworkPrefabsList");
                continue;
            }

            var networkObject = networkPrefab.Prefab.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                Debug.LogWarning($"Prefab {networkPrefab.Prefab.name} missing NetworkObject");
                continue;
            }

            // Check for duplicate prefab names
            if (registeredPrefabs.Contains(networkPrefab.Prefab.name))
            {
                Debug.LogWarning($"Duplicate prefab name detected: {networkPrefab.Prefab.name}");
                continue;
            }

            newPrefabs.Add(networkPrefab);
            registeredPrefabs.Add(networkPrefab.Prefab.name);
            Debug.Log($"Registered prefab: {networkPrefab.Prefab.name}");
        }

        NetworkManager.Singleton.NetworkConfig.Prefabs = newPrefabs;
    }
}