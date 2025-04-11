using Unity.Netcode;
using UnityChess;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Analytics;
using Firebase.Analytics;
using System.Collections.Generic;

public class NetworkGameController : NetworkBehaviour
{
    public static NetworkGameController Instance { get; private set; }

    private NetworkVariable<Side> currentTurn = new NetworkVariable<Side>(Side.White);
    private NetworkVariable<bool> isGameActive = new NetworkVariable<bool>(false);
    public float lastPingTime;
    bool hasTestedRpc = false;

    [SerializeField]
    public Text latencyText = null;

    [SerializeField]
    public Text matchResultText = null;

    public Side CurrentTurn => currentTurn.Value;

    // Test network connection
    [ClientRpc]
    public void TestConnectionClientRpc()
    {
    }

    // Setup network instance and initial game state
    public override void OnNetworkSpawn()
    {
        Instance = this;
        currentTurn.OnValueChanged += (oldValue, newValue) =>
        {
            BoardManager.Instance.EnsureOnlyPiecesOfSideAreEnabled(newValue);
        };
        if (IsServer)
        {
            isGameActive.Value = true;
            GameManager.Instance.StartNewGame();
            SyncBoardClientRpc(GameManager.Instance.SerializeGame());
            LogMatchStart();
        }
        else
        {
            BoardManager.Instance.ClearBoard();
        }
    }

    // Clean up instance on despawn
    public override void OnNetworkDespawn()
    {
        Instance = null;
    }

    // Handle move requests from clients
    [ServerRpc(RequireOwnership = false)]
    public void RequestMoveServerRpc(ForceNetworkSerializeByMemcpy<NetworkSquare> start, ForceNetworkSerializeByMemcpy<NetworkSquare> end, ulong clientId)
    {
        if (!isGameActive.Value)
            return;

        Side playerSide = clientId == NetworkManager.ServerClientId ? Side.White : Side.Black;
        if (playerSide != currentTurn.Value)
            return;

        Square startSquare = start.Value.ToSquare();
        Square endSquare = end.Value.ToSquare();
        Movement move = new Movement(startSquare, endSquare);
        if (GameManager.Instance.TryExecuteMove(move))
        {
            currentTurn.Value = currentTurn.Value == Side.White ? Side.Black : Side.White;
            SyncBoardClientRpc(GameManager.Instance.SerializeGame());

            if (GameManager.Instance.HalfMoveTimeline.TryGetCurrent(out HalfMove latestHalfMove))
            {
                if (latestHalfMove.CausedCheckmate || latestHalfMove.CausedStalemate)
                {
                    isGameActive.Value = false;
                    Side winner = latestHalfMove.CausedCheckmate ? currentTurn.Value.Complement() : Side.None;
                    if (NetworkObject.IsSpawned)
                    {
                        EndGameClientRpc(winner);
                    }
                    else
                    {
                        EndGameFallback(winner);
                    }
                }
            }
        }
    }

    // Process player resignation
    [ServerRpc(RequireOwnership = false)]
    public void ResignServerRpc(ulong clientId)
    {
        if (!isGameActive.Value) return;

        isGameActive.Value = false;
        Side winner = clientId == NetworkManager.ServerClientId ? Side.Black : Side.White;
        if (NetworkObject.IsSpawned)
        {
            EndGameClientRpc(winner);
        }
        else
        {
            EndGameFallback(winner);
        }
    }

    // Notify clients of game end
    [ClientRpc]
    private void EndGameClientRpc(Side winner)
    {
        string resultText = winner == Side.None ? "Stalemate!" : $"{winner} wins!";
        UIManager.Instance.resultText.text = resultText;
        UIManager.Instance.resultText.gameObject.SetActive(true);
        BoardManager.Instance.SetActiveAllPieces(false);

        if (matchResultText != null) matchResultText.text = resultText;

        if (IsServer) LogMatchEnd(winner);
    }

    // Fallback for game end if RPC fails
    private void EndGameFallback(Side winner)
    {
        string resultText = winner == Side.None ? "Stalemate!" : $"{winner} wins!";
        UIManager.Instance.resultText.text = resultText;
        UIManager.Instance.resultText.gameObject.SetActive(true);
        BoardManager.Instance.SetActiveAllPieces(false);

        if (matchResultText != null) matchResultText.text = resultText;
    }

    // Synchronize board state across clients
    [ClientRpc]
    public void SyncBoardClientRpc(string fen)
    {
        BoardManager.Instance.ClearBoard();

        if (!IsServer)
        {
            GameManager.Instance.LoadGame(fen);
        }

        foreach ((Square square, Piece piece) in GameManager.Instance.CurrentPieces)
        {
            BoardManager.Instance.CreateAndPlacePieceGO(piece, square);
            GameObject pieceGO = BoardManager.Instance.GetPieceGOAtPosition(square);
            if (pieceGO != null)
            {
                NetworkObject netObj = pieceGO.GetComponent<NetworkObject>();
                if (netObj != null) Destroy(netObj);
            }
        }

        BoardManager.Instance.EnsureOnlyPiecesOfSideAreEnabled(currentTurn.Value);
    }

    // Handle ping request from client
    [ServerRpc(RequireOwnership = false)]
    public void PingServerRpc(ulong clientId, float clientTime)
    {
        PongClientRpc(clientId, Time.time);
    }

    // Respond to client with latency info
    [ClientRpc]
    private void PongClientRpc(ulong clientId, float serverTime)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            float latency = (Time.time - lastPingTime) * 1000;
            if (latencyText != null) latencyText.text = $"Ping: {latency} ms";
        }
    }

    // Log match start event
    private void LogMatchStart()
    {
        string matchId = System.Guid.NewGuid().ToString();
        Analytics.CustomEvent("MatchStarted", new Dictionary<string, object>
        {
            { "MatchID", matchId },
            { "Timestamp", System.DateTime.UtcNow.ToString("o") }
        });
        FirebaseAnalytics.LogEvent("MatchStarted", new Parameter[]
        {
            new Parameter("MatchID", matchId),
            new Parameter("Timestamp", System.DateTime.UtcNow.ToString("o"))
        });
    }

    // Log match end event
    private void LogMatchEnd(Side winner)
    {
        string matchId = System.Guid.NewGuid().ToString();
        Analytics.CustomEvent("MatchEnded", new Dictionary<string, object>
        {
            { "MatchID", matchId },
            { "Winner", winner.ToString() },
            { "Timestamp", System.DateTime.UtcNow.ToString("o") }
        });
        FirebaseAnalytics.LogEvent("MatchEnded", new Parameter[]
        {
            new Parameter("MatchID", matchId),
            new Parameter("Winner", winner.ToString()),
            new Parameter("Timestamp", System.DateTime.UtcNow.ToString("o"))
        });
    }

    // Update ping and test connection periodically
    private void Update()
    {
        if (IsClient && Time.time - lastPingTime > 1f)
        {
            lastPingTime = Time.time;
            PingServerRpc(NetworkManager.Singleton.LocalClientId, lastPingTime);
        }

        if (IsServer && Time.time > 5f && !hasTestedRpc)
        {
            TestConnectionClientRpc();
            hasTestedRpc = true;
        }
    }
}