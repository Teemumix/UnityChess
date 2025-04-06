using Unity.Netcode;
using UnityChess;
using UnityEngine;
using UnityEngine.UI;

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

    [ClientRpc]
    public void TestConnectionClientRpc()
    {
        Debug.Log("Client received test RPC!");
    }

    public override void OnNetworkSpawn()
    {
        Instance = this;
        Debug.Log($"NetworkGameController spawned. IsServer: {IsServer}, IsClient: {IsClient}, NetworkObject.IsSpawned: {NetworkObject.IsSpawned}");
        currentTurn.OnValueChanged += (oldValue, newValue) =>
        {
            Debug.Log($"currentTurn changed from {oldValue} to {newValue} on ClientId {NetworkManager.Singleton.LocalClientId}");
            BoardManager.Instance.EnsureOnlyPiecesOfSideAreEnabled(newValue);
        };
        if (IsServer)
        {
            isGameActive.Value = true;
            GameManager.Instance.StartNewGame();
            SyncBoardClientRpc(GameManager.Instance.SerializeGame());
        }
        else
        {
            BoardManager.Instance.ClearBoard();
        }
    }

    public override void OnNetworkDespawn()
    {
        Instance = null;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestMoveServerRpc(ForceNetworkSerializeByMemcpy<NetworkSquare> start, ForceNetworkSerializeByMemcpy<NetworkSquare> end, ulong clientId)
    {
        if (!isGameActive.Value)
        {
            Debug.Log("Game is not active!");
            return;
        }

        Side playerSide = clientId == NetworkManager.ServerClientId ? Side.White : Side.Black;
        Debug.Log($"RequestMoveServerRpc: ClientId {clientId}, PlayerSide {playerSide}, CurrentTurn {currentTurn.Value}");
        if (playerSide != currentTurn.Value)
        {
            Debug.Log("Not your turn!");
            return;
        }

        Square startSquare = start.Value.ToSquare();
        Square endSquare = end.Value.ToSquare();
        Debug.Log($"Attempting move from {startSquare.File},{startSquare.Rank} to {endSquare.File},{endSquare.Rank}");

        Movement move = new Movement(startSquare, endSquare);
        bool moveSuccess = GameManager.Instance.TryExecuteMove(move);
        Debug.Log($"TryExecuteMove result: {moveSuccess}");
        if (moveSuccess)
        {
            Side newTurn = currentTurn.Value == Side.White ? Side.Black : Side.White;
            currentTurn.Value = newTurn;
            Debug.Log($"Turn switched to {currentTurn.Value}");
            SyncBoardClientRpc(GameManager.Instance.SerializeGame());

            if (GameManager.Instance.HalfMoveTimeline.TryGetCurrent(out HalfMove latestHalfMove))
            {
                if (latestHalfMove.CausedCheckmate || latestHalfMove.CausedStalemate)
                {
                    isGameActive.Value = false;
                    Side winner = latestHalfMove.CausedCheckmate ? currentTurn.Value.Complement() : Side.None;
                    EndGameClientRpc(winner);
                }
            }
        }
        else
        {
            Debug.Log("Move failed: TryExecuteMove returned false.");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResignServerRpc(ulong clientId)
    {
        if (!isGameActive.Value) return;

        isGameActive.Value = false;
        Side winner = clientId == NetworkManager.ServerClientId ? Side.Black : Side.White;
        EndGameClientRpc(winner);
    }

    [ClientRpc]
    private void EndGameClientRpc(Side winner)
    {
        string resultText = winner == Side.None ? "Stalemate!" : $"{winner} wins!";
        Debug.Log($"Game ended. Winner: {winner}"); // Added log for winner
        UIManager.Instance.resultText.text = resultText;
        UIManager.Instance.resultText.gameObject.SetActive(true);
        BoardManager.Instance.SetActiveAllPieces(false);

        if (matchResultText != null) matchResultText.text = resultText;
    }

    [ClientRpc]
    private void SyncBoardClientRpc(string fen)
    {
        Debug.Log($"SyncBoardClientRpc called. IsServer: {IsServer}, FEN: {fen}");
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

    [ServerRpc(RequireOwnership = false)]
    public void PingServerRpc(ulong clientId, float clientTime)
    {
        PongClientRpc(clientId, Time.time);
    }

    [ClientRpc]
    private void PongClientRpc(ulong clientId, float serverTime)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            float latency = (Time.time - lastPingTime) * 1000;
            Debug.Log($"Ping: {latency} ms");
            if (latencyText != null) latencyText.text = $"Ping: {latency} ms";
        }
    }

    public bool IsMyTurn(ulong clientId)
    {
        Side playerSide = clientId == NetworkManager.ServerClientId ? Side.White : Side.Black;
        bool isMyTurn = playerSide == currentTurn.Value && isGameActive.Value;
        Debug.Log($"IsMyTurn: ClientId {clientId}, PlayerSide {playerSide}, CurrentTurn {currentTurn.Value}, Result {isMyTurn}");
        return isMyTurn;
    }

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
            Debug.Log("Server sent delayed TestConnectionClientRpc");
            hasTestedRpc = true;
        }
    }
}