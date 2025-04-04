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

    [SerializeField]
    public Text latencyText = null;

    [ClientRpc]
    public void TestConnectionClientRpc()
    {
        Debug.Log("Client received test RPC!");
    }

    public override void OnNetworkSpawn()
    {
        Instance = this;
        Debug.Log($"NetworkGameController spawned. IsServer: {IsServer}, IsClient: {IsClient}, NetworkObject.IsSpawned: {NetworkObject.IsSpawned}");
        if (IsServer)
        {
            isGameActive.Value = true;
            GameManager.Instance.StartNewGame();
            SyncBoardClientRpc();
            TestConnectionClientRpc();
            Debug.Log("Server sent TestConnectionClientRpc");
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
        if (!isGameActive.Value) return;

        Side playerSide = clientId == NetworkManager.ServerClientId ? Side.White : Side.Black;
        if (playerSide != currentTurn.Value)
        {
            Debug.Log("Not your turn!");
            return;
        }

        Square startSquare = start.Value.ToSquare();
        Square endSquare = end.Value.ToSquare();

        Movement move = new Movement(startSquare, endSquare);
        if (GameManager.Instance.TryExecuteMove(move))
        {
            currentTurn.Value = currentTurn.Value == Side.White ? Side.Black : Side.White;
            SyncBoardClientRpc();

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
        UIManager.Instance.resultText.text = resultText;
        UIManager.Instance.resultText.gameObject.SetActive(true);
        BoardManager.Instance.SetActiveAllPieces(false);
    }

    [ClientRpc]
    private void SyncBoardClientRpc()
    {
        BoardManager.Instance.ClearBoard();

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

        BoardManager.Instance.EnsureOnlyPiecesOfSideAreEnabled(GameManager.Instance.SideToMove);
    }

    [ServerRpc(RequireOwnership = false)]
    public void PingServerRpc(ulong clientId, float clientTime)
    {
        PongClientRpc(clientId, Time.time);
    }

    [ClientRpc]
    private void PongClientRpc(ulong clientId, float serverTime)
    {
        if(clientId == NetworkManager.Singleton.LocalClientId)
        {
            float latency = (Time.time - lastPingTime) * 1000;
            Debug.Log($"Ping: {latency} ms");
            if (latencyText != null) latencyText.text = $"Ping: {latency} ms";
        }
    }

    public bool IsMyTurn(ulong clientId)
    {
        Side playerSide = clientId == NetworkManager.ServerClientId ? Side.White : Side.Black;
        return playerSide == currentTurn.Value && isGameActive.Value;
    }

    private void Update()
    {
        if (IsClient && Time.time - lastPingTime > 1f)
        {
            lastPingTime = Time.time;
            PingServerRpc(NetworkManager.Singleton.LocalClientId, lastPingTime);
        }
    }
}